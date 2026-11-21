using HarmonyLib;
using RimWorld;
using Verse;

namespace StorytellYourself;

[HarmonyPatch(typeof(DownedRefugeeQuestUtility), "GenerateRefugee")]
internal static class QuestRefugeeGenerationPatch
{
	private static bool Prefix(ref Pawn __result)
	{
		if (!QuestPawnInjectionScope.TryTakeNamedRole("refugee", out Pawn pawn))
		{
			return true;
		}
		__result = pawn;
		return false;
	}
}
