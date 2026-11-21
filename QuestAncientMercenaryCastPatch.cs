using System;
using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_Root_AncientMercenaries), "RunInt")]
internal static class QuestAncientMercenaryCastPatch
{
	private static void Prefix()
	{
		QuestPawnInjectionScope.EnterNamedRole("LEADER", multiple: false);
	}

	private static Exception? Finalizer(Exception? __exception)
	{
		QuestPawnInjectionScope.ExitNode();
		return __exception;
	}
}
