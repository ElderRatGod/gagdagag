using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnBlueprintResultMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "pawnBlueprintResult";

	[DataMember(Name = "requestId")]
	public string RequestId { get; set; } = string.Empty;

	[DataMember(Name = "blueprintId")]
	public string BlueprintId { get; set; } = string.Empty;

	[DataMember(Name = "accepted")]
	public bool Accepted { get; set; }

	[DataMember(Name = "errorCode", EmitDefaultValue = false)]
	public string? ErrorCode { get; set; }

	[DataMember(Name = "message")]
	public string Message { get; set; } = string.Empty;
}
