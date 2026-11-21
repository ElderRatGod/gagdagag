using System;
using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_Root_Bossgroup), "RunInt")]
internal static class QuestBossgroupCastPatch
{
	private static void Prefix()
	{
		QuestPawnInjectionScope.EnterNamedRole("escortees", multiple: true);
	}

	private static Exception? Finalizer(Exception? __exception)
	{
		QuestPawnInjectionScope.ExitNode();
		return __exception;
	}
}
