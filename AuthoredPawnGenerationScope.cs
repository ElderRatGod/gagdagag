using System;
using System.Collections.Generic;
using Verse;

namespace StorytellYourself;

internal sealed class AuthoredPawnGenerationScope : IDisposable
{
	[ThreadStatic]
	private static AuthoredPawnGenerationScope? current;

	private readonly string mode;

	private readonly List<Pawn> pawns;

	private bool consumed;

	private AuthoredPawnGenerationScope(string mode, IEnumerable<Pawn> pawns)
	{
		if (current != null)
		{
			throw new InvalidOperationException("An authored pawn generation scope is already active.");
		}
		this.mode = mode;
		this.pawns = new List<Pawn>(pawns);
		current = this;
	}

	internal static AuthoredPawnGenerationScope BeginRaid(IEnumerable<Pawn> pawns)
	{
		return new AuthoredPawnGenerationScope("raid", pawns);
	}

	internal static bool TryTakeRaid(out IEnumerable<Pawn> result)
	{
		if (current == null || current.mode != "raid" || current.consumed)
		{
			result = null;
			return false;
		}
		current.consumed = true;
		result = new List<Pawn>(current.pawns);
		return true;
	}

	public void Dispose()
	{
		if (current == this)
		{
			current = null;
		}
	}
}
