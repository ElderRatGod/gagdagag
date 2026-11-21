using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_GetFaction), "RunInt")]
internal static class QuestGetFactionNodePatch
{
	private static void Postfix(QuestNode_GetFaction __instance)
	{
		QuestPawnInjectionScope.ApplyFactionOverride(__instance);
	}
}
