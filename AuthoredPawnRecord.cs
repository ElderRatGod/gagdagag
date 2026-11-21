using System;
using RimWorld;
using RimWorld.Planet;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

public sealed class AuthoredPawnRecord : IExposable
{
	public string BlueprintId = string.Empty;

	public int Revision;

	public string BlueprintJson = string.Empty;

	public Pawn? Pawn;

	public bool HasMaterialized;

	public string State = "unspawned";

	public int DeathTick = -1;

	public int ReservedByQuestId = -1;

	public void ExposeData()
	{
		Scribe_Values.Look<string>(ref BlueprintId, "blueprintId", string.Empty, false);
		Scribe_Values.Look<int>(ref Revision, "revision", 0, false);
		Scribe_Values.Look<string>(ref BlueprintJson, "blueprintJson", string.Empty, false);
		Scribe_References.Look<Pawn>(ref Pawn, "pawn", false);
		Scribe_Values.Look<bool>(ref HasMaterialized, "hasMaterialized", false, false);
		Scribe_Values.Look<string>(ref State, "state", "unspawned", false);
		Scribe_Values.Look<int>(ref DeathTick, "deathTick", -1, false);
		Scribe_Values.Look<int>(ref ReservedByQuestId, "reservedByQuestId", -1, false);
	}

	internal void RefreshState()
	{
		if (State == "dead")
		{
			return;
		}
		if (Pawn == null)
		{
			State = ((HasMaterialized || string.IsNullOrEmpty(BlueprintJson)) ? "missing" : "unspawned");
			return;
		}
		if (Pawn.Dead)
		{
			State = "dead";
			int deathTick;
			if (DeathTick < 0)
			{
				TickManager tickManager = Find.TickManager;
				deathTick = ((tickManager != null) ? tickManager.TicksGame : 0);
			}
			else
			{
				deathTick = DeathTick;
			}
			DeathTick = deathTick;
			return;
		}
		if (((Thing)Pawn).Spawned)
		{
			State = "active";
			return;
		}
		if (ReservedByQuestId >= 0)
		{
			QuestManager questManager = Find.QuestManager;
			if (questManager != null && GenCollection.Any<Quest>(questManager.QuestsListForReading, (Predicate<Quest>)((Quest quest) =>
			{
				//IL_000f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_001d: Invalid comparison between Unknown and I4
				return quest.id == ReservedByQuestId && ((int)quest.State == 0 || (int)quest.State == 1);
			})))
			{
				State = "questReserved";
				return;
			}
		}
		if (((Thing)Pawn).ParentHolder != null)
		{
			State = "active";
			return;
		}
		ReservedByQuestId = -1;
		if (!((Thing)Pawn).Spawned && Find.WorldPawns != null)
		{
			Find.WorldPawns.ForcefullyKeptPawns.Add(Pawn);
		}
		State = "available";
	}

	internal PawnInstanceMessage ToMessage()
	{
		PawnBlueprintMessage blueprint = (string.IsNullOrEmpty(BlueprintJson) ? null : ProtocolJson.Deserialize<PawnBlueprintMessage>(BlueprintJson));
		PawnInstanceMessage pawnInstanceMessage = new PawnInstanceMessage
		{
			BlueprintId = BlueprintId,
			Revision = Revision,
			State = State
		};
		Pawn? pawn = Pawn;
		pawnInstanceMessage.PawnThingId = ((pawn != null) ? ((Thing)pawn).ThingID : null);
		Pawn? pawn2 = Pawn;
		object obj;
		if (pawn2 == null)
		{
			obj = null;
		}
		else
		{
			Name name = pawn2.Name;
			obj = ((name != null) ? name.ToStringFull : null);
		}
		if (obj == null)
		{
			obj = blueprint?.Name ?? "Unknown character";
		}
		pawnInstanceMessage.Name = (string)obj;
		Pawn? pawn3 = Pawn;
		object faction;
		if (pawn3 == null)
		{
			faction = null;
		}
		else
		{
			Faction faction2 = ((Thing)pawn3).Faction;
			faction = ((faction2 != null) ? faction2.Name : null);
		}
		pawnInstanceMessage.Faction = (string?)faction;
		Pawn? pawn4 = Pawn;
		object obj2;
		if (pawn4 == null)
		{
			obj2 = null;
		}
		else
		{
			Map map = ((Thing)pawn4).Map;
			if (map == null)
			{
				obj2 = null;
			}
			else
			{
				MapParent parent = map.Parent;
				obj2 = ((parent != null) ? ((WorldObject)parent).LabelCap.ToString() : null);
			}
		}
		if (obj2 == null)
		{
			if (Pawn != null)
			{
				Caravan caravan = CaravanUtility.GetCaravan((Thing)(object)Pawn);
				obj2 = ((caravan != null) ? ((WorldObject)caravan).LabelCap : null);
			}
			else
			{
				obj2 = null;
			}
		}
		pawnInstanceMessage.Location = (string?)obj2;
		pawnInstanceMessage.DeathTick = DeathTick;
		return pawnInstanceMessage;
	}
}
