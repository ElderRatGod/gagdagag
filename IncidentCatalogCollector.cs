using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

internal static class IncidentCatalogCollector
{
	private sealed class SimpleIncidentRegistration
	{
		internal string DefName { get; }

		internal string Group { get; }

		internal string? Label { get; }

		internal SimpleIncidentRegistration(string defName, string group, string? label)
		{
			DefName = defName;
			Group = group;
			Label = label;
		}
	}

	private static readonly SimpleIncidentRegistration[] SimpleIncidentDefinitions = new SimpleIncidentRegistration[98]
	{
		Register("Infestation", "Threats & attacks"),
		Register("DeepDrillInfestation", "Threats & attacks", "Too deep: infestation"),
		Register("DefoliatorShipPartCrash", "Threats & attacks", "Defoliator ship"),
		Register("PsychicEmanatorShipPartCrash", "Threats & attacks", "Psychic droner ship"),
		Register("MechCluster", "Threats & attacks"),
		Register("WastepackInfestation", "Threats & attacks"),
		Register("ProblemCauser", "Threats & attacks"),
		Register("SmallShamblerSwarm", "Threats & attacks", "Small shambler swarm"),
		Register("ShamblerSwarm", "Threats & attacks"),
		Register("ShamblerAssault", "Threats & attacks"),
		Register("SightstealerArrival", "Threats & attacks"),
		Register("SightstealerSwarm", "Threats & attacks"),
		Register("FleshbeastAttack", "Threats & attacks"),
		Register("ChimeraAssault", "Threats & attacks"),
		Register("GorehulkAssault", "Threats & attacks"),
		Register("DevourerAssault", "Threats & attacks"),
		Register("DevourerWaterAssault", "Threats & attacks", "Devourer water assault"),
		Register("GhoulAttack", "Threats & attacks"),
		Register("PsychicRitualSiege", "Threats & attacks"),
		Register("HateChanters", "Threats & attacks"),
		Register("PitGate", "Threats & attacks"),
		Register("FleshmassHeart", "Threats & attacks"),
		Register("Nociosphere", "Threats & attacks"),
		Register("Revenant", "Threats & attacks"),
		Register("Flashstorm", "Weather & conditions"),
		Register("ToxicFallout", "Weather & conditions"),
		Register("VolcanicWinter", "Weather & conditions"),
		Register("HeatWave", "Weather & conditions"),
		Register("ColdSnap", "Weather & conditions"),
		Register("Eclipse", "Weather & conditions"),
		Register("SolarFlare", "Weather & conditions"),
		Register("PsychicDrone", "Weather & conditions"),
		Register("PsychicSoothe", "Weather & conditions"),
		Register("Aurora", "Weather & conditions"),
		Register("DeathPall", "Weather & conditions"),
		Register("BloodRain", "Weather & conditions"),
		Register("UnnaturalDarkness", "Weather & conditions"),
		Register("SeasonalFlooding", "Weather & conditions"),
		Register("GillRot", "Weather & conditions"),
		Register("VolcanicAsh", "Weather & conditions"),
		Register("VolcanicDebris", "Weather & conditions"),
		Register("LavaEmergence", "Weather & conditions"),
		Register("LavaFlow", "Weather & conditions"),
		Register("BioluminescentSpores", "Weather & conditions"),
		Register("Drought", "Weather & conditions"),
		Register("NoxiousHaze", "Weather & conditions", "Acidic smog"),
		Register("Disease_Flu", "Disease & health", "Flu"),
		Register("Disease_Plague", "Disease & health", "Plague"),
		Register("Disease_Malaria", "Disease & health", "Malaria"),
		Register("Disease_SleepingSickness", "Disease & health", "Sleeping sickness"),
		Register("Disease_GutWorms", "Disease & health", "Gut worms"),
		Register("Disease_MuscleParasites", "Disease & health", "Muscle parasites"),
		Register("Disease_FibrousMechanites", "Disease & health", "Fibrous mechanites"),
		Register("Disease_SensoryMechanites", "Disease & health", "Sensory mechanites"),
		Register("Disease_OrganDecay", "Disease & health", "Organ decay"),
		Register("Disease_Abasia", "Disease & health", "Paralytic abasia"),
		Register("Disease_AnimalFlu", "Disease & health", "Animal flu"),
		Register("Disease_AnimalPlague", "Disease & health", "Animal plague"),
		Register("AnimalInsanityMass", "Animals & wildlife", "Psychic animal insanity"),
		Register("AnimalInsanitySingle", "Animals & wildlife", "Mad animal"),
		Register("FarmAnimalsWanderIn", "Animals & wildlife", "Animals join"),
		Register("SelfTame", "Animals & wildlife", "Animal self-tamed"),
		Register("HerdMigration", "Animals & wildlife"),
		Register("ThrumboPasses", "Animals & wildlife", "Rare thrumbos"),
		Register("Alphabeavers", "Animals & wildlife"),
		Register("WandererJoin", "People & visitors", "Wanderer joins"),
		Register("WildManWandersIn", "People & visitors", "Wild person wanders in"),
		Register("RefugeePodCrash", "People & visitors", "Transport pod crash"),
		Register("WandererJoinAbasia", "People & visitors", "Transport pod crash — paralytic abasia"),
		Register("RefugeePodCrash_Baby", "People & visitors", "Transport pod crash — baby"),
		Register("TravelerGroup", "People & visitors", "Travelers"),
		Register("VisitorGroup", "People & visitors", "Visitors"),
		Register("TraderCaravanArrival", "People & visitors", "Trade caravan"),
		Register("OrbitalTraderArrival", "People & visitors", "Orbital trader"),
		Register("CaravanArrivalTributeCollector", "People & visitors", "Royal tribute collector"),
		Register("StrangerInBlackJoin", "People & visitors", "Man in black"),
		Register("RaidFriendly", "People & visitors", "Friendly reinforcements"),
		Register("ResourcePodCrash", "Drops & discoveries", "Cargo pods"),
		Register("MeteoriteImpact", "Drops & discoveries", "Meteorite"),
		Register("ShipChunkDrop", "Drops & discoveries", "Ship chunks"),
		Register("AmbrosiaSprout", "Drops & discoveries"),
		Register("GauranlenPodSpawn", "Drops & discoveries", "Gauranlen pod sprout"),
		Register("PoluxTreeSpawn", "Drops & discoveries", "Polux tree sprout"),
		Register("BoomshroomSprout", "Drops & discoveries"),
		Register("OrbitalDebris", "Drops & discoveries"),
		Register("AnimaTreeSpawn", "Drops & discoveries", "Anima tree sprout"),
		Register("CropBlight", "Colony trouble"),
		Register("ShortCircuit", "Colony trouble", "Zzztt..."),
		Register("MetalhorrorImplantation", "Anomaly stories"),
		Register("WarpedObelisk_Mutator", "Anomaly stories", "Twisted obelisk"),
		Register("WarpedObelisk_Duplicator", "Anomaly stories", "Corrupted obelisk"),
		Register("WarpedObelisk_Abductor", "Anomaly stories", "Warped obelisk"),
		Register("CreepJoinerJoin", "Anomaly stories", "Creepjoiner arrival"),
		Register("UnnaturalCorpseArrival", "Anomaly stories", "Unnatural corpse"),
		Register("GoldenCubeArrival", "Anomaly stories", "Golden cube"),
		Register("MysteriousCargoRevenantSpine", "Anomaly stories", "Revenant spine / mysterious cargo"),
		Register("MysteriousCargoCube", "Anomaly stories", "Golden cube / mysterious cargo"),
		Register("MysteriousCargoUnnaturalCorpse", "Anomaly stories", "Unnatural corpse / mysterious cargo")
	};

