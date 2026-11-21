using System;
using System.Collections.Generic;
using Verse;

namespace StorytellYourself;

internal sealed class QuestRaidRecord : IExposable
{
	internal int QuestId;

	internal string InSignal = string.Empty;

	internal List<Pawn> Pawns = new List<Pawn>();

	internal bool Triggered;

	public void ExposeData()
	{
		Scribe_Values.Look<int>(ref QuestId, "questId", 0, false);
		Scribe_Values.Look<string>(ref InSignal, "inSignal", string.Empty, false);
		Scribe_Collections.Look<Pawn>(ref Pawns, "pawns", (LookMode)3, Array.Empty<object>());
		Scribe_Values.Look<bool>(ref Triggered, "triggered", false, false);
		if (Pawns == null)
		{
			Pawns = new List<Pawn>();
		}
	}
}
