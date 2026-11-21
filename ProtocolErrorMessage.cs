using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class ProtocolErrorMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "protocolError";

	[DataMember(Name = "requestId", EmitDefaultValue = false)]
	public string? RequestId { get; set; }

	[DataMember(Name = "errorCode")]
	public string ErrorCode { get; set; } = "invalid_request";

	[DataMember(Name = "message")]
	public string Message { get; set; } = string.Empty;

	[DataMember(Name = "supportedProtocolVersion")]
	public int SupportedProtocolVersion { get; set; } = 4;
}
