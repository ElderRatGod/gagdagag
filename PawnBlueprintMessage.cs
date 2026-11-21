using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnBlueprintMessage
{
	[DataMember(Name = "blueprintId")]
	public string BlueprintId { get; set; } = string.Empty;

	[DataMember(Name = "revision")]
	public int Revision { get; set; }

	[DataMember(Name = "name")]
	public string Name { get; set; } = string.Empty;

	[DataMember(Name = "archived")]
	public bool Archived { get; set; }

	[DataMember(Name = "useGeneratedLoadout")]
	public bool UseGeneratedLoadout { get; set; } = true;

	[DataMember(Name = "pawnKindDef")]
	public string PawnKindDef { get; set; } = "Colonist";

	[DataMember(Name = "factionId", EmitDefaultValue = false)]
	public string? FactionId { get; set; }

	[DataMember(Name = "gender")]
	public string Gender { get; set; } = "none";

	[DataMember(Name = "biologicalAgeYears")]
	public float BiologicalAgeYears { get; set; } = 25f;

	[DataMember(Name = "xenotypeDef", EmitDefaultValue = false)]
	public string? XenotypeDef { get; set; }

	[DataMember(Name = "childhoodBackstory", EmitDefaultValue = false)]
	public string? ChildhoodBackstory { get; set; }

	[DataMember(Name = "adulthoodBackstory", EmitDefaultValue = false)]
	public string? AdulthoodBackstory { get; set; }

	[DataMember(Name = "bodyTypeDef", EmitDefaultValue = false)]
	public string? BodyTypeDef { get; set; }

	[DataMember(Name = "headTypeDef", EmitDefaultValue = false)]
	public string? HeadTypeDef { get; set; }

	[DataMember(Name = "hairDef", EmitDefaultValue = false)]
	public string? HairDef { get; set; }

	[DataMember(Name = "beardDef", EmitDefaultValue = false)]
	public string? BeardDef { get; set; }

	[DataMember(Name = "faceTattooDef", EmitDefaultValue = false)]
	public string? FaceTattooDef { get; set; }

	[DataMember(Name = "bodyTattooDef", EmitDefaultValue = false)]
	public string? BodyTattooDef { get; set; }

	[DataMember(Name = "hairColor", EmitDefaultValue = false)]
	public string? HairColor { get; set; }

	[DataMember(Name = "skinColor", EmitDefaultValue = false)]
	public string? SkinColor { get; set; }

	[DataMember(Name = "skills")]
	public List<PawnSkillBlueprintMessage> Skills { get; set; } = new List<PawnSkillBlueprintMessage>();

	[DataMember(Name = "traits")]
	public List<PawnTraitBlueprintMessage> Traits { get; set; } = new List<PawnTraitBlueprintMessage>();

	[DataMember(Name = "geneDefs")]
	public List<string> GeneDefs { get; set; } = new List<string>();

	[DataMember(Name = "hediffDefs")]
	public List<string> HediffDefs { get; set; } = new List<string>();

	[DataMember(Name = "equipment")]
	public List<PawnItemBlueprintMessage> Equipment { get; set; } = new List<PawnItemBlueprintMessage>();

	[DataMember(Name = "apparel")]
	public List<PawnItemBlueprintMessage> Apparel { get; set; } = new List<PawnItemBlueprintMessage>();

	[DataMember(Name = "inventory")]
	public List<PawnItemBlueprintMessage> Inventory { get; set; } = new List<PawnItemBlueprintMessage>();
}
