using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class CommandResultMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "commandResult";

	[DataMember(Name = "requestId", EmitDefaultValue = false)]
	public string? RequestId { get; set; }

	[DataMember(Name = "accepted")]
	public bool Accepted { get; set; }

	[DataMember(Name = "duplicate")]
	public bool Duplicate { get; set; }

	[DataMember(Name = "errorCode", EmitDefaultValue = false)]
	public string? ErrorCode { get; set; }

	[DataMember(Name = "message")]
	public string Message { get; set; } = string.Empty;

	public CommandResultMessage AsDuplicate()
	{
		return new CommandResultMessage
		{
			RequestId = RequestId,
			Accepted = Accepted,
			Duplicate = true,
			ErrorCode = ErrorCode,
			Message = Message
		};
	}
}
