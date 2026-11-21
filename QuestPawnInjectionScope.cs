using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using StorytellYourself.Protocol;
using Verse;
using Verse.Grammar;

namespace StorytellYourself;

internal sealed class QuestPawnInjectionScope : IDisposable
{
	[ThreadStatic]
	private static QuestPawnInjectionScope? current;

	private static readonly FieldInfo QuestGenSlateField = AccessTools.Field(typeof(QuestGen), "slate");

	private static readonly FieldInfo QuestGenQuestField = AccessTools.Field(typeof(QuestGen), "quest");

	private readonly Dictionary<string, Queue<Pawn>> roles;

	private readonly List<Pawn> consumedPawns = new List<Pawn>();

	private readonly Faction? threatFaction;

	private readonly int raidDelayTicks;

	private readonly List<PawnItemBlueprintMessage> rewardItems;

	private readonly List<Reward_Items> authoredRewards = new List<Reward_Items>();

	private readonly string? raidPawnKindsText;

	private string? activeRole;

	private bool activeRoleAllowsMultiple;

	private bool generationStarted;

	private int preparedRewardNodes;

	private int rewardGenerationDepth;

	private bool rewardCreatedInCurrentInvocation;

	internal IReadOnlyList<Pawn> ConsumedPawns => consumedPawns;

	internal int PreparedRewardNodes => preparedRewardNodes;

	internal QuestPawnInjectionScope(Dictionary<string, Queue<Pawn>> roles, Faction? threatFaction = null, int raidDelayTicks = 0, IEnumerable<PawnItemBlueprintMessage>? rewardItems = null, string? raidPawnKindsText = null)
	{
		if (current != null)
		{
			throw new InvalidOperationException("A quest pawn-injection scope is already active.");
		}
		this.roles = roles;
		this.threatFaction = threatFaction;
		this.raidDelayTicks = raidDelayTicks;
		this.rewardItems = ((rewardItems != null) ? rewardItems.ToList() : null) ?? new List<PawnItemBlueprintMessage>();
		this.raidPawnKindsText = raidPawnKindsText;
		current = this;
	}

	internal bool HasAttachedAuthoredReward(Quest quest)
	{
		if (authoredRewards.Count > 0)
		{
			return quest.PartsListForReading.OfType<QuestPart_Choice>().SelectMany((QuestPart_Choice part) => part.choices).SelectMany((Choice choice) => choice.rewards)
				.OfType<Reward_Items>()
				.Any((Reward_Items reward) => authoredRewards.Contains(reward));
		}
		return false;
	}

	internal void BeginGeneration()
	{
		generationStarted = true;
	}

	internal void SeedSlate(Slate slate)
	{
		foreach (KeyValuePair<string, Queue<Pawn>> role in roles)
		{
			Pawn[] pawns = role.Value.ToArray();
			if (pawns.Length == 1)
			{
				slate.Set<Pawn>(role.Key, pawns[0], false);
			}
			else if (pawns.Length > 1)
			{
				slate.Set<int>(role.Key + "Count", pawns.Length, false);
			}
		}
	}

	internal static void EnterNode(QuestNode_GeneratePawn node)
	{
		if (current != null)
		{
			object value = QuestGenSlateField.GetValue(null);
			Slate slate = (Slate)((value is Slate) ? value : null);
			if (slate != null)
			{
				string listKey = ResolveKey((QuestNode)(object)node, "addToList", slate);
				string storeKey = ResolveKey((QuestNode)(object)node, "storeAs", slate);
				ref string? reference = ref current.activeRole;
				string text;
				if (HasAvailable(listKey))
				{
					text = listKey;
				}
				else
				{
					text = (HasAvailable(storeKey) ? storeKey : ResolveKeys((QuestNode)(object)node, "addToLists", slate).FirstOrDefault(HasAvailable));
				}
				reference = text;
				current.activeRoleAllowsMultiple = false;
			}
		}
	}

