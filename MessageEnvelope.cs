using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class MessageEnvelope
{
	[DataMember(Name = "type")]
	public string? Type { get; set; }
}
