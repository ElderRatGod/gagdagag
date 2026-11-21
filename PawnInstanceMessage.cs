using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnInstanceMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "pawnInstance";

	[DataMember(Name = "blueprintId")]
	public string BlueprintId { get; set; } = string.Empty;

	[DataMember(Name = "revision")]
	public int Revision { get; set; }

	[DataMember(Name = "state")]
	public string State { get; set; } = "unspawned";

	[DataMember(Name = "pawnThingId", EmitDefaultValue = false)]
	public string? PawnThingId { get; set; }

	[DataMember(Name = "name")]
	public string Name { get; set; } = string.Empty;

	[DataMember(Name = "faction", EmitDefaultValue = false)]
	public string? Faction { get; set; }

	[DataMember(Name = "location", EmitDefaultValue = false)]
	public string? Location { get; set; }

	[DataMember(Name = "deathTick")]
	public int DeathTick { get; set; } = -1;
}
