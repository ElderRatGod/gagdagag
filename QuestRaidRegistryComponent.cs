using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

public sealed class QuestRaidRegistryComponent(Game game) : GameComponent
{
	private static readonly FieldInfo IncidentParmsField = AccessTools.Field(typeof(QuestPart_Incident), "incidentParms");

	private List<QuestRaidRecord> records = new List<QuestRaidRecord>();

	private List<QuestRewardRecord> rewardRecords = new List<QuestRewardRecord>();

	public override void ExposeData()
	{
		Scribe_Collections.Look<QuestRaidRecord>(ref records, "humanStorytellerQuestRaids", (LookMode)2, Array.Empty<object>());
		if (records == null)
		{
			records = new List<QuestRaidRecord>();
		}
		Scribe_Collections.Look<QuestRewardRecord>(ref rewardRecords, "humanStorytellerQuestRewards", (LookMode)2, Array.Empty<object>());
		if (rewardRecords == null)
		{
			rewardRecords = new List<QuestRewardRecord>();
		}
	}

	internal List<Pawn> Register(Quest quest, IReadOnlyList<Pawn> pawns, float points, Faction faction, string? arrivalMode, string? strategy)
	{
		QuestPart_Incident part = quest.PartsListForReading.OfType<QuestPart_Incident>().FirstOrDefault((QuestPart_Incident candidate) => candidate.incident == IncidentDefOf.RaidEnemy);
		if (part == null || pawns.Count == 0)
		{
			return new List<Pawn>();
		}
		object value = IncidentParmsField.GetValue(part);
		IncidentParms parms = (IncidentParms)((value is IncidentParms) ? value : null);
		if (parms != null)
		{
			parms.points = Math.Max(35f, points);
			parms.faction = faction;
			if (!string.IsNullOrEmpty(arrivalMode))
			{
				parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail(arrivalMode);
			}
			if (!string.IsNullOrEmpty(strategy))
			{
				parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail(strategy);
			}
		}
		records.RemoveAll((QuestRaidRecord record) => record.QuestId == quest.id && record.InSignal == part.inSignal);
		records.Add(new QuestRaidRecord
		{
			QuestId = quest.id,
			InSignal = part.inSignal,
			Pawns = pawns.ToList()
		});
		return pawns.ToList();
	}

	internal bool TryBegin(QuestPart_Incident part, Signal signal, out QuestRaidExecutionState state)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		state = null;
		if (part.incident != IncidentDefOf.RaidEnemy || signal.tag != part.inSignal)
		{
			return false;
		}
		QuestRaidRecord record = GenCollection.FirstOrDefault<QuestRaidRecord>(records, (Predicate<QuestRaidRecord>)((QuestRaidRecord item) => item.QuestId == ((QuestPart)part).quest.id && item.InSignal == part.inSignal && !item.Triggered));
		if (record == null || record.Pawns.Count == 0 || GenCollection.Any<Pawn>(record.Pawns, (Predicate<Pawn>)((Pawn pawn) => pawn == null || pawn.Dead || ((Thing)pawn).Spawned)))
		{
			return false;
		}
		record.Triggered = true;
		AuthoredPawnIncidentExecutor.PrepareForVanillaWorker(record.Pawns);
		state = new QuestRaidExecutionState(record.Pawns, AuthoredPawnGenerationScope.BeginRaid(record.Pawns));
		return true;
	}

	internal void RegisterRewards(Quest quest, IReadOnlyList<PawnItemBlueprintMessage> items)
	{
		if (items.Count != 0)
		{
			rewardRecords.RemoveAll((QuestRewardRecord record) => record.QuestId == quest.id);
			rewardRecords.Add(new QuestRewardRecord
			{
				QuestId = quest.id,
				ItemsJson = ProtocolJson.SerializeToString(items.ToList())
			});
		}
	}

	internal void DeliverRewards(Quest quest, QuestEndOutcome outcome)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Expected Obj, but got Unknown
		QuestRewardRecord record = GenCollection.FirstOrDefault<QuestRewardRecord>(rewardRecords, (Predicate<QuestRewardRecord>)((QuestRewardRecord item) => item.QuestId == quest.id && !item.Delivered));
		if (record == null || (int)outcome != 1)
		{
			return;
		}
		Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
		if (map == null)
		{
			return;
		}
		record.Delivered = true;
		List<PawnItemBlueprintMessage>? obj = ProtocolJson.Deserialize<List<PawnItemBlueprintMessage>>(Encoding.UTF8.GetBytes(record.ItemsJson)) ?? new List<PawnItemBlueprintMessage>();
		List<Thing> things = new List<Thing>();
		foreach (PawnItemBlueprintMessage blueprint in obj)
		{
			ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(blueprint.ThingDef);
			if (def != null)
			{
				int remaining = blueprint.Count;
				while (remaining > 0)
				{
					int count = Math.Min(remaining, def.stackLimit);
					things.Add(AuthoredPawnFactory.CreateItem(new PawnItemBlueprintMessage
					{
						ThingDef = blueprint.ThingDef,
						StuffDef = blueprint.StuffDef,
						Count = count,
						Quality = blueprint.Quality,
						HitPointsPercent = blueprint.HitPointsPercent
					}));
					remaining -= count;
				}
			}
		}
		if (things.Count != 0)
		{
			DropPodUtility.DropThingsNear(map.Center, map, (IEnumerable<Thing>)things, 110, false, false, true, false, true, (Faction)null);
			Find.LetterStack.ReceiveLetter(TaggedString.op_Implicit("Quest reward arrived"), TaggedString.op_Implicit("The configured reward for '" + quest.name + "' has arrived in transport pods."), LetterDefOf.PositiveEvent, new LookTargets((IEnumerable<Thing>)things), (Faction)null, (Quest)null, (List<ThingDef>)null, (string)null, 0, true);
		}
	}
}