	internal static IncidentCatalogMessage Capture()
	{
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		IncidentCatalogMessage catalog = new IncidentCatalogMessage();
		CaptureFactionsAndTraders(catalog);
		CaptureQuestCatalog(catalog);
		CaptureAuthoringCatalog(catalog);
		catalog.RaidArrivalModes = (from option in DefDatabase<PawnsArrivalModeDef>.AllDefsListForReading.Select((PawnsArrivalModeDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.RaidStrategies = (from option in DefDatabase<RaidStrategyDef>.AllDefsListForReading.Select((RaidStrategyDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.AnimalKinds = (from option in DefDatabase<PawnKindDef>.AllDefsListForReading.Where((PawnKindDef def) => def.RaceProps.Animal).Select((PawnKindDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		SimpleIncidentRegistration[] simpleIncidentDefinitions = SimpleIncidentDefinitions;
		foreach (SimpleIncidentRegistration registration in simpleIncidentDefinitions)
		{
			IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail(registration.DefName);
			if (incident != null)
			{
				string label = registration.Label ?? ((object)((Def)incident).LabelCap/*cast due to constrained. prefix*/).ToString();
				catalog.SimpleIncidents.Add(Option(((Def)incident).defName, label, registration.Group));
			}
		}
		catalog.SimpleIncidents = (from option in catalog.SimpleIncidents
			orderby option.ParentId, option.Label
			select option).ToList();
		return catalog;
	}

	internal static Faction? FindFaction(string? id)
	{
		if (!int.TryParse(id, out var loadId))
		{
			return null;
		}
		FactionManager factionManager = Find.FactionManager;
		if (factionManager == null)
		{
			return null;
		}
		return GenCollection.FirstOrDefault<Faction>(factionManager.AllFactionsListForReading, (Predicate<Faction>)((Faction faction) => faction.loadID == loadId));
	}

	internal static bool IsSimpleIncidentAllowed(string? defName)
	{
		if (defName != null)
		{
			return SimpleIncidentDefinitions.Any((SimpleIncidentRegistration registration) => registration.DefName == defName);
		}
		return false;
	}

	private static SimpleIncidentRegistration Register(string defName, string group, string? label = null)
	{
		return new SimpleIncidentRegistration(defName, group, label);
	}

	private static void CaptureFactionsAndTraders(IncidentCatalogMessage catalog)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		foreach (Faction faction in Find.FactionManager.AllFactionsVisible.OrderBy((Faction val) => val.Name))
		{
			if (faction.IsPlayer || faction.defeated)
			{
				continue;
			}
			string id = faction.loadID.ToString();
			string label = (string.IsNullOrWhiteSpace(faction.Name) ? ((object)((Def)faction.def).LabelCap/*cast due to constrained. prefix*/).ToString() : faction.Name);
			if (FactionUtility.HostileTo(faction, Faction.OfPlayer))
			{
				catalog.RaidFactions.Add(Option(id, label, null, techLevel: ((object)faction.def.techLevel/*cast due to constrained. prefix*/).ToString(), detail: ((object)((Def)faction.def).LabelCap/*cast due to constrained. prefix*/).ToString()));
				IEnumerable<PawnKindDef> kinds = (from option in (faction.def.pawnGroupMakers ?? new List<PawnGroupMaker>()).Where((PawnGroupMaker maker) => maker.kindDef == PawnGroupKindDefOf.Combat).SelectMany((PawnGroupMaker maker) => maker.options ?? new List<PawnGenOption>())
					where option.kind != null && option.kind.RaceProps.Humanlike
					select option.kind).Distinct();
				if (!kinds.Any() && faction.def.basicMemberKind != null)
				{
					kinds = new PawnKindDef[1] { faction.def.basicMemberKind };
				}
				foreach (PawnKindDef kind in kinds.OrderBy((PawnKindDef val) =>
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					return ((object)((Def)val).LabelCap/*cast due to constrained. prefix*/).ToString();
				}))
				{
					catalog.RaidPawnKinds.Add(Option(((Def)kind).defName, ((object)((Def)kind).LabelCap/*cast due to constrained. prefix*/).ToString(), id, kind.combatPower.ToString("0") + " combat power", ((object)faction.def.techLevel/*cast due to constrained. prefix*/).ToString(), kind.combatPower));
				}
			}
			else
			{
				if (faction.def.caravanTraderKinds == null || faction.def.caravanTraderKinds.Count <= 0)
				{
					continue;
				}
				catalog.TraderFactions.Add(Option(id, label));
				foreach (TraderKindDef traderKind in faction.def.caravanTraderKinds)
				{
					catalog.TraderKinds.Add(Option(((Def)traderKind).defName, ((object)((Def)traderKind).LabelCap/*cast due to constrained. prefix*/).ToString(), id));
				}
			}
		}
	}

	private static void CaptureQuestCatalog(IncidentCatalogMessage catalog)
	{
		catalog.QuestFactions = (from faction in Find.FactionManager.AllFactionsVisible
			where !faction.IsPlayer && !faction.defeated
			orderby faction.Name
			select faction).Select((Faction faction) =>
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			return Option(faction.loadID.ToString(), string.IsNullOrWhiteSpace(faction.Name) ? ((object)((Def)faction.def).LabelCap/*cast due to constrained. prefix*/).ToString() : faction.Name, null, ((object)((Def)faction.def).LabelCap/*cast due to constrained. prefix*/).ToString(), ((object)faction.def.techLevel/*cast due to constrained. prefix*/).ToString());
		}).ToList();
		catalog.QuestTemplates = (from template in DefDatabase<QuestScriptDef>.AllDefsListForReading.Where(IsUserFacingQuestRoot).Select((QuestScriptDef def) =>
			{
				QuestTemplateMessage questTemplateMessage = new QuestTemplateMessage
				{
					Id = ((Def)def).defName,
					Label = FriendlyQuestName(def)
				};
				ModContentPack modContentPack = ((Def)def).modContentPack;
				questTemplateMessage.Source = ((modContentPack != null) ? modContentPack.Name : null) ?? "Unknown source";
				questTemplateMessage.MinimumPoints = Math.Max(0f, def.rootMinPoints);
				questTemplateMessage.RandomlySelectable = def.randomlySelectable;
				questTemplateMessage.HasRaid = ContainsQuestNode<QuestNode_Raid>(def.root);
				questTemplateMessage.HasGeneratedRewards = ContainsQuestNode<QuestNode_GiveRewards>(def.root);
				questTemplateMessage.PawnRoles = SafelyDiscoverPawnRoles(def);
				return questTemplateMessage;
			})
			orderby template.Source, template.Label
			select template).ToList();
	}

	private static bool IsUserFacingQuestRoot(QuestScriptDef def)
	{
		if (def.root == null || ((Def)def).defName.StartsWith("Util_", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!GenCollection.Any<IncidentDef>(DefDatabase<IncidentDef>.AllDefsListForReading, (Predicate<IncidentDef>)((IncidentDef incident) => incident.questScriptDef == def && !(incident.Worker is IncidentWorker_GiveQuest))))
		{
			if (!def.IsRootAny && !def.randomlySelectable)
			{
				return ((object)def.root).GetType().Name.IndexOf("Root", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return true;
		}
		return false;
	}

	private static string FriendlyQuestName(QuestScriptDef def)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		string label = ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString();
		if (!string.IsNullOrWhiteSpace(label) && !string.Equals(label, ((Def)def).defName, StringComparison.OrdinalIgnoreCase))
		{
			return label;
		}
		StringBuilder builder = new StringBuilder();
		string source = ((Def)def).defName.Replace('_', ' ');
		for (int index = 0; index < source.Length; index++)
		{
			char character = source[index];
			if (index > 0 && char.IsUpper(character) && char.IsLower(source[index - 1]))
			{
				builder.Append(' ');
			}
			builder.Append(character);
		}
		return builder.ToString();
	}

	private static List<QuestPawnRoleMessage> DiscoverPawnRoles(QuestNode root)
	{
		Dictionary<string, QuestPawnRoleMessage> roles = new Dictionary<string, QuestPawnRoleMessage>(StringComparer.Ordinal);
		HashSet<QuestNode> visited = new HashSet<QuestNode>();
		VisitQuestNode(root, visited, roles);
		return roles.Values.OrderBy((QuestPawnRoleMessage role) => role.Label).ToList();
	}

	internal static List<QuestPawnRoleMessage> GetQuestPawnRoles(QuestScriptDef def)
	{
		return SafelyDiscoverPawnRoles(def);
	}

	private static List<QuestPawnRoleMessage> SafelyDiscoverPawnRoles(QuestScriptDef def)
	{
		if (def.root == null)
		{
			return new List<QuestPawnRoleMessage>();
		}
		try
		{
			List<QuestPawnRoleMessage> roles = DiscoverPawnRoles(def.root);
			AddBuiltInIndirectRoles(def, roles);
			return roles;
		}
		catch (Exception ex)
		{
			Log.Warning("[Storytell Yourself] Could not inspect cast roles for quest " + ((Def)def).defName + ": " + ex.GetBaseException().Message);
			return new List<QuestPawnRoleMessage>();
		}
	}

	private static void AddBuiltInIndirectRoles(QuestScriptDef def, List<QuestPawnRoleMessage> roles)
	{
		string key = null;
		string label = null;
		bool multiple = false;
		if (((Def)def).defName == "OpportunitySite_PrisonerWillingToJoin")
		{
			key = "prisoner";
			label = "Prisoner to rescue";
		}
		else if (((Def)def).defName == "OpportunitySite_DownedRefugee")
		{
			key = "refugee";
			label = "Downed refugee";
		}
		else if (((object)def.root).GetType().Name == "QuestNode_Root_AncientMercenaries")
		{
			key = "LEADER";
			label = "Ancient mercenary leader";
		}
		else if (((object)def.root).GetType().Name == "QuestNode_Root_Bossgroup")
		{
			key = "escortees";
			label = "Boss group members";
			multiple = true;
		}
		else if (((object)def.root).GetType().Name == "QuestNode_Root_OrbitalFugitive")
		{
			key = "fugitive";
			label = "Orbital fugitive";
		}
		if (key != null && roles.All((QuestPawnRoleMessage role) => role.Key != key))
		{
			roles.Add(new QuestPawnRoleMessage
			{
				Key = key,
				Label = label,
				Multiple = multiple
			});
			roles.Sort((QuestPawnRoleMessage left, QuestPawnRoleMessage right) => string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase));
		}
	}

	private static void VisitQuestNode(QuestNode node, HashSet<QuestNode> visited, Dictionary<string, QuestPawnRoleMessage> roles)
	{
		if (!visited.Add(node))
		{
			return;
		}
		if (node is QuestNode_GeneratePawn)
		{
			string listKey = ReadLiteralSlateKey(node, "addToList");
			string storeKey = ReadLiteralSlateKey(node, "storeAs");
			List<string> keys = new List<string>();
			if (!string.IsNullOrEmpty(listKey))
			{
				keys.Add(listKey);
			}
			if (!string.IsNullOrEmpty(storeKey))
			{
				keys.Add(storeKey);
			}
			keys.AddRange(ReadLiteralSlateKeys(node, "addToLists"));
			foreach (string roleKey in keys.Distinct(StringComparer.Ordinal))
			{
				roles[roleKey] = new QuestPawnRoleMessage
				{
					Key = roleKey,
					Label = HumanizeRole(roleKey),
					Multiple = (roleKey != storeKey)
				};
			}
		}
		else if (node is QuestNode_GetPawn)
		{
			string storeKey2 = ReadLiteralSlateKey(node, "storeAs");
			if (!string.IsNullOrEmpty(storeKey2))
			{
				roles[storeKey2] = new QuestPawnRoleMessage
				{
					Key = storeKey2,
					Label = HumanizeRole(storeKey2),
					Multiple = false
				};
			}
		}
		if (node is QuestNode_SubScript)
		{
			QuestScriptDef subScript = ReadLiteralQuestDef(node, "def");
			if (subScript?.root != null)
			{
				VisitQuestNode(subScript.root, visited, roles);
			}
		}
		Type type = ((object)node).GetType();
		while (type != null && typeof(QuestNode).IsAssignableFrom(type))
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; i++)
			{
				object value = fields[i].GetValue(node);
				QuestNode child = (QuestNode)((value is QuestNode) ? value : null);
				if (child != null)
				{
					VisitQuestNode(child, visited, roles);
				}
				else
				{
					if (!(value is IEnumerable sequence) || value is string)
					{
						continue;
					}
					foreach (object item in sequence)
					{
						QuestNode childNode = (QuestNode)((item is QuestNode) ? item : null);
						if (childNode != null)
						{
							VisitQuestNode(childNode, visited, roles);
						}
					}
				}
			}
			type = type.BaseType;
		}
	}

	private static string? ReadLiteralSlateKey(QuestNode node, string fieldName)
	{
		object slateReference = AccessTools.Field(((object)node).GetType(), fieldName)?.GetValue(node);
		string key = (slateReference?.GetType().GetProperty("RimWorld.QuestGen.ISlateRef.SlateRef", BindingFlags.Instance | BindingFlags.NonPublic))?.GetValue(slateReference, null) as string;
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		string literalKey = key;
		if (!literalKey.StartsWith("$", StringComparison.Ordinal))
		{
			return literalKey;
		}
		return null;
	}

	private static IEnumerable<string> ReadLiteralSlateKeys(QuestNode node, string fieldName)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected Obj, but got Unknown
		object slateReference = AccessTools.Field(((object)node).GetType(), fieldName)?.GetValue(node);
		if (!((slateReference?.GetType().GetMethod("GetValue", new Type[1] { typeof(Slate) }))?.Invoke(slateReference, new object[1] { (object)new Slate() }) is IEnumerable<string> keys))
		{
			return Enumerable.Empty<string>();
		}
		return keys.Where((string key) => !string.IsNullOrWhiteSpace(key) && !key.StartsWith("$", StringComparison.Ordinal)).ToList();
	}

	private static QuestScriptDef? ReadLiteralQuestDef(QuestNode node, string fieldName)
	{
		string defName = ReadLiteralSlateKey(node, fieldName);
		if (!string.IsNullOrEmpty(defName))
		{
			return DefDatabase<QuestScriptDef>.GetNamedSilentFail(defName);
		}
		return null;
	}

	private static bool ContainsQuestNode<TNode>(QuestNode root) where TNode : QuestNode
	{
		try
		{
			HashSet<QuestNode> visited = new HashSet<QuestNode>();
			return ContainsQuestNode<TNode>(root, visited);
		}
		catch (Exception ex)
		{
			Log.Warning("[Storytell Yourself] Could not inspect quest capability " + typeof(TNode).Name + ": " + ex.GetBaseException().Message);
			return false;
		}
	}

	private static bool ContainsQuestNode<TNode>(QuestNode node, HashSet<QuestNode> visited) where TNode : QuestNode
	{
		if (!visited.Add(node))
		{
			return false;
		}
		if (node is TNode)
		{
			return true;
		}
		if (node is QuestNode_SubScript)
		{
			QuestScriptDef subScript = ReadLiteralQuestDef(node, "def");
			if (subScript?.root != null && ContainsQuestNode<TNode>(subScript.root, visited))
			{
				return true;
			}
		}
		Type type = ((object)node).GetType();
		while (type != null && typeof(QuestNode).IsAssignableFrom(type))
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; i++)
			{
				object value = fields[i].GetValue(node);
				QuestNode child = (QuestNode)((value is QuestNode) ? value : null);
				if (child != null && ContainsQuestNode<TNode>(child, visited))
				{
					return true;
				}
				if (!(value is IEnumerable sequence) || value is string)
				{
					continue;
				}
				foreach (object item in sequence)
				{
					QuestNode childNode = (QuestNode)((item is QuestNode) ? item : null);
					if (childNode != null && ContainsQuestNode<TNode>(childNode, visited))
					{
						return true;
					}
				}
			}
			type = type.BaseType;
		}
		return false;
	}

	private static string HumanizeRole(string key)
	{
		StringBuilder builder = new StringBuilder();
		string source = key.Replace('_', ' ');
		for (int index = 0; index < source.Length; index++)
		{
			char character = source[index];
			if (index > 0 && char.IsUpper(character) && char.IsLower(source[index - 1]))
			{
				builder.Append(' ');
			}
			builder.Append(character);
		}
		string label = builder.ToString();
		return char.ToUpperInvariant(label[0]) + label.Substring(1);
	}

	private static void CaptureAuthoringCatalog(IncidentCatalogMessage catalog)
	{
		catalog.Skills = (from option in DefDatabase<SkillDef>.AllDefsListForReading.Select((SkillDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.Traits = (from option in DefDatabase<TraitDef>.AllDefsListForReading.SelectMany((TraitDef def) => def.degreeDatas.Select((TraitDegreeData degree) => Option(((Def)def).defName + "|" + degree.degree, degree.LabelCap, ((Def)def).defName, null, null, degree.degree)))
			orderby option.Label
			select option).ToList();
		catalog.ChildhoodBackstories = (from def in DefDatabase<BackstoryDef>.AllDefsListForReading.Where((BackstoryDef def) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Invalid comparison between Unknown and I4
				return (int)def.slot == 0;
			})
			select Option(((Def)def).defName, def.TitleCapFor((Gender)0).ToString()) into option
			orderby option.Label
			select option).ToList();
		catalog.AdulthoodBackstories = (from def in DefDatabase<BackstoryDef>.AllDefsListForReading.Where((BackstoryDef def) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Invalid comparison between Unknown and I4
				return (int)def.slot == 1;
			})
			select Option(((Def)def).defName, def.TitleCapFor((Gender)0).ToString()) into option
			orderby option.Label
			select option).ToList();
		catalog.Xenotypes = (from option in DefDatabase<XenotypeDef>.AllDefsListForReading.Select((XenotypeDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.BodyTypes = (from option in DefDatabase<BodyTypeDef>.AllDefsListForReading.Select((BodyTypeDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.HeadTypes = (from option in DefDatabase<HeadTypeDef>.AllDefsListForReading.Select((HeadTypeDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.HairStyles = (from option in DefDatabase<HairDef>.AllDefsListForReading.Select((HairDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.Beards = (from option in DefDatabase<BeardDef>.AllDefsListForReading.Select((BeardDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		catalog.Tattoos = (from option in DefDatabase<TattooDef>.AllDefsListForReading.Select((TattooDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		if (ModsConfig.BiotechActive)
		{
			catalog.Genes = (from option in DefDatabase<GeneDef>.AllDefsListForReading.Select((GeneDef def) =>
				{
					//IL_0007: Unknown result type (might be due to invalid IL or missing references)
					//IL_000c: Unknown result type (might be due to invalid IL or missing references)
					return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
				})
				orderby option.Label
				select option).ToList();
		}
		catalog.Hediffs = (from option in DefDatabase<HediffDef>.AllDefsListForReading.Where((HediffDef def) => def.spawnThingOnRemoved == null).Select((HediffDef def) =>
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString());
			})
			orderby option.Label
			select option).ToList();
		IEnumerable<ThingDef> items = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef def) =>
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			return (int)def.category == 2 && def.PlayerAcquirable;
		});
		catalog.Weapons = (from option in items.Where((ThingDef def) => def.IsWeapon).Select(ItemOption)
			orderby option.Label
			select option).ToList();
		catalog.Apparel = (from option in items.Where((ThingDef def) => def.IsApparel).Select(ItemOption)
			orderby option.Label
			select option).ToList();
		catalog.InventoryItems = (from option in items.Where((ThingDef def) => !def.IsWeapon && !def.IsApparel && !def.IsCorpse).Select(ItemOption)
			orderby option.Label
			select option).ToList();
		catalog.Stuffs = (from option in DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef def) => def.IsStuff).Select(ItemOption)
			orderby option.Label
			select option).ToList();
	}

	private static IncidentOptionMessage ItemOption(ThingDef def)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return Option(((Def)def).defName, ((object)((Def)def).LabelCap/*cast due to constrained. prefix*/).ToString(), null, def.BaseMarketValue.ToString("0.#") + " silver", ((object)def.techLevel/*cast due to constrained. prefix*/).ToString(), def.BaseMarketValue);
	}

	private static IncidentOptionMessage Option(string id, string label, string? parentId = null, string? detail = null, string? techLevel = null, float value = 0f)
	{
		return new IncidentOptionMessage
		{
			Id = id,
			Label = (string.IsNullOrWhiteSpace(label) ? id : label),
			ParentId = parentId,
			Detail = detail,
			TechLevel = techLevel,
			Value = value
		};
	}
}
