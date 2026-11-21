using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse.Grammar;

namespace StorytellYourself;

[HarmonyPatch]
internal static class QuestGenerateRewardsPipelinePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(QuestGen_Rewards), "GenerateRewards", (Type[])null, (Type[])null);
	}

	private static bool Prefix(RewardsGeneratorParams parmsResolved, bool addDescriptionRules, ref bool chosenPawnSignalUsed, Choice choice, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules, ref List<Reward> __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return !QuestPawnInjectionScope.TryGenerateExactRewards(parmsResolved, addDescriptionRules, choice, customLetterLabel, customLetterText, customLetterLabelRules, customLetterTextRules, ref chosenPawnSignalUsed, ref __result);
	}
}
