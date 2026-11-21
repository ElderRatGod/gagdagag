using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class PawnSkillBlueprintMessage
{
	[DataMember(Name = "skillDef")]
	public string SkillDef { get; set; } = string.Empty;

	[DataMember(Name = "level")]
	public int Level { get; set; }

	[DataMember(Name = "passion")]
	public string Passion { get; set; } = "none";
}
