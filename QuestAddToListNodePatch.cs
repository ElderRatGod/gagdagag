using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_AddToList), "RunInt")]
internal static class QuestAddToListNodePatch
{
	private static bool Prefix(QuestNode_AddToList __instance)
	{
		return !QuestPawnInjectionScope.TryAssignAddToList(__instance);
	}
}
