using System;
using RimWorld;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

internal static class IncidentExecutor
{
	internal static CommandResultMessage TryExecute(TriggerIncidentMessage request)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		if ((int)Current.ProgramState != 2 || Current.Game == null)
		{
			return Rejected(request.RequestId, "game_unavailable", "A RimWorld game is not currently available.");
		}
		Map map = Find.CurrentMap;
		if (map == null)
		{
			return Rejected(request.RequestId, "no_map", "No colony map is currently selected.");
		}
		try
		{
			if (request.AuthoredPawnIds != null && request.AuthoredPawnIds.Count > 0)
			{
				return AuthoredPawnIncidentExecutor.Execute(request, map);
			}
			return request.Incident switch
			{
				"raid" => ExecuteRaid(request, map), 
				"trader" => ExecuteTrader(request, map), 
				"manhunter" => ExecuteManhunter(request, map), 
				"simple" => ExecuteSimple(request, map), 
				"quest" => QuestExecutor.TryExecute(request, map), 
				_ => Rejected(request.RequestId, "invalid_request", "That incident family is not supported."), 
			};
		}
		catch (Exception arg)
		{
			Log.Error($"[Storytell Yourself] Incident adapter failed for {request.Incident}: {arg}");
			return Rejected(request.RequestId, "internal_error", "The incident adapter failed. See the RimWorld log.");
		}
	}

	internal static CommandResultMessage Rejected(string? requestId, string errorCode, string message)
	{
		return new CommandResultMessage
		{
			RequestId = requestId,
			Accepted = false,
			ErrorCode = errorCode,
			Message = message
		};
	}

	private static CommandResultMessage ExecuteRaid(TriggerIncidentMessage request, Map map)
	{
		if (!ValidPoints(request.Points))
		{
			return Rejected(request.RequestId, "invalid_points", "Raid points must be between 35 and 10,000.");
		}
		IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, (IIncidentTarget)(object)map);
		parms.points = request.Points;
		if (!string.IsNullOrEmpty(request.FactionId))
		{
			Faction faction = IncidentCatalogCollector.FindFaction(request.FactionId);
			if (faction == null || faction.IsPlayer || faction.defeated || !FactionUtility.HostileTo(faction, Faction.OfPlayer))
			{
				return Rejected(request.RequestId, "invalid_option", "The selected raid faction is unavailable or not hostile.");
			}
			parms.faction = faction;
		}
		if (!string.IsNullOrEmpty(request.ArrivalMode))
		{
			PawnsArrivalModeDef arrival = DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail(request.ArrivalMode);
			if (arrival == null)
			{
				return Rejected(request.RequestId, "invalid_option", "The selected raid arrival mode is unavailable.");
			}
			parms.raidArrivalMode = arrival;
		}
		if (!string.IsNullOrEmpty(request.Strategy))
		{
			RaidStrategyDef strategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail(request.Strategy);
			if (strategy == null)
			{
				return Rejected(request.RequestId, "invalid_option", "The selected raid strategy is unavailable.");
			}
			parms.raidStrategy = strategy;
		}
		return ExecuteWorker(request.RequestId, IncidentDefOf.RaidEnemy, parms, $"{request.Points:0}-point raid");
	}

	private static CommandResultMessage ExecuteTrader(TriggerIncidentMessage request, Map map)
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		Faction faction = IncidentCatalogCollector.FindFaction(request.FactionId);
		if (faction == null || faction.IsPlayer || faction.defeated || FactionUtility.HostileTo(faction, Faction.OfPlayer))
		{
			return Rejected(request.RequestId, "invalid_option", "The selected trader faction is unavailable or hostile.");
		}
		TraderKindDef traderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail(request.TraderKind);
		if (traderKind == null || faction.def.caravanTraderKinds == null || !faction.def.caravanTraderKinds.Contains(traderKind))
		{
			return Rejected(request.RequestId, "invalid_option", "That trader kind is not available for the selected faction.");
		}
		IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail("TraderCaravanArrival");
		if (incident == null)
		{
			return Rejected(request.RequestId, "incident_unavailable", "The trader caravan incident is not loaded.");
		}
		IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, (IIncidentTarget)(object)map);
		parms.faction = faction;
		parms.traderKind = traderKind;
		return ExecuteWorker(request.RequestId, incident, parms, $"{((Def)traderKind).LabelCap} caravan from {faction.Name}");
	}

	private static CommandResultMessage ExecuteManhunter(TriggerIncidentMessage request, Map map)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		if (!ValidPoints(request.Points))
		{
			return Rejected(request.RequestId, "invalid_points", "Manhunter points must be between 35 and 10,000.");
		}
		PawnKindDef pawnKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(request.PawnKind);
		if (pawnKind == null || !pawnKind.RaceProps.Animal)
		{
			return Rejected(request.RequestId, "invalid_option", "The selected manhunter animal is unavailable.");
		}
		IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail("ManhunterPack");
		if (incident == null)
		{
			return Rejected(request.RequestId, "incident_unavailable", "The manhunter pack incident is not loaded.");
		}
		IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, (IIncidentTarget)(object)map);
		parms.points = request.Points;
		parms.pawnKind = pawnKind;
		return ExecuteWorker(request.RequestId, incident, parms, $"{request.Points:0}-point {((Def)pawnKind).LabelCap} manhunter pack");
	}

	private static CommandResultMessage ExecuteSimple(TriggerIncidentMessage request, Map map)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (!IncidentCatalogCollector.IsSimpleIncidentAllowed(request.IncidentDef))
		{
			return Rejected(request.RequestId, "invalid_option", "That one-click incident is not on the supported list.");
		}
		IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail(request.IncidentDef);
		if (incident == null)
		{
			return Rejected(request.RequestId, "incident_unavailable", "The selected incident is not loaded.");
		}
		IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, (IIncidentTarget)(object)map);
		return ExecuteWorker(request.RequestId, incident, parms, ((object)((Def)incident).LabelCap/*cast due to constrained. prefix*/).ToString());
	}

	private static CommandResultMessage ExecuteWorker(string requestId, IncidentDef incident, IncidentParms parms, string description)
	{
		bool executed;
		using (IncidentHistory.BeginHumanCommand())
		{
			executed = incident.Worker.TryExecute(parms);
		}
		if (!executed)
		{
			return Rejected(requestId, "incident_rejected", "RimWorld rejected " + description + " for the current game state.");
		}
		return new CommandResultMessage
		{
			RequestId = requestId,
			Accepted = true,
			Message = "RimWorld started " + description + "."
		};
	}

	private static bool ValidPoints(float points)
	{
		if (points >= 35f && points <= 10000f && !float.IsNaN(points))
		{
			return !float.IsInfinity(points);
		}
		return false;
	}
}
