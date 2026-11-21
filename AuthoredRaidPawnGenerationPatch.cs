using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StorytellYourself;

[HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
internal static class AuthoredRaidPawnGenerationPatch
{
	private static bool Prefix(ref IEnumerable<Pawn> __result)
	{
		if (!AuthoredPawnGenerationScope.TryTakeRaid(out IEnumerable<Pawn> pawns))
		{
			return true;
		}
		__result = pawns;
		return false;
	}
}
