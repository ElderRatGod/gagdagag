using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class UpsertPawnBlueprintMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "upsertPawnBlueprint";

	[DataMember(Name = "requestId")]
	public string RequestId { get; set; } = string.Empty;

	[DataMember(Name = "blueprint")]
	public PawnBlueprintMessage Blueprint { get; set; } = new PawnBlueprintMessage();
}
