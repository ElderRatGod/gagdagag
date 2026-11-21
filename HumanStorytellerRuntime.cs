using System.Collections.Generic;
using RimWorld;
using StorytellYourself.Protocol;
using UnityEngine;
using Verse;

namespace StorytellYourself;

internal static class HumanStorytellerRuntime
{
	private static float nextTelemetryTime;

	private static float nextCatalogTime;

	private static float nextPawnRegistryTime;

	internal static void Update()
	{
		LocalStorytellerServer server = LocalStorytellerServer.Instance;
		IncidentHistory.ResetIfGameChanged(Current.Game);
		server.ProcessPendingCommands();
		GameStateMessage state = CaptureGameState();
		server.PublishGameState(state);
		if (Time.realtimeSinceStartup >= nextPawnRegistryTime)
		{
			nextPawnRegistryTime = Time.realtimeSinceStartup + 1f;
			Game game = Current.Game;
			server.PublishPawnRegistry(((game != null) ? game.GetComponent<AuthoredPawnRegistryComponent>() : null)?.GetSnapshots() ?? new List<PawnInstanceMessage>());
		}
		if (state.State != "colonyAvailable" || Find.CurrentMap == null)
		{
			nextTelemetryTime = 0f;
			nextCatalogTime = 0f;
			server.ClearColonySnapshot();
			server.ClearIncidentCatalog();
			return;
		}
		if (Time.realtimeSinceStartup >= nextTelemetryTime)
		{
			nextTelemetryTime = Time.realtimeSinceStartup + 1f;
			server.PublishColonySnapshot(ColonyTelemetryCollector.Capture(Find.CurrentMap));
		}
		if (Time.realtimeSinceStartup >= nextCatalogTime)
		{
			nextCatalogTime = float.PositiveInfinity;
			server.PublishIncidentCatalog(IncidentCatalogCollector.Capture());
		}
	}

	private static GameStateMessage CaptureGameState()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		if (LongEventHandler.AnyEventNowOrWaiting)
		{
			return new GameStateMessage
			{
				State = "loading"
			};
		}
		if ((int)Current.ProgramState != 2 || Current.Game == null)
		{
			return new GameStateMessage
			{
				State = "mainMenu"
			};
		}
		bool paused = Find.TickManager != null && Find.TickManager.Paused;
		if (Find.CurrentMap == null)
		{
			GameStateMessage gameStateMessage = new GameStateMessage
			{
				State = "playingNoMap"
			};
			Faction ofPlayer = Faction.OfPlayer;
			gameStateMessage.ColonyName = ((ofPlayer != null) ? ofPlayer.Name : null);
			gameStateMessage.Paused = paused;
			return gameStateMessage;
		}
		GameStateMessage gameStateMessage2 = new GameStateMessage
		{
			State = "colonyAvailable"
		};
		Faction ofPlayer2 = Faction.OfPlayer;
		gameStateMessage2.ColonyName = ((ofPlayer2 != null) ? ofPlayer2.Name : null);
		gameStateMessage2.Paused = paused;
		return gameStateMessage2;
	}
}
