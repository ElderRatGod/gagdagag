using System;
using HarmonyLib;
using Verse;

namespace StorytellYourself;

[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
internal static class QuestPawnGeneratorPatch
{
	private static bool Prefix(ref Pawn __result)
	{
		if (!QuestPawnInjectionScope.TryTakePawn(out Pawn pawn))
		{
			return true;
		}
		__result = pawn;
		return false;
	}
}
