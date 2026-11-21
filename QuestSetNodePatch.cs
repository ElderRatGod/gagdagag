using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_Set), "RunInt")]
internal static class QuestSetNodePatch
{
	private static void Postfix(QuestNode_Set __instance)
	{
		QuestPawnInjectionScope.ApplySetOverride(__instance);
	}
}
