using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class HelloAckMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "helloAck";

	[DataMember(Name = "protocolVersion")]
	public int ProtocolVersion { get; set; }

	[DataMember(Name = "serverVersion")]
	public string ServerVersion { get; set; } = string.Empty;

	[DataMember(Name = "sessionId")]
	public string SessionId { get; set; } = string.Empty;
}
