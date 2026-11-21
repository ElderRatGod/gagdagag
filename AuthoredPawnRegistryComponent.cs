using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

public sealed class AuthoredPawnRegistryComponent(Game game) : GameComponent
{
	private List<AuthoredPawnRecord> records = new List<AuthoredPawnRecord>();

	public override void ExposeData()
	{
		Scribe_Collections.Look<AuthoredPawnRecord>(ref records, "authoredPawnRecords", (LookMode)2, Array.Empty<object>());
		if (records == null)
		{
			records = new List<AuthoredPawnRecord>();
		}
	}

	internal PawnBlueprintResultMessage Upsert(string requestId, PawnBlueprintMessage blueprint)
	{
		string error = Validate(blueprint);
		if (error != null)
		{
			return Result(requestId, blueprint.BlueprintId, accepted: false, "invalid_option", error);
		}
		AuthoredPawnRecord record = GenCollection.FirstOrDefault<AuthoredPawnRecord>(records, (Predicate<AuthoredPawnRecord>)((AuthoredPawnRecord item) => item.BlueprintId == blueprint.BlueprintId));
		record?.RefreshState();
		if (record != null && record.State == "dead")
		{
			return Result(requestId, blueprint.BlueprintId, accepted: false, "pawn_dead", "This character is dead in the current save and cannot be edited or reused.");
		}
		if (record == null)
		{
			record = new AuthoredPawnRecord
			{
				BlueprintId = blueprint.BlueprintId
			};
			records.Add(record);
		}
		if (blueprint.Revision < record.Revision)
		{
			return Result(requestId, blueprint.BlueprintId, accepted: false, "stale_revision", "RimWorld already has a newer revision of this character.");
		}
		int num;
		if (!(record.State == "active"))
		{
			Pawn? pawn = record.Pawn;
			if (pawn == null || !((Thing)pawn).Spawned)
			{
				Pawn? pawn2 = record.Pawn;
				num = ((((pawn2 != null) ? ((Thing)pawn2).ParentHolder : null) != null) ? 1 : 0);
				goto IL_011c;
			}
		}
		num = 1;
		goto IL_011c;
		IL_011c:
		bool active = (byte)num != 0;
		record.Revision = blueprint.Revision;
		record.BlueprintJson = ProtocolJson.SerializeToString(blueprint);
		if (record.Pawn != null && !((Thing)record.Pawn).Spawned && !record.Pawn.Dead)
		{
			AuthoredPawnFactory.Apply(record.Pawn, blueprint, ResolveFaction(blueprint.FactionId) ?? ((Thing)record.Pawn).Faction, replaceLoadout: true);
		}
		record.RefreshState();
		return Result(requestId, blueprint.BlueprintId, accepted: true, null, active ? "Character changes were saved and will be applied when the pawn is safely off-map." : "Character blueprint synchronized with this save.");
	}

	internal List<PawnInstanceMessage> GetSnapshots()
	{
		List<PawnInstanceMessage> snapshots = new List<PawnInstanceMessage>(records.Count);
		foreach (AuthoredPawnRecord record in records)
		{
			record.RefreshState();
			snapshots.Add(record.ToMessage());
		}
		return snapshots;
	}

