using Verse;

namespace StorytellYourself;

internal sealed class QuestRewardRecord : IExposable
{
	internal int QuestId;

	internal string ItemsJson = string.Empty;

	internal bool Delivered;

	public void ExposeData()
	{
		Scribe_Values.Look<int>(ref QuestId, "questId", 0, false);
		Scribe_Values.Look<string>(ref ItemsJson, "itemsJson", string.Empty, false);
		Scribe_Values.Look<bool>(ref Delivered, "delivered", false, false);
	}
}
