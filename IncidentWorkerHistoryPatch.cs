using HarmonyLib;
using RimWorld;

namespace StorytellYourself;

[HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
internal static class IncidentWorkerHistoryPatch
{
	private static void Postfix(IncidentWorker __instance, bool __result)
	{
		if (__result)
		{
			IncidentHistory.Record(__instance.def);
		}
	}
}
