using HarmonyLib;
using RimWorld;

namespace StorytellYourself;

[HarmonyPatch(typeof(Storyteller), "TryFire")]
internal static class AiStorytellerControlPatch
{
	private static bool Prefix(FiringIncident fi, ref bool __result)
	{
		if (fi.sourceQuestPart != null)
		{
			return true;
		}
		string mode = StorytellYourselfMod.CurrentSettings?.AiStorytellerMode ?? "concurrent";
		if (mode == "concurrent")
		{
			return true;
		}
		if (mode == "questsOnly" && IsQuestIncident(fi.def))
		{
			return true;
		}
		__result = false;
		return false;
	}

	private static bool IsQuestIncident(IncidentDef incident)
	{
		if (incident.category != IncidentCategoryDefOf.GiveQuest)
		{
			return incident.Worker is IncidentWorker_GiveQuest;
		}
		return true;
	}
}