	internal bool TryAcquire(IReadOnlyList<string> blueprintIds, Faction? fallbackFaction, out List<Pawn> pawns, out string errorCode, out string message)
	{
		pawns = new List<Pawn>();
		errorCode = string.Empty;
		message = string.Empty;
		if (blueprintIds == null || blueprintIds.Count == 0 || blueprintIds.Distinct(StringComparer.Ordinal).Count() != blueprintIds.Count)
		{
			errorCode = "invalid_request";
			message = "Select at least one unique authored character.";
			return false;
		}
		List<AuthoredPawnRecord> selectedRecords = new List<AuthoredPawnRecord>(blueprintIds.Count);
		foreach (string blueprintId in blueprintIds)
		{
			AuthoredPawnRecord record = GenCollection.FirstOrDefault<AuthoredPawnRecord>(records, (Predicate<AuthoredPawnRecord>)((AuthoredPawnRecord item) => item.BlueprintId == blueprintId));
			if (record == null || string.IsNullOrEmpty(record.BlueprintJson))
			{
				errorCode = "pawn_not_synchronized";
				message = "One of the selected characters has not been synchronized into this save.";
				return false;
			}
			record.RefreshState();
			if (!(record.State == "dead"))
			{
				Pawn? pawn = record.Pawn;
				if (pawn == null || !pawn.Dead)
				{
					if (record.State == "missing")
					{
						errorCode = "pawn_missing";
						message = record.ToMessage().Name + " was previously materialized, but its pawn reference is missing. It will not be regenerated automatically.";
						return false;
					}
					if (record.State == "active")
					{
						errorCode = "pawn_already_active";
						message = record.ToMessage().Name + " is already active on a map.";
						return false;
					}
					if (record.State == "questReserved")
					{
						errorCode = "pawn_quest_reserved";
						message = record.ToMessage().Name + " is already reserved by an active quest.";
						return false;
					}
					selectedRecords.Add(record);
					continue;
				}
			}
			errorCode = "pawn_dead";
			message = record.ToMessage().Name + " is dead in this save.";
			return false;
		}
		foreach (AuthoredPawnRecord record2 in selectedRecords)
		{
			PawnBlueprintMessage blueprint = ProtocolJson.Deserialize<PawnBlueprintMessage>(record2.BlueprintJson) ?? throw new InvalidOperationException("A synchronized character blueprint could not be read.");
			Faction faction = ResolveFaction(blueprint.FactionId) ?? fallbackFaction;
			if (record2.Pawn == null)
			{
				record2.Pawn = AuthoredPawnFactory.Create(blueprint, faction);
				record2.HasMaterialized = true;
			}
			else
			{
				AuthoredPawnFactory.Apply(record2.Pawn, blueprint, faction, replaceLoadout: true);
			}
			record2.State = "available";
			pawns.Add(record2.Pawn);
		}
		return true;
	}

	internal void MarkActive(IEnumerable<Pawn> pawns)
	{
		HashSet<Pawn> pawnSet = new HashSet<Pawn>(pawns);
		foreach (AuthoredPawnRecord record in records)
		{
			if (record.Pawn != null && pawnSet.Contains(record.Pawn))
			{
				record.State = "active";
			}
		}
	}

	internal void MarkQuestReserved(IEnumerable<Pawn> pawns, int questId)
	{
		HashSet<Pawn> pawnSet = new HashSet<Pawn>(pawns);
		foreach (AuthoredPawnRecord record in records)
		{
			if (record.Pawn != null && pawnSet.Contains(record.Pawn))
			{
				record.ReservedByQuestId = questId;
				record.State = "questReserved";
			}
		}
	}

	private static Faction? ResolveFaction(string? factionId)
	{
		if (string.IsNullOrWhiteSpace(factionId))
		{
			return null;
		}
		if (string.Equals(factionId, "player", StringComparison.OrdinalIgnoreCase))
		{
			return Faction.OfPlayer;
		}
		return IncidentCatalogCollector.FindFaction(factionId);
	}

