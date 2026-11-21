using System;
using System.Collections.Generic;
using Verse;

namespace StorytellYourself;

internal sealed class QuestRaidExecutionState
{
	internal List<Pawn> Pawns { get; }

	internal IDisposable Scope { get; }

	internal QuestRaidExecutionState(List<Pawn> pawns, IDisposable scope)
	{
		Pawns = pawns;
		Scope = scope;
	}
}
