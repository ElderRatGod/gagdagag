using System;
using HarmonyLib;
using RimWorld.QuestGen;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestNode_Root_OrbitalFugitive), "RunInt")]
internal static class QuestOrbitalFugitiveCastPatch
{
	private static void Prefix()
	{
		QuestPawnInjectionScope.EnterNamedRole("fugitive", multiple: false);
	}

	private static Exception? Finalizer(Exception? __exception)
	{
		QuestPawnInjectionScope.ExitNode();
		return __exception;
	}
}
