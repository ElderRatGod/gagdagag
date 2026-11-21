using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

internal sealed class LocalStorytellerServer
{
	private sealed class ClientSession
	{
		private readonly TcpClient tcpClient;

		private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

		private int closed;

		internal string SessionId { get; }

		internal NetworkStream Stream { get; }

		internal bool HandshakeComplete { get; set; }

		internal string ClientDescription { get; set; } = "Unknown client";

		internal bool IsClosed => Volatile.Read(ref closed) != 0;

		internal ClientSession(string sessionId, TcpClient tcpClient, NetworkStream stream)
		{
			SessionId = sessionId;
			this.tcpClient = tcpClient;
			Stream = stream;
		}

		internal Task SendAsync<T>(T message)
		{
			return SendFrameAsync(1, ProtocolJson.Serialize(message));
		}

		internal void SendInBackground<T>(T message)
		{
			SendIgnoringFailureAsync(message);
		}

		internal async Task SendFrameAsync(int opcode, byte[] payload)
		{
			if (IsClosed)
			{
				throw new IOException("The client is disconnected.");
			}
			await sendLock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				await WriteFrameAsync(Stream, opcode, payload).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				sendLock.Release();
			}
		}

		internal async Task SendCloseAsync(int statusCode, string reason)
		{
			byte[] reasonBytes = Encoding.UTF8.GetBytes(reason);
			byte[] payload = new byte[2 + reasonBytes.Length];
			payload[0] = (byte)(statusCode >> 8);
			payload[1] = (byte)statusCode;
			Buffer.BlockCopy(reasonBytes, 0, payload, 2, reasonBytes.Length);
			await SendFrameAsync(8, payload).ConfigureAwait(continueOnCapturedContext: false);
		}

		internal void Close()
		{
			if (Interlocked.Exchange(ref closed, 1) == 0)
			{
				tcpClient.Close();
			}
		}

