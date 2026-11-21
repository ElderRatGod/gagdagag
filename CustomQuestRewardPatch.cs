using HarmonyLib;
using RimWorld;
using Verse;

namespace StorytellYourself;

[HarmonyPatch(typeof(Quest), "End")]
internal static class CustomQuestRewardPatch
{
	private static void Postfix(Quest __instance, QuestEndOutcome outcome)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Game game = Current.Game;
		if (game != null)
		{
			game.GetComponent<QuestRaidRegistryComponent>()?.DeliverRewards(__instance, outcome);
		}
	}
}
