using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class RecentIncidentMessage
{
	[DataMember(Name = "gameTick")]
	public int GameTick { get; set; }

	[DataMember(Name = "defName")]
	public string DefName { get; set; } = string.Empty;

	[DataMember(Name = "label")]
	public string Label { get; set; } = string.Empty;

	[DataMember(Name = "source")]
	public string Source { get; set; } = "ai";

	public RecentIncidentMessage Copy()
	{
		return new RecentIncidentMessage
		{
			GameTick = GameTick,
			DefName = DefName,
			Label = Label,
			Source = Source
		};
	}
}
