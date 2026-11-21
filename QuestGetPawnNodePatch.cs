using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_GetPawn), "RunInt")]
internal static class QuestGetPawnNodePatch
{
	private static bool Prefix(QuestNode_GetPawn __instance)
	{
		return !QuestPawnInjectionScope.TryAssignGetPawn(__instance);
	}
}
