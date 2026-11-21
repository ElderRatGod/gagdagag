using System;
using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestGen_Rewards), "GiveRewards")]
internal static class QuestGiveRewardsPipelinePatch
{
	private static void Prefix()
	{
		QuestPawnInjectionScope.BeginRewardGeneration();
	}

	private static Exception? Finalizer(Exception? __exception)
	{
		QuestPawnInjectionScope.EndRewardGeneration();
		return __exception;
	}
}
