using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace StorytellYourself;

[HarmonyPatch]
internal static class RootUpdatePatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		MethodInfo entryUpdate = AccessTools.Method(typeof(Root_Entry), "Update", (Type[])null, (Type[])null);
		MethodInfo playUpdate = AccessTools.Method(typeof(Root_Play), "Update", (Type[])null, (Type[])null);
		if (entryUpdate != null)
		{
			yield return entryUpdate;
		}
		if (playUpdate != null && playUpdate != entryUpdate)
		{
			yield return playUpdate;
		}
	}

	private static void Postfix()
	{
		HumanStorytellerRuntime.Update();
	}
}
