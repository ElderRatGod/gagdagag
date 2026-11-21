using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StorytellYourself;

[HarmonyPatch(typeof(QuestPart_Incident), "Notify_QuestSignalReceived")]
internal static class AuthoredQuestRaidPatch
{
	private static void Prefix(QuestPart_Incident __instance, Signal signal, out QuestRaidExecutionState? __state)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		__state = null;
		Game game = Current.Game;
		if (game != null)
		{
			game.GetComponent<QuestRaidRegistryComponent>()?.TryBegin(__instance, signal, out __state);
		}
	}

	private static Exception? Finalizer(Exception? __exception, QuestRaidExecutionState? __state)
	{
		if (__state == null)
		{
			return __exception;
		}
		__state.Scope.Dispose();
		Game game = Current.Game;
		AuthoredPawnRegistryComponent registry = ((game != null) ? game.GetComponent<AuthoredPawnRegistryComponent>() : null);
		if (GenCollection.Any<Pawn>(__state.Pawns, (Predicate<Pawn>)((Pawn pawn) => ((Thing)pawn).Spawned)))
		{
			registry?.MarkActive(__state.Pawns);
		}
		else
		{
			AuthoredPawnIncidentExecutor.ReturnToWorld(__state.Pawns);
		}
		return __exception;
	}
}