	private static string? Validate(PawnBlueprintMessage blueprint)
	{
		if (!Guid.TryParseExact(blueprint.BlueprintId, "N", out var _))
		{
			return "blueprintId must be an immutable 32-character GUID.";
		}
		if (blueprint.Revision < 1 || string.IsNullOrWhiteSpace(blueprint.Name))
		{
			return "A positive revision and non-empty name are required.";
		}
		if (DefDatabase<PawnKindDef>.GetNamedSilentFail(blueprint.PawnKindDef) == null)
		{
			return "The selected pawn kind is not loaded.";
		}
		if (blueprint.BiologicalAgeYears < 0f || blueprint.BiologicalAgeYears > 10000f)
		{
			return "Biological age must be between 0 and 10,000 years.";
		}
		if (blueprint.Gender != "none" && blueprint.Gender != "male" && blueprint.Gender != "female")
		{
			return "Gender must be none, male, or female.";
		}
		if (!string.IsNullOrEmpty(blueprint.FactionId) && !string.Equals(blueprint.FactionId, "player", StringComparison.OrdinalIgnoreCase) && IncidentCatalogCollector.FindFaction(blueprint.FactionId) == null)
		{
			return "The selected faction is not present in this save.";
		}
		if (!Loaded<XenotypeDef>(blueprint.XenotypeDef) || !Loaded<BackstoryDef>(blueprint.ChildhoodBackstory) || !Loaded<BackstoryDef>(blueprint.AdulthoodBackstory) || !Loaded<BodyTypeDef>(blueprint.BodyTypeDef) || !Loaded<HeadTypeDef>(blueprint.HeadTypeDef) || !Loaded<HairDef>(blueprint.HairDef) || !Loaded<BeardDef>(blueprint.BeardDef) || !Loaded<TattooDef>(blueprint.FaceTattooDef) || !Loaded<TattooDef>(blueprint.BodyTattooDef))
		{
			return "An appearance, backstory, or xenotype definition is not loaded.";
		}
		foreach (PawnSkillBlueprintMessage skill in blueprint.Skills)
		{
			if (DefDatabase<SkillDef>.GetNamedSilentFail(skill.SkillDef) == null || skill.Level < 0 || skill.Level > 20 || (skill.Passion != "none" && skill.Passion != "minor" && skill.Passion != "major"))
			{
				return "A skill is missing, outside level 0–20, or has an invalid passion.";
			}
		}
		foreach (string geneDef in blueprint.GeneDefs)
		{
			if (DefDatabase<GeneDef>.GetNamedSilentFail(geneDef) == null)
			{
				return "A selected gene definition is not loaded.";
			}
		}
		foreach (string hediffDef in blueprint.HediffDefs)
		{
			if (DefDatabase<HediffDef>.GetNamedSilentFail(hediffDef) == null)
			{
				return "A selected health-condition definition is not loaded.";
			}
		}
		foreach (PawnTraitBlueprintMessage trait in blueprint.Traits)
		{
			if (DefDatabase<TraitDef>.GetNamedSilentFail(trait.TraitDef) == null)
			{
				return "A selected trait definition is not loaded.";
			}
		}
		foreach (PawnItemBlueprintMessage item in blueprint.Equipment.Concat(blueprint.Apparel).Concat(blueprint.Inventory))
		{
			ThingDef itemDef = DefDatabase<ThingDef>.GetNamedSilentFail(item.ThingDef);
			if (itemDef == null || item.Count < 1 || item.Count > itemDef.stackLimit || item.Quality < 0 || item.Quality > 6 || item.HitPointsPercent <= 0f || item.HitPointsPercent > 1f)
			{
				return "A loadout item is missing or has invalid quantity/durability.";
			}
			if (!string.IsNullOrEmpty(item.StuffDef) && DefDatabase<ThingDef>.GetNamedSilentFail(item.StuffDef) == null)
			{
				return "A selected loadout material is not loaded.";
			}
			if (!string.IsNullOrEmpty(item.StuffDef))
			{
				ThingDef stuff = DefDatabase<ThingDef>.GetNamed(item.StuffDef, true);
				if (!((BuildableDef)itemDef).MadeFromStuff || !GenStuff.AllowedStuffsFor((BuildableDef)(object)itemDef, (TechLevel)0, false).Contains(stuff))
				{
					return "The selected material cannot be used for one of the loadout items.";
				}
			}
		}
		return null;
	}

	private static bool Loaded<T>(string? defName) where T : Def
	{
		if (!string.IsNullOrEmpty(defName))
		{
			return DefDatabase<T>.GetNamedSilentFail(defName) != null;
		}
		return true;
	}

	private static PawnBlueprintResultMessage Result(string requestId, string blueprintId, bool accepted, string? code, string message)
	{
		return new PawnBlueprintResultMessage
		{
			RequestId = requestId,
			BlueprintId = blueprintId,
			Accepted = accepted,
			ErrorCode = code,
			Message = message
		};
	}
}
