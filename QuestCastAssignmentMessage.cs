using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class QuestCastAssignmentMessage
{
	[DataMember(Name = "roleKey")]
	public string RoleKey { get; set; } = string.Empty;

	[DataMember(Name = "pawnIds")]
	public List<string> PawnIds { get; set; } = new List<string>();

	public QuestCastAssignmentMessage Copy()
	{
		return new QuestCastAssignmentMessage
		{
			RoleKey = RoleKey,
			PawnIds = new List<string>(PawnIds)
		};
	}
}
