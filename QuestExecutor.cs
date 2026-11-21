using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

internal static class QuestExecutor
{
	internal static CommandResultMessage TryExecute(TriggerIncidentMessage request, Map map)
	{
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Expected Obj, but got Unknown
		//IL_082d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0832: Unknown result type (might be due to invalid IL or missing references)
		QuestScriptDef template = DefDatabase<QuestScriptDef>.GetNamedSilentFail(request.QuestTemplateDef);
		if (template == null || template.root == null)
		{
			return IncidentExecutor.Rejected(request.RequestId, "incident_unavailable", "That quest archetype is not loaded.");
		}
		if (request.Points < Math.Max(35f, template.rootMinPoints) || request.Points > 10000f)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_points", $"Quest points must be between {Math.Max(35f, template.rootMinPoints):0} and 10,000 for this archetype.");
		}
		if (request.QuestExpiryDays < 0f || request.QuestExpiryDays > 60f)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "Quest offer expiration must be between 0 and 60 days.");
		}
		if ((request.QuestTitle?.Length ?? 0) > 120 || (request.QuestDescription?.Length ?? 0) > 8000)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "The custom quest title or description is too long.");
		}
		List<string> raidIds = request.QuestRaidPawnIds ?? new List<string>();
		List<PawnItemBlueprintMessage> rewardItems = request.QuestRewardItems ?? new List<PawnItemBlueprintMessage>();
		if (raidIds.Count > 64 || request.QuestRaidDelayHours < 0f || request.QuestRaidDelayHours > 240f)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "A quest raid may contain at most 64 authored pawns and a delay between 0 and 240 hours.");
		}
		if (rewardItems.Count > 64 || GenCollection.Any<PawnItemBlueprintMessage>(rewardItems, (Predicate<PawnItemBlueprintMessage>)((PawnItemBlueprintMessage item) =>
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Invalid comparison between Unknown and I4
			if (item != null && !string.IsNullOrWhiteSpace(item.ThingDef))
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(item.ThingDef);
				if (namedSilentFail != null && (int)namedSilentFail.category == 2 && item.Count >= 1 && item.Count <= 100000 && item.Quality >= 0 && item.Quality <= 6 && !(item.HitPointsPercent <= 0f))
				{
					return item.HitPointsPercent > 1f;
				}
			}
			return true;
		})))
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "A custom quest reward item is unavailable or has invalid count, quality, or condition.");
		}
		List<QuestCastAssignmentMessage> assignments = request.QuestCastAssignments ?? new List<QuestCastAssignmentMessage>();
		if (assignments.Count > 64 || GenCollection.Any<QuestCastAssignmentMessage>(assignments, (Predicate<QuestCastAssignmentMessage>)((QuestCastAssignmentMessage questCastAssignmentMessage) => questCastAssignmentMessage == null || questCastAssignmentMessage.PawnIds == null || questCastAssignmentMessage.PawnIds.Count > 64)))
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_request", "The quest cast assignment is too large or malformed.");
		}
		List<QuestPawnRoleMessage> supportedRoles = IncidentCatalogCollector.GetQuestPawnRoles(template);
		List<string> roleIds = assignments.SelectMany((QuestCastAssignmentMessage questCastAssignmentMessage) => questCastAssignmentMessage.PawnIds).ToList();
		List<string> allIds = roleIds.Concat(raidIds).ToList();
		if (assignments.Select((QuestCastAssignmentMessage questCastAssignmentMessage) => questCastAssignmentMessage.RoleKey).Distinct(StringComparer.Ordinal).Count() != assignments.Count || GenCollection.Any<string>(allIds, (Predicate<string>)string.IsNullOrWhiteSpace) || allIds.Distinct(StringComparer.Ordinal).Count() != allIds.Count)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_request", "Quest roles must be unique, and a cast character cannot fill two roles in the same quest.");
		}
		foreach (QuestCastAssignmentMessage assignment in assignments)
		{
			QuestPawnRoleMessage role = GenCollection.FirstOrDefault<QuestPawnRoleMessage>(supportedRoles, (Predicate<QuestPawnRoleMessage>)((QuestPawnRoleMessage item) => item.Key == assignment.RoleKey));
			if (role == null || assignment.PawnIds.Count == 0 || (!role.Multiple && assignment.PawnIds.Count != 1))
			{
				return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "A cast assignment does not match a pawn role exposed by this quest archetype.");
			}
		}
		AuthoredPawnRegistryComponent registry = Current.Game.GetComponent<AuthoredPawnRegistryComponent>();
		List<Pawn> pawns = new List<Pawn>();
		if (allIds.Count > 0 && !registry.TryAcquire(allIds, null, out pawns, out string errorCode, out string errorMessage))
		{
			return IncidentExecutor.Rejected(request.RequestId, errorCode, errorMessage);
		}
		Dictionary<string, Pawn> pawnById = allIds.Select((string id, int index) => new
		{
			id = id,
			pawn = pawns[index]
		}).ToDictionary(item => item.id, item => item.pawn, StringComparer.Ordinal);
		Dictionary<string, Queue<Pawn>> roleQueues = assignments.ToDictionary((QuestCastAssignmentMessage questCastAssignmentMessage) => questCastAssignmentMessage.RoleKey, (QuestCastAssignmentMessage questCastAssignmentMessage) => new Queue<Pawn>(questCastAssignmentMessage.PawnIds.Select((string id) => pawnById[id])), StringComparer.Ordinal);
		List<Pawn> raidPawns = raidIds.Select((string id) => pawnById[id]).ToList();
		Slate slate = new Slate();
		slate.Set<float>("points", request.Points, false);
		slate.Set<Map>("map", map, false);
		slate.Set<MapParent>("mapParent", map.Parent, false);
		Faction faction = IncidentCatalogCollector.FindFaction(request.FactionId);
		if (!string.IsNullOrEmpty(request.FactionId) && faction == null)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "The selected quest faction is no longer available.");
		}
		if (faction != null)
		{
			slate.Set<Faction>("faction", faction, false);
			slate.Set<Faction>("askerFaction", faction, false);
		}
		Faction threatFaction = IncidentCatalogCollector.FindFaction(request.QuestRaidFactionId);
		if (raidIds.Count > 0 && (threatFaction == null || threatFaction.IsPlayer || threatFaction.defeated || !FactionUtility.HostileTo(threatFaction, Faction.OfPlayer)))
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "Choose an available hostile faction for the configured quest raid.");
		}
		if (raidIds.Count > 0 && (GenCollection.Any<Pawn>(raidPawns, (Predicate<Pawn>)((Pawn pawn) => ((Thing)pawn).Faction != threatFaction)) || (!string.IsNullOrEmpty(request.QuestRaidArrivalMode) && DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail(request.QuestRaidArrivalMode) == null) || (!string.IsNullOrEmpty(request.QuestRaidStrategy) && DefDatabase<RaidStrategyDef>.GetNamedSilentFail(request.QuestRaidStrategy) == null) || request.QuestRaidPoints < 35f || request.QuestRaidPoints > 10000f))
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "The configured quest raid has an invalid squad faction, points, arrival mode, or strategy.");
		}
		int raidDelayTicks = ((!(request.QuestRaidDelayHours <= 0f)) ? ((int)(request.QuestRaidDelayHours * 2500f)) : 0);
		string raidPawnKindsText = ((raidPawns.Count == 0) ? null : string.Join("\n", from @group in raidPawns.GroupBy((Pawn pawn) =>
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				return ((object)((Def)pawn.kindDef).LabelCap/*cast due to constrained. prefix*/).ToString();
			})
			select $"{@group.Count()}x {@group.Key}"));
		QuestPawnInjectionScope injection = new QuestPawnInjectionScope(roleQueues, threatFaction, raidDelayTicks, rewardItems, raidPawnKindsText);
		Quest quest;
		using (injection)
		{
			injection.SeedSlate(slate);
			if (!template.CanRun(slate, (IIncidentTarget)(object)map))
			{
				return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld says this quest archetype cannot run for the selected colony right now.");
			}
			if (!template.root.TestRun(slate.DeepCopy()))
			{
				return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld could not prepare this quest archetype with the selected custom parameters.");
			}
			injection.BeginGeneration();
			injection.SeedSlate(slate);
			quest = QuestGen.Generate(template, slate);
		}
		if (quest == null)
		{
			return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld could not generate this quest archetype. See the game log for quest-generation details.");
		}
		List<Pawn> unusedRolePawns = roleIds.Select((string id) => pawnById[id]).ToList().Except(injection.ConsumedPawns)
			.ToList();
		if (unusedRolePawns.Count > 0)
		{
			DiscardGeneratedQuest(quest, injection.ConsumedPawns);
			string unusedNames = string.Join(", ", unusedRolePawns.Select((Pawn pawn) => ((Entity)pawn).LabelShortCap));
			return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "The generated branch of this quest did not use the selected authored role(s): " + unusedNames + ". No quest was created. Try the quest again or choose a different archetype/role combination.");
		}
		if (rewardItems.Count > 0 && (injection.PreparedRewardNodes == 0 || !injection.HasAttachedAuthoredReward(quest)))
		{
			DiscardGeneratedQuest(quest, injection.ConsumedPawns);
			return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld did not attach the configured items to a native quest reward choice. No quest was created.");
		}
		if (!string.IsNullOrWhiteSpace(request.QuestTitle))
		{
			quest.name = request.QuestTitle.Trim();
		}
		if (!string.IsNullOrWhiteSpace(request.QuestDescription))
		{
			quest.description = TaggedString.op_Implicit(request.QuestDescription.Trim());
		}
		quest.initiallyAccepted = request.QuestAutoAccept;
		if (request.QuestAutoAccept)
		{
			quest.SetInitiallyAccepted();
		}
		else
		{
			quest.SetNotYetAccepted();
			if (request.QuestExpiryDays > 0f)
			{
				quest.acceptanceExpireTick = Find.TickManager.TicksGame + (int)(request.QuestExpiryDays * 60000f);
			}
		}
		List<Pawn> registeredRaidPawns = ((raidIds.Count == 0) ? new List<Pawn>() : Current.Game.GetComponent<QuestRaidRegistryComponent>().Register(quest, raidPawns, (request.QuestRaidPoints > 0f) ? request.QuestRaidPoints : request.Points, threatFaction, request.QuestRaidArrivalMode, request.QuestRaidStrategy));
		Find.QuestManager.Add(quest);
		QuestUtility.SendLetterQuestAvailable(quest, (string)null);
		List<Pawn> usedPawns = injection.ConsumedPawns.Concat(registeredRaidPawns).Distinct().ToList();
		registry.MarkQuestReserved(usedPawns, quest.id);
		CommandResultMessage commandResultMessage = new CommandResultMessage();
		commandResultMessage.RequestId = request.RequestId;
		commandResultMessage.Accepted = true;
		commandResultMessage.Message = string.Format("RimWorld {0} quest '{1}' from {2} (quest #{3}).", request.QuestAutoAccept ? "started" : "offered", quest.name, ((Def)template).defName, quest.id);
		return commandResultMessage;
	}

	private static void DiscardGeneratedQuest(Quest quest, IEnumerable<Pawn> consumedPawns)
	{
		try
		{
			quest.CleanupQuestParts();
		}
		catch (Exception arg)
		{
			Log.Warning($"[Storytell Yourself] Failed to completely clean up a rejected generated quest: {arg}");
		}
		AuthoredPawnIncidentExecutor.ReturnToWorld(consumedPawns.Distinct());
	}
}
