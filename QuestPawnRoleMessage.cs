using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class QuestPawnRoleMessage
{
	[DataMember(Name = "key")]
	public string Key { get; set; } = string.Empty;

	[DataMember(Name = "label")]
	public string Label { get; set; } = string.Empty;

	[DataMember(Name = "multiple")]
	public bool Multiple { get; set; }

	public QuestPawnRoleMessage Copy()
	{
		return new QuestPawnRoleMessage
		{
			Key = Key,
			Label = Label,
			Multiple = Multiple
		};
	}
}
