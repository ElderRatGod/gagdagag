using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class QuestTemplateMessage
{
	[DataMember(Name = "id")]
	public string Id { get; set; } = string.Empty;

	[DataMember(Name = "label")]
	public string Label { get; set; } = string.Empty;

	[DataMember(Name = "source")]
	public string Source { get; set; } = string.Empty;

	[DataMember(Name = "minimumPoints")]
	public float MinimumPoints { get; set; }

	[DataMember(Name = "randomlySelectable")]
	public bool RandomlySelectable { get; set; }

	[DataMember(Name = "pawnRoles")]
	public List<QuestPawnRoleMessage> PawnRoles { get; set; } = new List<QuestPawnRoleMessage>();

	[DataMember(Name = "hasRaid")]
	public bool HasRaid { get; set; }

	[DataMember(Name = "hasGeneratedRewards")]
	public bool HasGeneratedRewards { get; set; }

	public QuestTemplateMessage Copy()
	{
		return new QuestTemplateMessage
		{
			Id = Id,
			Label = Label,
			Source = Source,
			MinimumPoints = MinimumPoints,
			RandomlySelectable = RandomlySelectable,
			HasRaid = HasRaid,
			HasGeneratedRewards = HasGeneratedRewards,
			PawnRoles = PawnRoles.ConvertAll((QuestPawnRoleMessage role) => role.Copy())
		};
	}
}
