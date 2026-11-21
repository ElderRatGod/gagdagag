using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class IncidentOptionMessage
{
	[DataMember(Name = "id")]
	public string Id { get; set; } = string.Empty;

	[DataMember(Name = "label")]
	public string Label { get; set; } = string.Empty;

	[DataMember(Name = "parentId", EmitDefaultValue = false)]
	public string? ParentId { get; set; }

	[DataMember(Name = "detail", EmitDefaultValue = false)]
	public string? Detail { get; set; }

	[DataMember(Name = "techLevel", EmitDefaultValue = false)]
	public string? TechLevel { get; set; }

	[DataMember(Name = "value")]
	public float Value { get; set; }

	public IncidentOptionMessage Copy()
	{
		return new IncidentOptionMessage
		{
			Id = Id,
			Label = Label,
			ParentId = ParentId,
			Detail = Detail,
			TechLevel = TechLevel,
			Value = Value
		};
	}
}
