using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnTraitBlueprintMessage
{
	[DataMember(Name = "traitDef")]
	public string TraitDef { get; set; } = string.Empty;

	[DataMember(Name = "degree")]
	public int Degree { get; set; }
}
