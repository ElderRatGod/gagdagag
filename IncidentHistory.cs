using System;
using System.Collections.Generic;
using RimWorld;
using StorytellYourself.Protocol;
using Verse;

namespace StorytellYourself;

internal static class IncidentHistory
{
	private sealed class HumanCommandScope : IDisposable
	{
		private bool disposed;

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				humanCommandDepth--;
			}
		}
	}

	private const int MaximumEntries = 12;

	private static readonly List<RecentIncidentMessage> entries = new List<RecentIncidentMessage>();

	private static Game? trackedGame;

	private static int humanCommandDepth;

	internal static void ResetIfGameChanged(Game? game)
	{
		if (trackedGame != game)
		{
			trackedGame = game;
			entries.Clear();
			humanCommandDepth = 0;
		}
	}

	internal static IDisposable BeginHumanCommand()
	{
		humanCommandDepth++;
		return new HumanCommandScope();
	}

	internal static List<RecentIncidentMessage> GetRecent()
	{
		List<RecentIncidentMessage> result = new List<RecentIncidentMessage>(entries.Count);
		for (int index = entries.Count - 1; index >= 0; index--)
		{
			result.Add(entries[index].Copy());
		}
		return result;
	}

	internal static void Record(IncidentDef? incidentDef)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (incidentDef != null && Current.Game != null)
		{
			string label = ((object)((Def)incidentDef).LabelCap/*cast due to constrained. prefix*/).ToString();
			List<RecentIncidentMessage> list = entries;
			RecentIncidentMessage recentIncidentMessage = new RecentIncidentMessage();
			TickManager tickManager = Find.TickManager;
			recentIncidentMessage.GameTick = ((tickManager != null) ? tickManager.TicksGame : 0);
			recentIncidentMessage.DefName = ((Def)incidentDef).defName;
			recentIncidentMessage.Label = (string.IsNullOrWhiteSpace(label) ? ((Def)incidentDef).defName : label);
			recentIncidentMessage.Source = ((humanCommandDepth > 0) ? "human" : "ai");
			list.Add(recentIncidentMessage);
			while (entries.Count > 12)
			{
				entries.RemoveAt(0);
			}
		}
	}
}
