using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnItemBlueprintMessage
{
	[DataMember(Name = "thingDef")]
	public string ThingDef { get; set; } = string.Empty;

	[DataMember(Name = "stuffDef", EmitDefaultValue = false)]
	public string? StuffDef { get; set; }

	[DataMember(Name = "count")]
	public int Count { get; set; } = 1;

	[DataMember(Name = "quality")]
	public int Quality { get; set; } = 2;

	[DataMember(Name = "hitPointsPercent")]
	public float HitPointsPercent { get; set; } = 1f;
}
