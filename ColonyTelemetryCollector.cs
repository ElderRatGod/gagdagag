using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using RimWorld.Planet;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

internal static class ColonyTelemetryCollector
{
	internal static ColonySnapshotMessage Capture(Map map)
	{
		Stopwatch timer = Stopwatch.StartNew();
		ColonySnapshotMessage colonySnapshotMessage = new ColonySnapshotMessage();
		TickManager tickManager = Find.TickManager;
		colonySnapshotMessage.GameTick = ((tickManager != null) ? tickManager.TicksGame : 0);
		Faction ofPlayer = Faction.OfPlayer;
		colonySnapshotMessage.ColonyName = ((ofPlayer != null) ? ofPlayer.Name : null) ?? string.Empty;
		MapParent parent = map.Parent;
		colonySnapshotMessage.MapName = ((parent != null) ? ((WorldObject)parent).LabelCap.ToString() : null) ?? ((object)map).ToString();
		colonySnapshotMessage.IsPlayerHome = map.IsPlayerHome;
		colonySnapshotMessage.WealthTotal = map.wealthWatcher.WealthTotal;
		colonySnapshotMessage.WealthItems = map.wealthWatcher.WealthItems;
		colonySnapshotMessage.WealthBuildings = map.wealthWatcher.WealthBuildings;
		colonySnapshotMessage.WealthPawns = map.wealthWatcher.WealthPawns;
		colonySnapshotMessage.RecentIncidents = IncidentHistory.GetRecent();
		ColonySnapshotMessage snapshot = colonySnapshotMessage;
		CapturePawns(map, snapshot);
		CaptureResources(map, snapshot);
		CapturePower(map, snapshot);
		timer.Stop();
		snapshot.CollectionMilliseconds = timer.Elapsed.TotalMilliseconds;
		return snapshot;
	}

	private static void CapturePawns(Map map, ColonySnapshotMessage snapshot)
	{
		float moodTotal = 0f;
		foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
		{
			if (GenHostility.HostileTo((Thing)(object)pawn, Faction.OfPlayer) && !pawn.Downed)
			{
				snapshot.HostilePawns++;
			}
			if (pawn.RaceProps.Animal)
			{
				if (((Thing)pawn).Faction == Faction.OfPlayer)
				{
					snapshot.ColonyAnimals++;
				}
			}
			else
			{
				if (!pawn.RaceProps.Humanlike)
				{
					continue;
				}
				if (pawn.IsPrisonerOfColony)
				{
					snapshot.Prisoners++;
				}
				else if (((Thing)pawn).Faction == Faction.OfPlayer)
				{
					snapshot.Colonists++;
					if (pawn.needs?.mood != null)
					{
						moodTotal += ((Need)pawn.needs.mood).CurLevelPercentage;
						snapshot.MoodPawnCount++;
					}
				}
				else if (pawn.HostFaction == Faction.OfPlayer)
				{
					snapshot.Guests++;
				}
			}
		}
		snapshot.AverageMood = ((snapshot.MoodPawnCount == 0) ? 0f : (moodTotal / (float)snapshot.MoodPawnCount));
	}

	private static void CaptureResources(Map map, ColonySnapshotMessage snapshot)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		foreach (Thing thing in map.listerThings.AllThings)
		{
			if (thing.Destroyed || thing.stackCount <= 0 || (int)thing.def.category != 2)
			{
				continue;
			}
			int count = thing.stackCount;
			switch (((Def)thing.def).defName)
			{
			case "Silver":
				snapshot.Silver += count;
				break;
			case "Steel":
				snapshot.Steel += count;
				break;
			case "WoodLog":
				snapshot.Wood += count;
				break;
			case "ComponentIndustrial":
				snapshot.Components += count;
				break;
			case "ComponentSpacer":
				snapshot.AdvancedComponents += count;
				break;
			}
			if (thing.def.IsMedicine)
			{
				snapshot.MedicineCount += count;
			}
			if (thing.def.IsNutritionGivingIngestible)
			{
				snapshot.FoodNutrition += StatExtension.GetStatValue(thing, StatDefOf.Nutrition, true, -1) * (float)count;
				if (thing.def.ingestible != null && (thing.def.ingestible.foodType & 0x100) != 0)
				{
					snapshot.MealCount += count;
				}
			}
		}
	}

	private static void CapturePower(Map map, ColonySnapshotMessage snapshot)
	{
		List<PowerNet> powerNets = map.powerNetManager.AllNetsListForReading;
		snapshot.PowerNetCount = powerNets.Count;
		foreach (PowerNet powerNet in powerNets)
		{
			snapshot.StoredEnergyWattDays += powerNet.CurrentStoredEnergy();
			snapshot.NetPowerGainWatts += powerNet.CurrentEnergyGainRate();
		}
	}
}
