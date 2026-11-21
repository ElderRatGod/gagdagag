using System;
using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_GeneratePawn), "RunInt")]
internal static class QuestGeneratePawnNodePatch
{
	private static void Prefix(QuestNode_GeneratePawn __instance)
	{
		QuestPawnInjectionScope.EnterNode(__instance);
	}

	private static Exception? Finalizer(Exception? __exception)
	{
		QuestPawnInjectionScope.ExitNode();
		return __exception;
	}
}
