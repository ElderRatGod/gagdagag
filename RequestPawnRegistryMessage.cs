using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class RequestPawnRegistryMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "requestPawnRegistry";
}
