using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class GameStateMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "gameState";

	[DataMember(Name = "sequence")]
	public long Sequence { get; set; }

	[DataMember(Name = "state")]
	public string State { get; set; } = "mainMenu";

	[DataMember(Name = "colonyName", EmitDefaultValue = false)]
	public string? ColonyName { get; set; }

	[DataMember(Name = "saveName", EmitDefaultValue = false)]
	public string? SaveName { get; set; }

	[DataMember(Name = "paused")]
	public bool Paused { get; set; }

	public GameStateMessage Copy()
	{
		return new GameStateMessage
		{
			Sequence = Sequence,
			State = State,
			ColonyName = ColonyName,
			SaveName = SaveName,
			Paused = Paused
		};
	}

	public bool SameStateAs(GameStateMessage other)
	{
		if (State == other.State && ColonyName == other.ColonyName && SaveName == other.SaveName)
		{
			return Paused == other.Paused;
		}
		return false;
	}
}
