using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using StorytellYourself.Protocol;
using UnityEngine;
using Verse;

namespace StorytellYourself;

internal static class AuthoredPawnFactory
{
	private const long TicksPerYear = 3600000L;

	internal static Pawn Create(PawnBlueprintMessage blueprint, Faction? faction)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		PawnKindDef named = DefDatabase<PawnKindDef>.GetNamed(blueprint.PawnKindDef, true);
		Nullable gender = ParseGender(blueprint.Gender);
		XenotypeDef xenotype = (string.IsNullOrEmpty(blueprint.XenotypeDef) ? null : DefDatabase<XenotypeDef>.GetNamedSilentFail(blueprint.XenotypeDef));
		float? num = blueprint.BiologicalAgeYears;
		float? num2 = blueprint.BiologicalAgeYears;
		Nullable val = gender;
		XenotypeDef val2 = xenotype;
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(named, faction, (PawnGenerationContext)2, (Nullable)null, true, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, (Pawn)null, 1f, (Predicate<Pawn>)null, (Predicate<Pawn>)null, (IEnumerable<TraitDef>)null, (IEnumerable<TraitDef>)null, (float?)null, num, num2, val, (string)null, (string)null, (RoyalTitleDef)null, (Ideo)null, false, false, false, false, (List<GeneDef>)null, (List<GeneDef>)null, val2, (CustomXenotype)null, (List<XenotypeDef>)null, 0f, (DevelopmentalStage)8, (Func<XenotypeDef, PawnKindDef>)null, (Nullable)null, (Nullable)null, false, false, false, -1, 0, false));
		Apply(pawn, blueprint, faction, replaceLoadout: true);
		Find.WorldPawns.PassToWorld(pawn, (PawnDiscardDecideMode)1);
		Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);
		return pawn;
	}

	internal static void Apply(Pawn pawn, PawnBlueprintMessage blueprint, Faction? faction, bool replaceLoadout)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (((Thing)pawn).Spawned || ((Thing)pawn).ParentHolder != null || pawn.Dead)
		{
			return;
		}
		if (((Thing)pawn).Faction != faction)
		{
			((Thing)pawn).SetFaction(faction, (Pawn)null);
		}
		pawn.Name = (Name)(object)BuildName(blueprint.Name);
		Nullable gender = ParseGender(blueprint.Gender);
		if (gender.HasValue)
		{
			pawn.gender = gender.Value;
		}
		long ageTicks = (long)Math.Max(0.0, blueprint.BiologicalAgeYears * 3600000f);
		pawn.ageTracker.AgeBiologicalTicks = ageTicks;
		pawn.ageTracker.AgeChronologicalTicks = Math.Max(pawn.ageTracker.AgeChronologicalTicks, ageTicks);
		ApplyStory(pawn, blueprint);
		ApplySkills(pawn, blueprint.Skills);
		ApplyTraits(pawn, blueprint.Traits);
		ApplyGenesAndHealth(pawn, blueprint);
		if (replaceLoadout)
		{
			ApplyLoadout(pawn, blueprint);
		}
		Pawn_DrawTracker drawer = pawn.Drawer;
		if (drawer != null)
		{
			PawnRenderer renderer = drawer.renderer;
			if (renderer != null)
			{
				renderer.SetAllGraphicsDirty();
			}
		}
	}

	private static void ApplyStory(Pawn pawn, PawnBlueprintMessage blueprint)
	{
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		if (pawn.story != null)
		{
			if (!string.IsNullOrEmpty(blueprint.ChildhoodBackstory))
			{
				pawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail(blueprint.ChildhoodBackstory);
			}
			if (!string.IsNullOrEmpty(blueprint.AdulthoodBackstory))
			{
				pawn.story.Adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail(blueprint.AdulthoodBackstory);
			}
			pawn.story.bodyType = GetDef<BodyTypeDef>(blueprint.BodyTypeDef) ?? pawn.story.bodyType;
			pawn.story.headType = GetDef<HeadTypeDef>(blueprint.HeadTypeDef) ?? pawn.story.headType;
			pawn.story.hairDef = GetDef<HairDef>(blueprint.HairDef) ?? pawn.story.hairDef;
			if (pawn.style != null)
			{
				pawn.style.beardDef = GetDef<BeardDef>(blueprint.BeardDef) ?? pawn.style.beardDef;
				pawn.style.FaceTattoo = GetDef<TattooDef>(blueprint.FaceTattooDef) ?? pawn.style.FaceTattoo;
				pawn.style.BodyTattoo = GetDef<TattooDef>(blueprint.BodyTattooDef) ?? pawn.style.BodyTattoo;
			}
			if (TryParseColor(blueprint.HairColor, out var hairColor))
			{
				pawn.story.HairColor = hairColor;
			}
			if (TryParseColor(blueprint.SkinColor, out var skinColor))
			{
				pawn.story.SkinColorBase = skinColor;
			}
		}
	}

	private static void ApplySkills(Pawn pawn, IEnumerable<PawnSkillBlueprintMessage> blueprints)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (pawn.skills == null)
		{
			return;
		}
		foreach (PawnSkillBlueprintMessage blueprint in blueprints)
		{
			SkillDef def = DefDatabase<SkillDef>.GetNamedSilentFail(blueprint.SkillDef);
			SkillRecord record = ((def == null) ? null : pawn.skills.GetSkill(def));
			if (record != null)
			{
				record.Level = Math.Max(0, Math.Min(20, blueprint.Level));
				record.passion = (Passion)((blueprint.Passion == "major") ? 2 : ((blueprint.Passion == "minor") ? 1 : 0));
			}
		}
	}

	private static void ApplyTraits(Pawn pawn, IEnumerable<PawnTraitBlueprintMessage> blueprints)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected Obj, but got Unknown
		if (pawn.story?.traits == null)
		{
			return;
		}
		foreach (PawnTraitBlueprintMessage blueprint in blueprints)
		{
			TraitDef def = DefDatabase<TraitDef>.GetNamedSilentFail(blueprint.TraitDef);
			if (def != null && !GenCollection.Any<Trait>(pawn.story.traits.allTraits, (Predicate<Trait>)((Trait trait) => trait.def == def)))
			{
				pawn.story.traits.GainTrait(new Trait(def, blueprint.Degree, false), true);
			}
		}
	}

	private static void ApplyGenesAndHealth(Pawn pawn, PawnBlueprintMessage blueprint)
	{
		if (ModsConfig.BiotechActive && pawn.genes != null)
		{
			foreach (string geneDefName in blueprint.GeneDefs)
			{
				GeneDef gene = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
				if (gene != null && pawn.genes.GenesListForReading.All((Gene existing) => existing.def != gene))
				{
					pawn.genes.AddGene(gene, false);
				}
			}
		}
		foreach (string hediffDef in blueprint.HediffDefs)
		{
			HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDef);
			if (hediff != null && !pawn.health.hediffSet.HasHediff(hediff, false))
			{
				pawn.health.AddHediff(hediff, (BodyPartRecord)null, (Nullable)null, (DamageResult)null);
			}
		}
	}

	private static void ApplyLoadout(Pawn pawn, PawnBlueprintMessage blueprint)
	{
		if (blueprint.UseGeneratedLoadout && blueprint.Equipment.Count == 0 && blueprint.Apparel.Count == 0 && blueprint.Inventory.Count == 0)
		{
			return;
		}
		Pawn_EquipmentTracker equipment = pawn.equipment;
		if (equipment != null)
		{
			equipment.DestroyAllEquipment((DestroyMode)0);
		}
		Pawn_ApparelTracker apparel = pawn.apparel;
		if (apparel != null)
		{
			apparel.DestroyAll((DestroyMode)0);
		}
		Pawn_InventoryTracker inventory = pawn.inventory;
		if (inventory != null)
		{
			inventory.DestroyAll((DestroyMode)0);
		}
		foreach (PawnItemBlueprintMessage item3 in blueprint.Equipment)
		{
			Thing item = CreateItem(item3);
			ThingWithComps equipment2 = (ThingWithComps)(object)((item is ThingWithComps) ? item : null);
			if (equipment2 != null)
			{
				Pawn_EquipmentTracker equipment3 = pawn.equipment;
				if (equipment3 != null)
				{
					equipment3.AddEquipment(equipment2);
				}
			}
			else
			{
				item.Destroy((DestroyMode)0);
			}
		}
		foreach (PawnItemBlueprintMessage item4 in blueprint.Apparel)
		{
			Thing item2 = CreateItem(item4);
			Apparel apparel2 = (Apparel)(object)((item2 is Apparel) ? item2 : null);
			if (apparel2 != null)
			{
				Pawn_ApparelTracker apparel3 = pawn.apparel;
				if (apparel3 != null)
				{
					apparel3.Wear(apparel2, false, false);
				}
			}
			else
			{
				item2.Destroy((DestroyMode)0);
			}
		}
		foreach (PawnItemBlueprintMessage itemBlueprint in blueprint.Inventory)
		{
			Pawn_InventoryTracker inventory2 = pawn.inventory;
			if (inventory2 != null)
			{
				((ThingOwner)inventory2.innerContainer).TryAdd(CreateItem(itemBlueprint), true);
			}
		}
	}

	internal static Thing CreateItem(PawnItemBlueprintMessage blueprint)
	{
		ThingDef def = DefDatabase<ThingDef>.GetNamed(blueprint.ThingDef, true);
		ThingDef stuff = GetDef<ThingDef>(blueprint.StuffDef) ?? (((BuildableDef)def).MadeFromStuff ? GenStuff.DefaultStuffFor((BuildableDef)(object)def) : null);
		Thing item = ThingMaker.MakeThing(def, stuff);
		item.stackCount = Math.Max(1, Math.Min(blueprint.Count, def.stackLimit));
		item.HitPoints = Math.Max(1, Math.Min(item.MaxHitPoints, (int)Math.Round((float)item.MaxHitPoints * blueprint.HitPointsPercent)));
		ThingWithComps thingWithComps = (ThingWithComps)(object)((item is ThingWithComps) ? item : null);
		if (thingWithComps != null)
		{
			CompQuality val = ThingCompUtility.TryGetComp<CompQuality>((Thing)(object)thingWithComps);
			if (val != null)
			{
				val.SetQuality((QualityCategory)(byte)Math.Max(0, Math.Min(6, blueprint.Quality)), (Nullable)null);
			}
		}
		return item;
	}

	private static Nullable ParseGender(string value)
	{
		return value switch
		{
			"male" => (Nullable)(Gender)1, 
			"female" => (Gender)2, 
			"none" => (Gender)0, 
			_ => null, 
		};
	}

	private static NameTriple BuildName(string displayName)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected Obj, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected Obj, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected Obj, but got Unknown
		string[] pieces = displayName.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (pieces.Length != 0)
		{
			if (pieces.Length != 1)
			{
				string obj = pieces[0];
				string last = string.Join(" ", pieces.Skip(1));
				return new NameTriple(obj, obj, last);
			}
			return new NameTriple(pieces[0], pieces[0], string.Empty);
		}
		return new NameTriple("Story", "Story", "Pawn");
	}

	private static T? GetDef<T>(string? defName) where T : Def
	{
		if (!string.IsNullOrEmpty(defName))
		{
			return DefDatabase<T>.GetNamedSilentFail(defName);
		}
		return default;
	}

	private static bool TryParseColor(string? value, out Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		color = default;
		if (!string.IsNullOrWhiteSpace(value))
		{
			return ColorUtility.TryParseHtmlString(value, ref color);
		}
		return false;
	}
}
