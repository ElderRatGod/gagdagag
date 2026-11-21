using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using StorytellYourself.Protocol;
using Verse;
using Verse.AI.Group;

namespace StorytellYourself;

internal static class AuthoredPawnIncidentExecutor
{
	internal static CommandResultMessage Execute(TriggerIncidentMessage request, Map map)
	{
		return (string.IsNullOrEmpty(request.AuthoredPawnRole) ? InferRole(request) : (request.AuthoredPawnRole ?? string.Empty)) switch
		{
			"raidAttacker" => ExecuteRaid(request, map), 
			"trader" => ExecuteTrader(request, map), 
			"wandererJoiner" => ExecuteWandererJoin(request, map), 
			"podCrash" => ExecutePodCrash(request, map), 
			_ => IncidentExecutor.Rejected(request.RequestId, "invalid_option", "That authored-character role is not supported."), 
		};
	}

	private static CommandResultMessage ExecuteRaid(TriggerIncidentMessage request, Map map)
	{
		Faction faction = IncidentCatalogCollector.FindFaction(request.FactionId);
		if (faction == null || faction.IsPlayer || faction.defeated || !FactionUtility.HostileTo(faction, Faction.OfPlayer))
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "Choose an available hostile faction for authored raiders.");
		}
		if (!TryAcquire(request, faction, out AuthoredPawnRegistryComponent registry, out List<Pawn> pawns, out CommandResultMessage error))
		{
			return error;
		}
		if (GenCollection.Any<Pawn>(pawns, (Predicate<Pawn>)((Pawn pawn) => ((Thing)pawn).Faction != faction)))
		{
			ReturnToWorld(pawns);
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "Every authored raider must belong to the selected hostile faction.");
		}
		IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, (IIncidentTarget)(object)map);
		parms.points = Math.Max(35f, request.Points);
		parms.faction = faction;
		if (!string.IsNullOrEmpty(request.ArrivalMode))
		{
			parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail(request.ArrivalMode);
		}
		if (!string.IsNullOrEmpty(request.Strategy))
		{
			parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail(request.Strategy);
		}
		PrepareForVanillaWorker(pawns);
		bool executed;
		using (AuthoredPawnGenerationScope.BeginRaid(pawns))
		{
			using (IncidentHistory.BeginHumanCommand())
			{
				executed = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
			}
		}
		if (!executed)
		{
			ReturnToWorld(pawns);
			return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld rejected the authored raid composition for the current map.");
		}
		registry.MarkActive(pawns);
		return new CommandResultMessage
		{
			RequestId = request.RequestId,
			Accepted = true,
			Message = $"RimWorld started a raid containing exactly {pawns.Count} authored pawn(s)."
		};
	}

	private static CommandResultMessage ExecuteTrader(TriggerIncidentMessage request, Map map)
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected Obj, but got Unknown
		Faction faction = IncidentCatalogCollector.FindFaction(request.FactionId);
		TraderKindDef traderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail(request.TraderKind);
		if (faction == null || faction.IsPlayer || faction.defeated || FactionUtility.HostileTo(faction, Faction.OfPlayer) || traderKind == null)
		{
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "Choose an available friendly faction and trader kind.");
		}
		if (!TryAcquire(request, faction, out AuthoredPawnRegistryComponent registry, out List<Pawn> pawns, out CommandResultMessage error))
		{
			return error;
		}
		if (GenCollection.Any<Pawn>(pawns, (Predicate<Pawn>)((Pawn val) => ((Thing)val).Faction != faction)))
		{
			ReturnToWorld(pawns);
			return IncidentExecutor.Rejected(request.RequestId, "invalid_option", "Every authored trader must belong to the selected visiting faction.");
		}
		if (!TryFindEntry(map, out var entry))
		{
			ReturnToWorld(pawns);
			return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld could not find a valid caravan entry cell.");
		}
		foreach (Pawn pawn in pawns)
		{
			if (pawn.trader != null)
			{
				pawn.trader.traderKind = traderKind;
			}
		}
		SpawnNear(pawns, entry, map);
		LordMaker.MakeNewLord(faction, (LordJob)new LordJob_TradeWithColony(faction, map.Center), map, (IEnumerable<Pawn>)pawns);
		registry.MarkActive(pawns);
		return Accepted(request.RequestId, "TraderCaravanArrival", $"{pawns.Count} authored trader(s) arrived as persistent characters.");
	}

	private static CommandResultMessage ExecuteWandererJoin(TriggerIncidentMessage request, Map map)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected Obj, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (!TryAcquire(request, Faction.OfPlayer, out AuthoredPawnRegistryComponent registry, out List<Pawn> pawns, out CommandResultMessage error))
		{
			return error;
		}
		if (pawns.Count != 1)
		{
			ReturnToWorld(pawns);
			return IncidentExecutor.Rejected(request.RequestId, "invalid_request", "The vanilla wanderer-join event accepts exactly one authored pawn.");
		}
		Pawn pawn = pawns[0];
		((Thing)pawn).SetFaction(Faction.OfPlayer, (Pawn)null);
		if (!TryFindEntry(map, out var entry))
		{
			ReturnToWorld(pawns);
			return IncidentExecutor.Rejected(request.RequestId, "incident_rejected", "RimWorld could not find a valid map-edge cell for the wanderer.");
		}
		SpawnNear(pawns, entry, map);
		registry.MarkActive(pawns);
		Find.LetterStack.ReceiveLetter(TaggedString.op_Implicit("Wanderer joins"), TaggedString.op_Implicit($"{pawn.NameFullColored} has arrived and joined the colony."), LetterDefOf.PositiveEvent, new LookTargets((Thing)(object)pawn), Faction.OfPlayer, (Quest)null, (List<ThingDef>)null, (string)null, 0, true);
		return Accepted(request.RequestId, "WandererJoin", TaggedString.op_Implicit(pawn.NameShortColored + " arrived and joined the colony as authored."));
	}

	private static CommandResultMessage ExecutePodCrash(TriggerIncidentMessage request, Map map)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected Obj, but got Unknown
		if (!TryAcquire(request, null, out AuthoredPawnRegistryComponent registry, out List<Pawn> pawns, out CommandResultMessage error))
		{
			return error;
		}
		foreach (Pawn pawn in pawns)
		{
			Find.WorldPawns.RemovePawn(pawn);
			HealthUtility.DamageUntilDowned(pawn, false, (DamageDef)null, (ThingDef)null, (BodyPartGroupDef)null);
		}
		DropPodUtility.DropThingsNear(map.Center, map, pawns.Cast<Thing>(), 110, false, false, true, false, true, (Faction)null);
		registry.MarkActive(pawns);
		Find.LetterStack.ReceiveLetter(TaggedString.op_Implicit((pawns.Count == 1) ? "Transport pod crash" : "Transport pod crash survivors"), TaggedString.op_Implicit((pawns.Count == 1) ? $"A transport pod has crashed nearby. Inside is {pawns[0].NameFullColored}, wounded and in need of help." : $"Transport pods have crashed nearby carrying {pawns.Count} wounded survivors."), LetterDefOf.NeutralEvent, new LookTargets((IEnumerable<Pawn>)pawns), ((Thing)pawns[0]).Faction, (Quest)null, (List<ThingDef>)null, (string)null, 0, true);
		return Accepted(request.RequestId, "RefugeePodCrash", $"{pawns.Count} authored character(s) crashed in transport pods.");
	}

	private static bool TryAcquire(TriggerIncidentMessage request, Faction? faction, out AuthoredPawnRegistryComponent registry, out List<Pawn> pawns, out CommandResultMessage error)
	{
		registry = Current.Game.GetComponent<AuthoredPawnRegistryComponent>();
		if (!registry.TryAcquire(request.AuthoredPawnIds, faction, out pawns, out string errorCode, out string message))
		{
			error = IncidentExecutor.Rejected(request.RequestId, errorCode, message);
			return false;
		}
		error = null;
		return true;
	}

	private static bool TryFindEntry(Map map, out IntVec3 entry)
	{
		return RCellFinder.TryFindRandomPawnEntryCell(ref entry, map, CellFinder.EdgeRoadChance_Always, false, (Predicate<IntVec3>)null);
	}

	private static void SpawnNear(IEnumerable<Pawn> pawns, IntVec3 entry, Map map)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		foreach (Pawn pawn in pawns)
		{
			Find.WorldPawns.RemovePawn(pawn);
			IntVec3 spawnCell = CellFinder.FindNoWipeSpawnLocNear(entry, map, ((Thing)pawn).def, Rot4.Random, 12, (Predicate<IntVec3>)null);
			GenSpawn.Spawn((Thing)(object)pawn, spawnCell, map, (WipeMode)0);
		}
	}

	internal static void PrepareForVanillaWorker(IEnumerable<Pawn> pawns)
	{
		foreach (Pawn pawn in pawns)
		{
			if (Find.WorldPawns.AllPawnsAlive.Contains(pawn))
			{
				Find.WorldPawns.RemovePawn(pawn);
			}
		}
	}

	internal static void ReturnToWorld(IEnumerable<Pawn> pawns)
	{
		foreach (Pawn pawn in pawns.Where((Pawn val) => !((Thing)val).Spawned && !val.Dead))
		{
			if (!Find.WorldPawns.AllPawnsAlive.Contains(pawn))
			{
				Find.WorldPawns.PassToWorld(pawn, (PawnDiscardDecideMode)1);
			}
			Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);
		}
	}

	private static string InferRole(TriggerIncidentMessage request)
	{
		if (request.Incident == "raid")
		{
			return "raidAttacker";
		}
		if (request.Incident == "trader")
		{
			return "trader";
		}
		if (!(request.IncidentDef == "RefugeePodCrash"))
		{
			return "wandererJoiner";
		}
		return "podCrash";
	}

	private static CommandResultMessage Accepted(string requestId, string incidentDefName, string message)
	{
		using (IncidentHistory.BeginHumanCommand())
		{
			IncidentHistory.Record(DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName));
		}
		return new CommandResultMessage
		{
			RequestId = requestId,
			Accepted = true,
			Message = message
		};
	}
}
