using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class TriggerIncidentMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "triggerIncident";

	[DataMember(Name = "requestId")]
	public string RequestId { get; set; } = string.Empty;

	[DataMember(Name = "incident")]
	public string Incident { get; set; } = string.Empty;

	[DataMember(Name = "points")]
	public float Points { get; set; }

	[DataMember(Name = "factionId", EmitDefaultValue = false)]
	public string? FactionId { get; set; }

	[DataMember(Name = "arrivalMode", EmitDefaultValue = false)]
	public string? ArrivalMode { get; set; }

	[DataMember(Name = "strategy", EmitDefaultValue = false)]
	public string? Strategy { get; set; }

	[DataMember(Name = "traderKind", EmitDefaultValue = false)]
	public string? TraderKind { get; set; }

	[DataMember(Name = "pawnKind", EmitDefaultValue = false)]
	public string? PawnKind { get; set; }

	[DataMember(Name = "incidentDef", EmitDefaultValue = false)]
	public string? IncidentDef { get; set; }

	[DataMember(Name = "authoredPawnIds")]
	public List<string> AuthoredPawnIds { get; set; } = new List<string>();

	[DataMember(Name = "authoredPawnRole", EmitDefaultValue = false)]
	public string? AuthoredPawnRole { get; set; }

	[DataMember(Name = "questTemplateDef", EmitDefaultValue = false)]
	public string? QuestTemplateDef { get; set; }

	[DataMember(Name = "questExpiryDays")]
	public float QuestExpiryDays { get; set; }

	[DataMember(Name = "questAutoAccept")]
	public bool QuestAutoAccept { get; set; }

	[DataMember(Name = "questTitle", EmitDefaultValue = false)]
	public string? QuestTitle { get; set; }

	[DataMember(Name = "questDescription", EmitDefaultValue = false)]
	public string? QuestDescription { get; set; }

	[DataMember(Name = "questCastAssignments")]
	public List<QuestCastAssignmentMessage> QuestCastAssignments { get; set; } = new List<QuestCastAssignmentMessage>();

	[DataMember(Name = "questRaidPawnIds")]
	public List<string> QuestRaidPawnIds { get; set; } = new List<string>();

	[DataMember(Name = "questRaidPoints")]
	public float QuestRaidPoints { get; set; }

	[DataMember(Name = "questRaidFactionId", EmitDefaultValue = false)]
	public string? QuestRaidFactionId { get; set; }

	[DataMember(Name = "questRaidArrivalMode", EmitDefaultValue = false)]
	public string? QuestRaidArrivalMode { get; set; }

	[DataMember(Name = "questRaidStrategy", EmitDefaultValue = false)]
	public string? QuestRaidStrategy { get; set; }

	[DataMember(Name = "questRaidDelayHours")]
	public float QuestRaidDelayHours { get; set; }

	[DataMember(Name = "questRewardItems")]
	public List<PawnItemBlueprintMessage> QuestRewardItems { get; set; } = new List<PawnItemBlueprintMessage>();
}
