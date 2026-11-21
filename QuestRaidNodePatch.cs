using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_Raid), "RunInt")]
internal static class QuestRaidNodePatch
{
	private static void Postfix()
	{
		QuestPawnInjectionScope.ApplyRaidDescription();
	}
}
