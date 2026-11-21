using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class HelloMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "hello";

	[DataMember(Name = "protocolVersion")]
	public int ProtocolVersion { get; set; }

	[DataMember(Name = "clientName")]
	public string ClientName { get; set; } = string.Empty;

	[DataMember(Name = "clientVersion")]
	public string ClientVersion { get; set; } = string.Empty;

	[DataMember(Name = "pairingCode", EmitDefaultValue = false)]
	public string? PairingCode { get; set; }
}
