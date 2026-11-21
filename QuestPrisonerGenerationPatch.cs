using HarmonyLib;
using RimWorld;
using Verse;

namespace StorytellYourself;

[HarmonyPatch(typeof(PrisonerWillingToJoinQuestUtility), "GeneratePrisoner")]
internal static class QuestPrisonerGenerationPatch
{
	private static bool Prefix(ref Pawn __result)
	{
		if (!QuestPawnInjectionScope.TryTakeNamedRole("prisoner", out Pawn pawn))
		{
			return true;
		}
		__result = pawn;
		return false;
	}
}
