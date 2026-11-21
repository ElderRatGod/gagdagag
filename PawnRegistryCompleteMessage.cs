using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnRegistryCompleteMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "pawnRegistryComplete";

	[DataMember(Name = "count")]
	public int Count { get; set; }
}