		private async Task SendIgnoringFailureAsync<T>(T message)
		{
			try
			{
				await SendAsync(message).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				Close();
			}
		}
	}

	private sealed class RequestRecord
	{
		internal string RequestId => Request.RequestId;

		internal TriggerIncidentMessage Request { get; }

		internal string Fingerprint { get; }

		internal TaskCompletionSource<CommandResultMessage> Completion { get; }

		internal RequestRecord(TriggerIncidentMessage request, string fingerprint)
		{
			Request = request;
			Fingerprint = fingerprint;
			Completion = new TaskCompletionSource<CommandResultMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}

	private sealed class BlueprintUpsertRecord
	{
		internal UpsertPawnBlueprintMessage Request { get; }

		internal TaskCompletionSource<PawnBlueprintResultMessage> Completion { get; }

		internal BlueprintUpsertRecord(UpsertPawnBlueprintMessage request)
		{
			Request = request;
			Completion = new TaskCompletionSource<PawnBlueprintResultMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}

	private sealed class WebSocketFrame
	{
		internal int Opcode { get; }

		internal byte[] Payload { get; }

		internal WebSocketFrame(int opcode, byte[] payload)
		{
			Opcode = opcode;
			Payload = payload;
		}
	}

	private const int MaximumHandshakeBytes = 16384;

	private const int MaximumRememberedRequests = 512;

	private const string WebSocketMagic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

	private static readonly LocalStorytellerServer server = new LocalStorytellerServer();

	private readonly ConcurrentDictionary<string, ClientSession> clients = new ConcurrentDictionary<string, ClientSession>();

	private readonly ConcurrentQueue<RequestRecord> pendingCommands = new ConcurrentQueue<RequestRecord>();

	private readonly ConcurrentQueue<BlueprintUpsertRecord> pendingBlueprintUpserts = new ConcurrentQueue<BlueprintUpsertRecord>();

	private readonly Dictionary<string, RequestRecord> requestLedger = new Dictionary<string, RequestRecord>(StringComparer.Ordinal);

	private readonly Queue<string> completedRequestIds = new Queue<string>();

	private readonly object lifecycleLock = new object();

	private readonly object ledgerLock = new object();

	private readonly object stateLock = new object();

	private TcpListener? listener;

	private int listeningPort;

	private bool listeningOnLan;

	private string pairingCode = string.Empty;

	private long stateSequence;

	private long snapshotSequence;

	private long catalogSequence;

	private GameStateMessage latestState = new GameStateMessage
	{
		State = "mainMenu"
	};

	private ColonySnapshotMessage? latestSnapshot;

	private IncidentCatalogMessage? latestCatalog;

	private List<PawnInstanceMessage> latestPawnRegistry = new List<PawnInstanceMessage>();

	private string latestPawnRegistryFingerprint = string.Empty;

	internal static LocalStorytellerServer Instance => server;

	private LocalStorytellerServer()
	{
	}

	internal void Configure(bool enabled, int port, bool allowLan, string configuredPairingCode)
	{
		port = StorytellYourselfSettings.ClampPort(port);
		configuredPairingCode = StorytellYourselfSettings.NormalizePairingCode(configuredPairingCode);
		lock (lifecycleLock)
		{
			if (!enabled || listener == null || listeningPort != port || listeningOnLan != allowLan || !(pairingCode == configuredPairingCode))
			{
				StopLocked();
				if (enabled)
				{
					StartLocked(port, allowLan, configuredPairingCode);
				}
			}
		}
	}

	internal void ProcessPendingCommands()
	{
		BlueprintUpsertRecord upsert;
		while (pendingBlueprintUpserts.TryDequeue(out upsert))
		{
			PawnBlueprintResultMessage result;
			try
			{
				Game game = Current.Game;
				AuthoredPawnRegistryComponent registry = ((game != null) ? game.GetComponent<AuthoredPawnRegistryComponent>() : null);
				result = ((registry == null) ? PawnBlueprintResult(upsert.Request.RequestId, upsert.Request.Blueprint.BlueprintId, accepted: false, "game_unavailable", "Load a colony before synchronizing characters.") : registry.Upsert(upsert.Request.RequestId, upsert.Request.Blueprint));
			}
			catch (Exception arg)
			{
				Log.Error($"[Storytell Yourself] Character blueprint {upsert.Request.RequestId} failed: {arg}");
				result = PawnBlueprintResult(upsert.Request.RequestId, upsert.Request.Blueprint.BlueprintId, accepted: false, "internal_error", "The character could not be synchronized. See the game log.");
			}
			upsert.Completion.TrySetResult(result);
		}
		RequestRecord request;
		while (pendingCommands.TryDequeue(out request))
		{
			CommandResultMessage result2;
			try
			{
				result2 = IncidentExecutor.TryExecute(request.Request);
			}
			catch (Exception arg2)
			{
				Log.Error($"[Storytell Yourself] Command {request.RequestId} failed: {arg2}");
				result2 = IncidentExecutor.Rejected(request.RequestId, "internal_error", "The incident failed inside RimWorld. See the game log for details.");
			}
			CompleteRequest(request, result2);
		}
	}

	internal void PublishPawnRegistry(IReadOnlyList<PawnInstanceMessage> snapshots)
	{
		string fingerprint = string.Join("|", snapshots.Select((PawnInstanceMessage item) => string.Join(":", item.BlueprintId, item.Revision.ToString(CultureInfo.InvariantCulture), item.State, item.PawnThingId ?? string.Empty, item.Faction ?? string.Empty, item.Location ?? string.Empty, item.DeathTick.ToString(CultureInfo.InvariantCulture))));
		List<PawnInstanceMessage> published;
		lock (stateLock)
		{
			latestPawnRegistry = snapshots.Select(ClonePawnInstance).ToList();
			if (fingerprint == latestPawnRegistryFingerprint)
			{
				return;
			}
			latestPawnRegistryFingerprint = fingerprint;
			published = latestPawnRegistry.Select(ClonePawnInstance).ToList();
		}
		foreach (ClientSession session in clients.Values)
		{
			if (!session.HandshakeComplete)
			{
				continue;
			}
			foreach (PawnInstanceMessage instance in published)
			{
				session.SendInBackground(instance);
			}
			session.SendInBackground(new PawnRegistryCompleteMessage
			{
				Count = published.Count
			});
		}
	}

	internal void PublishGameState(GameStateMessage state)
	{
		GameStateMessage published;
		lock (stateLock)
		{
			if (latestState.SameStateAs(state))
			{
				return;
			}
			state.Sequence = ++stateSequence;
			latestState = state.Copy();
			published = latestState.Copy();
		}
		foreach (ClientSession session in clients.Values)
		{
			if (session.HandshakeComplete)
			{
				session.SendInBackground(published);
			}
		}
	}

	internal void PublishColonySnapshot(ColonySnapshotMessage snapshot)
	{
		ColonySnapshotMessage published;
		lock (stateLock)
		{
			snapshot.Sequence = ++snapshotSequence;
			latestSnapshot = snapshot.Copy();
			published = latestSnapshot.Copy();
		}
		foreach (ClientSession session in clients.Values)
		{
			if (session.HandshakeComplete)
			{
				session.SendInBackground(published);
			}
		}
	}

	internal void ClearColonySnapshot()
	{
		lock (stateLock)
		{
			latestSnapshot = null;
		}
	}

	internal void PublishIncidentCatalog(IncidentCatalogMessage catalog)
	{
		IncidentCatalogMessage published;
		lock (stateLock)
		{
			catalog.Sequence = ++catalogSequence;
			latestCatalog = catalog.Copy();
			published = latestCatalog.Copy();
		}
		foreach (ClientSession session in clients.Values)
		{
			if (session.HandshakeComplete)
			{
				session.SendInBackground(published);
			}
		}
	}

	internal void ClearIncidentCatalog()
	{
		lock (stateLock)
		{
			latestCatalog = null;
		}
	}

	private void StartLocked(int port, bool allowLan, string configuredPairingCode)
	{
		try
		{
			IPAddress bindAddress = (allowLan ? IPAddress.Any : IPAddress.Loopback);
			TcpListener newListener = new TcpListener(bindAddress, port);
			newListener.Start();
			listener = newListener;
			listeningPort = port;
			listeningOnLan = allowLan;
			pairingCode = configuredPairingCode;
			Task.Run(() => ListenLoopAsync(newListener));
			string endpoint = (allowLan ? $"all network adapters on port {port}" : string.Format("ws://127.0.0.1:{0}{1}", port, "/storyteller/"));
			Log.Message(string.Format("[Storytell Yourself] Server started on {0} (protocol v{1}, pairing {2}).", endpoint, 4, allowLan ? "required" : "local-only"));
		}
		catch (Exception arg)
		{
			listener?.Stop();
			listener = null;
			listeningPort = 0;
			listeningOnLan = false;
			pairingCode = string.Empty;
			Log.Error($"[Storytell Yourself] Could not start the server on port {port}: {arg}");
		}
	}

	private void StopLocked()
	{
		TcpListener? tcpListener = listener;
		listener = null;
		listeningPort = 0;
		listeningOnLan = false;
		pairingCode = string.Empty;
		tcpListener?.Stop();
		foreach (ClientSession value in clients.Values)
		{
			value.Close();
		}
		clients.Clear();
		RequestRecord request;
		while (pendingCommands.TryDequeue(out request))
		{
			CompleteRequest(request, IncidentExecutor.Rejected(request.RequestId, "server_stopped", "The Human Storyteller server was stopped before the command could run."));
		}
	}

	private async Task ListenLoopAsync(TcpListener activeListener)
	{
		while (listener == activeListener)
		{
			try
			{
				TcpClient tcpClient = await activeListener.AcceptTcpClientAsync().ConfigureAwait(continueOnCapturedContext: false);
				Task.Run(() => HandleClientAsync(tcpClient));
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (SocketException arg)
			{
				if (listener == activeListener)
				{
					Log.Error($"[Storytell Yourself] Listener socket failed: {arg}");
				}
				break;
			}
			catch (Exception arg2)
			{
				Log.Error($"[Storytell Yourself] Listener error: {arg2}");
			}
		}
	}

	private async Task HandleClientAsync(TcpClient tcpClient)
	{
		string sessionId = Guid.NewGuid().ToString("N");
		using (tcpClient)
		{
			tcpClient.NoDelay = true;
			ClientSession session = null;
			try
			{
				NetworkStream stream = tcpClient.GetStream();
				if (!(await PerformWebSocketHandshakeAsync(stream).ConfigureAwait(continueOnCapturedContext: false)))
				{
					return;
				}
				session = new ClientSession(sessionId, tcpClient, stream);
				clients[sessionId] = session;
				Log.Message("[Storytell Yourself] Client connected (" + ShortId(sessionId) + "); awaiting protocol handshake.");
				await ReceiveMessagesAsync(session).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (EndOfStreamException)
			{
			}
			catch (IOException ex2)
			{
				Log.Message("[Storytell Yourself] Client " + ShortId(sessionId) + " disconnected: " + ex2.Message);
			}
			catch (Exception arg)
			{
				Log.Warning($"[Storytell Yourself] Client {ShortId(sessionId)} failed: {arg}");
			}
			finally
			{
				clients.TryRemove(sessionId, out ClientSession _);
				session?.Close();
				Log.Message("[Storytell Yourself] Client disconnected (" + ShortId(sessionId) + ").");
			}
		}
	}

	private async Task ReceiveMessagesAsync(ClientSession session)
	{
		while (!session.IsClosed)
		{
			WebSocketFrame frame = await ReadFrameAsync(session.Stream).ConfigureAwait(continueOnCapturedContext: false);
			switch (frame.Opcode)
			{
			case 1:
				await HandleTextMessageAsync(session, Encoding.UTF8.GetString(frame.Payload)).ConfigureAwait(continueOnCapturedContext: false);
				break;
			case 8:
				await session.SendFrameAsync(8, (frame.Payload.Length <= 125) ? frame.Payload : new byte[0]).ConfigureAwait(continueOnCapturedContext: false);
				return;
			case 9:
				await session.SendFrameAsync(10, frame.Payload).ConfigureAwait(continueOnCapturedContext: false);
				break;
			default:
				await session.SendCloseAsync(1003, "Only text messages are supported.").ConfigureAwait(continueOnCapturedContext: false);
				return;
			case 10:
				break;
			}
		}
	}

	private async Task HandleTextMessageAsync(ClientSession session, string json)
	{
		MessageEnvelope envelope;
		try
		{
			envelope = ProtocolJson.Deserialize<MessageEnvelope>(json);
		}
		catch (Exception)
		{
			await session.SendAsync(Error(null, "invalid_json", "The message is not valid protocol JSON.")).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		if (envelope?.Type == "hello")
		{
			await HandleHelloAsync(session, json).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (!session.HandshakeComplete)
		{
			await session.SendAsync(Error(null, "handshake_required", "Send a compatible hello message before commands.")).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (envelope?.Type == "requestPawnRegistry")
		{
			await SendPawnRegistryAsync(session).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (envelope?.Type == "upsertPawnBlueprint")
		{
			UpsertPawnBlueprintMessage upsert;
			try
			{
				upsert = ProtocolJson.Deserialize<UpsertPawnBlueprintMessage>(json);
			}
			catch (Exception)
			{
				await session.SendAsync(Error(null, "invalid_json", "The character blueprint is malformed.")).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			await session.SendAsync(await HandleBlueprintUpsertAsync(upsert).ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (envelope?.Type != "triggerIncident")
		{
			await session.SendAsync(Error(null, "unsupported_message", "This protocol message type is not supported.")).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			TriggerIncidentMessage request;
			try
			{
				request = ProtocolJson.Deserialize<TriggerIncidentMessage>(json);
			}
			catch (Exception)
			{
				await session.SendAsync(Error(null, "invalid_json", "The incident command is malformed.")).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			await session.SendAsync(await HandleIncidentRequestAsync(request).ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task<PawnBlueprintResultMessage> HandleBlueprintUpsertAsync(UpsertPawnBlueprintMessage? request)
	{
		if (request == null || string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 128 || request.Blueprint == null)
		{
			return PawnBlueprintResult(request?.RequestId, request?.Blueprint?.BlueprintId, accepted: false, "invalid_request", "A request ID and character blueprint are required.");
		}
		if (GetLatestState().State != "colonyAvailable")
		{
			return PawnBlueprintResult(request.RequestId, request.Blueprint.BlueprintId, accepted: false, "game_unavailable", "Load a colony before synchronizing characters.");
		}
		BlueprintUpsertRecord record = new BlueprintUpsertRecord(request);
		pendingBlueprintUpserts.Enqueue(record);
		return await record.Completion.Task.ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task SendPawnRegistryAsync(ClientSession session)
	{
		List<PawnInstanceMessage> snapshots;
		lock (stateLock)
		{
			snapshots = latestPawnRegistry.Select(ClonePawnInstance).ToList();
		}
		foreach (PawnInstanceMessage snapshot in snapshots)
		{
			await session.SendAsync(snapshot).ConfigureAwait(continueOnCapturedContext: false);
		}
		await session.SendAsync(new PawnRegistryCompleteMessage
		{
			Count = snapshots.Count
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task HandleHelloAsync(ClientSession session, string json)
	{
		HelloMessage hello;
		try
		{
			hello = ProtocolJson.Deserialize<HelloMessage>(json);
		}
		catch (Exception)
		{
			await session.SendAsync(Error(null, "invalid_json", "The hello message is malformed.")).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		if (hello == null || hello.ProtocolVersion != 4)
		{
			await session.SendAsync(Error(null, "protocol_mismatch", $"Protocol v{hello?.ProtocolVersion ?? 0} is incompatible; RimWorld requires v{4}.")).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		if (listeningOnLan && !PairingCodesMatch(hello.PairingCode, pairingCode))
		{
			await session.SendAsync(Error(null, "authentication_failed", "The pairing code was not accepted by RimWorld.")).ConfigureAwait(continueOnCapturedContext: false);
			Log.Warning("[Storytell Yourself] Rejected an unauthenticated LAN/VPN handshake (" + ShortId(session.SessionId) + ").");
			return;
		}
		session.HandshakeComplete = true;
		session.ClientDescription = (string.IsNullOrWhiteSpace(hello.ClientName) ? "Unknown client" : (hello.ClientName + (string.IsNullOrWhiteSpace(hello.ClientVersion) ? string.Empty : (" " + hello.ClientVersion))));
		await session.SendAsync(new HelloAckMessage
		{
			ProtocolVersion = 4,
			ServerVersion = "0.9.1-quest-composer",
			SessionId = session.SessionId
		}).ConfigureAwait(continueOnCapturedContext: false);
		await session.SendAsync(GetLatestState()).ConfigureAwait(continueOnCapturedContext: false);
		ColonySnapshotMessage snapshot = GetLatestSnapshot();
		if (snapshot != null)
		{
			await session.SendAsync(snapshot).ConfigureAwait(continueOnCapturedContext: false);
		}
		IncidentCatalogMessage catalog = GetLatestCatalog();
		if (catalog != null)
		{
			await session.SendAsync(catalog).ConfigureAwait(continueOnCapturedContext: false);
		}
		await SendPawnRegistryAsync(session).ConfigureAwait(continueOnCapturedContext: false);
		Log.Message("[Storytell Yourself] Protocol handshake accepted for " + session.ClientDescription + " (" + ShortId(session.SessionId) + ").");
	}

	private static bool PairingCodesMatch(string? supplied, string expected)
	{
		byte[] suppliedHash;
		byte[] expectedHash;
		using (SHA256 sha256 = SHA256.Create())
		{
			suppliedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes((supplied ?? string.Empty).Trim().ToUpperInvariant()));
			expectedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(expected));
		}
		int difference = 0;
		for (int index = 0; index < expectedHash.Length; index++)
		{
			difference |= suppliedHash[index] ^ expectedHash[index];
		}
		return difference == 0;
	}

	private static PawnBlueprintResultMessage PawnBlueprintResult(string? requestId, string? blueprintId, bool accepted, string? errorCode, string message)
	{
		return new PawnBlueprintResultMessage
		{
			RequestId = (requestId ?? string.Empty),
			BlueprintId = (blueprintId ?? string.Empty),
			Accepted = accepted,
			ErrorCode = errorCode,
			Message = message
		};
	}

	private static PawnInstanceMessage ClonePawnInstance(PawnInstanceMessage source)
	{
		return new PawnInstanceMessage
		{
			BlueprintId = source.BlueprintId,
			Revision = source.Revision,
			State = source.State,
			PawnThingId = source.PawnThingId,
			Name = source.Name,
			Faction = source.Faction,
			Location = source.Location,
			DeathTick = source.DeathTick
		};
	}

	private async Task<CommandResultMessage> HandleIncidentRequestAsync(TriggerIncidentMessage? request)
	{
		if (request == null || string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 128)
		{
			return IncidentExecutor.Rejected(request?.RequestId, "invalid_request", "A non-empty requestId of at most 128 characters is required.");
		}
		if (request.Incident != "raid" && request.Incident != "trader" && request.Incident != "manhunter" && request.Incident != "simple" && request.Incident != "quest")
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_request", "That incident family is not supported in Stage 3.");
		}
		if ((request.Incident == "raid" || request.Incident == "manhunter" || request.Incident == "quest") && (request.Points < 35f || request.Points > 10000f || float.IsNaN(request.Points) || float.IsInfinity(request.Points)))
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_points", "Threat points must be between 35 and 10,000.");
		}
		string fingerprint = BuildFingerprint(request);
		RequestRecord record;
		bool duplicate;
		lock (ledgerLock)
		{
			if (requestLedger.TryGetValue(request.RequestId, out RequestRecord existing))
			{
				if (existing.Fingerprint != fingerprint)
				{
					return IncidentExecutor.Rejected(request.RequestId, "request_id_conflict", "That requestId was already used for a different command.");
				}
				record = existing;
				duplicate = true;
			}
			else
			{
				record = new RequestRecord(CloneRequest(request), fingerprint);
				requestLedger.Add(request.RequestId, record);
				duplicate = false;
			}
		}
		if (!duplicate)
		{
			if (GetLatestState().State != "colonyAvailable")
			{
				CompleteRequest(record, IncidentExecutor.Rejected(request.RequestId, "game_unavailable", "A colony map is not currently available."));
			}
			else
			{
				pendingCommands.Enqueue(record);
			}
		}
		CommandResultMessage result = await record.Completion.Task.ConfigureAwait(continueOnCapturedContext: false);
		if (duplicate)
		{
			Log.Message("[Storytell Yourself] Duplicate request " + request.RequestId + " returned its original result without executing again.");
			return result.AsDuplicate();
		}
		return result;
	}

	private void CompleteRequest(RequestRecord request, CommandResultMessage result)
	{
		if (!request.Completion.TrySetResult(result))
		{
			return;
		}
		lock (ledgerLock)
		{
			completedRequestIds.Enqueue(request.RequestId);
			while (completedRequestIds.Count > 512)
			{
				requestLedger.Remove(completedRequestIds.Dequeue());
			}
		}
		string outcome = (result.Accepted ? "accepted" : ("rejected (" + result.ErrorCode + ")"));
		Log.Message("[Storytell Yourself] Command " + request.RequestId + ": " + request.Request.Incident + " " + outcome + ". " + result.Message);
	}

	private static string BuildFingerprint(TriggerIncidentMessage request)
	{
		return string.Join("|", request.Incident, request.Points.ToString("R", CultureInfo.InvariantCulture), request.FactionId ?? string.Empty, request.ArrivalMode ?? string.Empty, request.Strategy ?? string.Empty, request.TraderKind ?? string.Empty, request.PawnKind ?? string.Empty, request.IncidentDef ?? string.Empty, request.AuthoredPawnRole ?? string.Empty, string.Join(",", request.AuthoredPawnIds ?? new List<string>()), request.QuestTemplateDef ?? string.Empty, request.QuestExpiryDays.ToString("R", CultureInfo.InvariantCulture), request.QuestAutoAccept.ToString(), request.QuestTitle ?? string.Empty, request.QuestDescription ?? string.Empty, string.Join(";", (request.QuestCastAssignments ?? new List<QuestCastAssignmentMessage>()).Select((QuestCastAssignmentMessage assignment) => (assignment.RoleKey ?? string.Empty) + ":" + string.Join(",", assignment.PawnIds ?? new List<string>()))), string.Join(",", request.QuestRaidPawnIds ?? new List<string>()), request.QuestRaidPoints.ToString("R", CultureInfo.InvariantCulture), request.QuestRaidFactionId ?? string.Empty, request.QuestRaidArrivalMode ?? string.Empty, request.QuestRaidStrategy ?? string.Empty, request.QuestRaidDelayHours.ToString("R", CultureInfo.InvariantCulture), string.Join(";", (request.QuestRewardItems ?? new List<PawnItemBlueprintMessage>()).Select((PawnItemBlueprintMessage item) => string.Join(",", item.ThingDef, item.StuffDef ?? string.Empty, item.Count.ToString(CultureInfo.InvariantCulture), item.Quality.ToString(CultureInfo.InvariantCulture), item.HitPointsPercent.ToString("R", CultureInfo.InvariantCulture)))));
	}

	private static TriggerIncidentMessage CloneRequest(TriggerIncidentMessage request)
	{
		return new TriggerIncidentMessage
		{
			RequestId = request.RequestId,
			Incident = request.Incident,
			Points = request.Points,
			FactionId = request.FactionId,
			ArrivalMode = request.ArrivalMode,
			Strategy = request.Strategy,
			TraderKind = request.TraderKind,
			PawnKind = request.PawnKind,
			IncidentDef = request.IncidentDef,
			AuthoredPawnRole = request.AuthoredPawnRole,
			AuthoredPawnIds = ((request.AuthoredPawnIds == null) ? new List<string>() : new List<string>(request.AuthoredPawnIds)),
			QuestTemplateDef = request.QuestTemplateDef,
			QuestExpiryDays = request.QuestExpiryDays,
			QuestAutoAccept = request.QuestAutoAccept,
			QuestTitle = request.QuestTitle,
			QuestDescription = request.QuestDescription,
			QuestCastAssignments = ((request.QuestCastAssignments == null) ? new List<QuestCastAssignmentMessage>() : request.QuestCastAssignments.Select((QuestCastAssignmentMessage assignment) => assignment.Copy()).ToList()),
			QuestRaidPawnIds = ((request.QuestRaidPawnIds == null) ? new List<string>() : new List<string>(request.QuestRaidPawnIds)),
			QuestRaidPoints = request.QuestRaidPoints,
			QuestRaidFactionId = request.QuestRaidFactionId,
			QuestRaidArrivalMode = request.QuestRaidArrivalMode,
			QuestRaidStrategy = request.QuestRaidStrategy,
			QuestRaidDelayHours = request.QuestRaidDelayHours,
			QuestRewardItems = ((request.QuestRewardItems == null) ? new List<PawnItemBlueprintMessage>() : request.QuestRewardItems.Select((PawnItemBlueprintMessage item) => new PawnItemBlueprintMessage
			{
				ThingDef = item.ThingDef,
				StuffDef = item.StuffDef,
				Count = item.Count,
				Quality = item.Quality,
				HitPointsPercent = item.HitPointsPercent
			}).ToList())
		};
	}

	private GameStateMessage GetLatestState()
	{
		lock (stateLock)
		{
			return latestState.Copy();
		}
	}

	private ColonySnapshotMessage? GetLatestSnapshot()
	{
		lock (stateLock)
		{
			return latestSnapshot?.Copy();
		}
	}

	private IncidentCatalogMessage? GetLatestCatalog()
	{
		lock (stateLock)
		{
			return latestCatalog?.Copy();
		}
	}

	private static ProtocolErrorMessage Error(string? requestId, string code, string message)
	{
		return new ProtocolErrorMessage
		{
			RequestId = requestId,
			ErrorCode = code,
			Message = message,
			SupportedProtocolVersion = 4
		};
	}

	private static string ShortId(string sessionId)
	{
		if (sessionId.Length > 8)
		{
			return sessionId.Substring(0, 8);
		}
		return sessionId;
	}

	private static async Task<bool> PerformWebSocketHandshakeAsync(NetworkStream stream)
	{
		string[] lines = (await ReadHttpHeadersAsync(stream).ConfigureAwait(continueOnCapturedContext: false)).Split(new string[1] { "\r\n" }, StringSplitOptions.None);
		string[] requestLine = ((lines.Length == 0) ? new string[0] : lines[0].Split(' ', StringSplitOptions.None));
		Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int index = 1; index < lines.Length; index++)
		{
			int separator = lines[index].IndexOf(':');
			if (separator > 0)
			{
				headers[lines[index].Substring(0, separator).Trim()] = lines[index].Substring(separator + 1).Trim();
			}
		}
		string upgrade = string.Empty;
		string version = string.Empty;
		string key = string.Empty;
		if (requestLine.Length < 2 || !requestLine[0].Equals("GET", StringComparison.OrdinalIgnoreCase) || !(requestLine[1] == "/storyteller/") || !headers.TryGetValue("Upgrade", out upgrade) || !upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase) || !headers.TryGetValue("Sec-WebSocket-Version", out version) || !(version == "13") || !headers.TryGetValue("Sec-WebSocket-Key", out key) || string.IsNullOrWhiteSpace(key))
		{
			byte[] badRequest = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
			await stream.WriteAsync(badRequest, 0, badRequest.Length).ConfigureAwait(continueOnCapturedContext: false);
			return false;
		}
		string accept;
		using (SHA1 sha1 = SHA1.Create())
		{
			accept = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
		}
		byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n");
		await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(continueOnCapturedContext: false);
		return true;
	}

	private static async Task<string> ReadHttpHeadersAsync(NetworkStream stream)
	{
		using (MemoryStream buffer = new MemoryStream())
		{
			int matched = 0;
			byte[] terminator = new byte[4] { 13, 10, 13, 10 };
			byte[] oneByte = new byte[1];
			while (buffer.Length < 16384)
			{
				await ReadExactlyAsync(stream, oneByte, 0, 1).ConfigureAwait(continueOnCapturedContext: false);
				byte value = oneByte[0];
				buffer.WriteByte(value);
				matched = ((value == terminator[matched]) ? (matched + 1) : ((value == terminator[0]) ? 1 : 0));
				if (matched == terminator.Length)
				{
					return Encoding.ASCII.GetString(buffer.ToArray());
				}
			}
		}
		throw new InvalidDataException("WebSocket handshake was too large.");
	}

	private static async Task<WebSocketFrame> ReadFrameAsync(NetworkStream stream)
	{
		byte[] header = new byte[2];
		await ReadExactlyAsync(stream, header, 0, header.Length).ConfigureAwait(continueOnCapturedContext: false);
		bool flag = (header[0] & 0x80) != 0;
		int opcode = header[0] & 0xF;
		bool masked = (header[1] & 0x80) != 0;
		ulong length = (ulong)(header[1] & 0x7F);
		if (!flag || !masked)
		{
			throw new InvalidDataException("Expected one complete, masked WebSocket frame.");
		}
		switch (length)
		{
		case 126uL:
		{
			byte[] extended = new byte[2];
			await ReadExactlyAsync(stream, extended, 0, extended.Length).ConfigureAwait(continueOnCapturedContext: false);
			length = (ulong)((extended[0] << 8) | extended[1]);
			break;
		}
		case 127uL:
		{
			byte[] extended = new byte[8];
			await ReadExactlyAsync(stream, extended, 0, extended.Length).ConfigureAwait(continueOnCapturedContext: false);
			length = 0uL;
			for (int index = 0; index < extended.Length; index++)
			{
				length = (length << 8) | extended[index];
			}
			break;
		}
		}
		if (length > 2097152)
		{
			throw new InvalidDataException("WebSocket message was too large.");
		}
		byte[] mask = new byte[4];
		await ReadExactlyAsync(stream, mask, 0, mask.Length).ConfigureAwait(continueOnCapturedContext: false);
		byte[] payload = new byte[(uint)length];
		await ReadExactlyAsync(stream, payload, 0, payload.Length).ConfigureAwait(continueOnCapturedContext: false);
		for (int i = 0; i < payload.Length; i++)
		{
			payload[i] ^= mask[i % mask.Length];
		}
		return new WebSocketFrame(opcode, payload);
	}

	private static async Task WriteFrameAsync(NetworkStream stream, int opcode, byte[] payload)
	{
		if (payload.Length > 2097152)
		{
			throw new InvalidDataException("Outgoing WebSocket message was too large.");
		}
		using MemoryStream frame = new MemoryStream();
		frame.WriteByte((byte)(0x80 | opcode));
		if (payload.Length <= 125)
		{
			frame.WriteByte((byte)payload.Length);
		}
		else if (payload.Length <= 65535)
		{
			frame.WriteByte(126);
			frame.WriteByte((byte)(payload.Length >> 8));
			frame.WriteByte((byte)payload.Length);
		}
		else
		{
			frame.WriteByte(127);
			ulong length = (ulong)payload.LongLength;
			for (int shift = 56; shift >= 0; shift -= 8)
			{
				frame.WriteByte((byte)(length >> shift));
			}
		}
		frame.Write(payload, 0, payload.Length);
		byte[] frameBytes = frame.ToArray();
		await stream.WriteAsync(frameBytes, 0, frameBytes.Length).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count)
	{
		while (count > 0)
		{
			int read = await stream.ReadAsync(buffer, offset, count).ConfigureAwait(continueOnCapturedContext: false);
			if (read == 0)
			{
				throw new EndOfStreamException();
			}
			offset += read;
			count -= read;
		}
	}
}