	internal static void EnterNamedRole(string roleKey, bool multiple)
	{
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope != null && questPawnInjectionScope.generationStarted && HasAvailable(roleKey))
		{
			current.activeRole = roleKey;
			current.activeRoleAllowsMultiple = multiple;
		}
	}

	internal static void ExitNode()
	{
		if (current != null)
		{
			current.activeRole = null;
			current.activeRoleAllowsMultiple = false;
		}
	}

	internal static bool TryTakePawn(out Pawn pawn)
	{
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope == null || !questPawnInjectionScope.generationStarted || current.activeRole == null || !current.roles.TryGetValue(current.activeRole, out Queue<Pawn> queue) || queue.Count == 0)
		{
			pawn = null;
			return false;
		}
		pawn = queue.Dequeue();
		current.consumedPawns.Add(pawn);
		if (!current.activeRoleAllowsMultiple || queue.Count == 0)
		{
			current.activeRole = null;
			current.activeRoleAllowsMultiple = false;
		}
		return true;
	}

	internal static bool TryAssignGetPawn(QuestNode_GetPawn node)
	{
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope == null || !questPawnInjectionScope.generationStarted)
		{
			return false;
		}
		object value = QuestGenSlateField.GetValue(null);
		Slate slate = (Slate)((value is Slate) ? value : null);
		if (slate == null)
		{
			return false;
		}
		string key = ResolveKey((QuestNode)(object)node, "storeAs", slate);
		if (!TryTakeRole(key, out Pawn pawn))
		{
			return false;
		}
		slate.Set<Pawn>(key, pawn, false);
		return true;
	}

	internal static bool TryAssignAddToList(QuestNode_AddToList node)
	{
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope == null || !questPawnInjectionScope.generationStarted)
		{
			return false;
		}
		object value = QuestGenSlateField.GetValue(null);
		Slate slate = (Slate)((value is Slate) ? value : null);
		if (slate == null)
		{
			return false;
		}
		string key = ResolveKey((QuestNode)(object)node, "name", slate);
		if (!TryTakeRole(key, out Pawn pawn))
		{
			return false;
		}
		QuestGenUtility.AddToOrMakeList(slate, key, (object)pawn);
		return true;
	}

	internal static bool TryTakeNamedRole(string roleKey, out Pawn pawn)
	{
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope == null || !questPawnInjectionScope.generationStarted)
		{
			pawn = null;
			return false;
		}
		return TryTakeRole(roleKey, out pawn);
	}

	internal static void ApplyFactionOverride(QuestNode_GetFaction node)
	{
		if (current?.threatFaction == null)
		{
			return;
		}
		object value = QuestGenSlateField.GetValue(null);
		Slate slate = (Slate)((value is Slate) ? value : null);
		if (slate == null)
		{
			return;
		}
		string key = ResolveKey((QuestNode)(object)node, "storeAs", slate);
		if (key != null)
		{
			string factionKey = key;
			if (factionKey.IndexOf("enemyFaction", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				slate.Set<Faction>(factionKey, current.threatFaction, false);
			}
		}
	}

	internal static void ApplySetOverride(QuestNode_Set node)
	{
		if (current == null || current.raidDelayTicks <= 0)
		{
			return;
		}
		object value = QuestGenSlateField.GetValue(null);
		Slate slate = (Slate)((value is Slate) ? value : null);
		if (slate == null)
		{
			return;
		}
		string key = ResolveKey((QuestNode)(object)node, "name", slate);
		if (key != null)
		{
			string delayKey = key;
			if (delayKey.IndexOf("raidDelayTicks", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				slate.Set<int>(delayKey, current.raidDelayTicks, false);
			}
		}
	}

	internal static void BeginRewardGeneration()
	{
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope != null && questPawnInjectionScope.generationStarted && current.rewardItems.Count != 0)
		{
			current.rewardGenerationDepth++;
			current.rewardCreatedInCurrentInvocation = false;
		}
	}

	internal static void EndRewardGeneration()
	{
		if (current != null && current.rewardGenerationDepth != 0)
		{
			current.rewardGenerationDepth--;
			if (current.rewardGenerationDepth == 0)
			{
				current.rewardCreatedInCurrentInvocation = false;
			}
		}
	}

	internal static bool TryGenerateExactRewards(RewardsGeneratorParams parms, bool addDescriptionRules, Choice choice, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules, ref bool chosenPawnSignalUsed, ref List<Reward> result)
	{
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected Obj, but got Unknown
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Expected Obj, but got Unknown
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected Obj, but got Unknown
		QuestPawnInjectionScope? questPawnInjectionScope = current;
		if (questPawnInjectionScope == null || !questPawnInjectionScope.generationStarted || current.rewardGenerationDepth == 0 || current.rewardItems.Count == 0)
		{
			return false;
		}
		if (current.rewardCreatedInCurrentInvocation)
		{
			result = null;
			return true;
		}
		object value = QuestGenQuestField.GetValue(null);
		Quest quest = (Quest)((value is Quest) ? value : null);
		if (quest == null)
		{
			return false;
		}
		List<Thing> things = new List<Thing>();
		foreach (PawnItemBlueprintMessage blueprint in current.rewardItems)
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
		Reward_Items reward = new Reward_Items
		{
			items = things
		};
		current.authoredRewards.Add(reward);
		result = new List<Reward> { (Reward)(object)reward };
		choice.rewards.Add((Reward)(object)reward);
		foreach (QuestPart part in ((Reward)reward).GenerateQuestParts(0, parms, customLetterLabel, customLetterText, customLetterLabelRules, customLetterTextRules))
		{
			quest.AddPart(part);
			choice.questParts.Add(part);
		}
		if (addDescriptionRules)
		{
			string description = ((Reward)reward).GetDescription(parms);
			QuestGen.AddQuestDescriptionRules(new List<Rule>
			{
				(Rule)new Rule_String("allRewardsDescriptions", description),
				(Rule)new Rule_String("allRewardsDescriptionsExceptItems", string.Empty)
			});
		}
		chosenPawnSignalUsed = false;
		current.rewardCreatedInCurrentInvocation = true;
		current.preparedRewardNodes++;
		Log.Message($"[Storytell Yourself] Built native authored item reward ({things.Count} stack(s)) in RimWorld 1.6 reward pipeline.");
		return true;
	}

	internal static void ApplyRaidDescription()
	{
		if (!string.IsNullOrWhiteSpace(current?.raidPawnKindsText))
		{
			object value = QuestGenSlateField.GetValue(null);
			object obj = ((value is Slate) ? value : null);
			if (obj != null)
			{
				((Slate)obj).Set<string>("raidPawnKinds", current.raidPawnKindsText, false);
			}
		}
	}

	public void Dispose()
	{
		if (current == this)
		{
			current = null;
		}
	}

	private static bool HasAvailable(string? key)
	{
		if (key != null && current != null && current.roles.TryGetValue(key, out Queue<Pawn> queue))
		{
			return queue.Count > 0;
		}
		return false;
	}

	private static bool TryTakeRole(string? key, out Pawn pawn)
	{
		if (!HasAvailable(key))
		{
			pawn = null;
			return false;
		}
		pawn = current.roles[key].Dequeue();
		current.consumedPawns.Add(pawn);
		Log.Message("[Storytell Yourself] Assigned authored pawn '" + ((Entity)pawn).LabelShortCap + "' to quest role '" + key + "'.");
		return true;
	}

	private static string? ResolveKey(QuestNode node, string fieldName, Slate slate)
	{
		object reference = AccessTools.Field(((object)node).GetType(), fieldName)?.GetValue(node);
		return (reference?.GetType().GetMethod("GetValue", new Type[1] { typeof(Slate) }))?.Invoke(reference, new object[1] { slate }) as string;
	}

	private static IEnumerable<string> ResolveKeys(QuestNode node, string fieldName, Slate slate)
	{
		object reference = AccessTools.Field(((object)node).GetType(), fieldName)?.GetValue(node);
		return ((reference?.GetType().GetMethod("GetValue", new Type[1] { typeof(Slate) }))?.Invoke(reference, new object[1] { slate }) as IEnumerable<string>) ?? Array.Empty<string>();
	}
}
