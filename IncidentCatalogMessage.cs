using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class IncidentCatalogMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "incidentCatalog";

	[DataMember(Name = "sequence")]
	public long Sequence { get; set; }

	[DataMember(Name = "raidFactions")]
	public List<IncidentOptionMessage> RaidFactions { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "raidArrivalModes")]
	public List<IncidentOptionMessage> RaidArrivalModes { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "raidStrategies")]
	public List<IncidentOptionMessage> RaidStrategies { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "traderFactions")]
	public List<IncidentOptionMessage> TraderFactions { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "traderKinds")]
	public List<IncidentOptionMessage> TraderKinds { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "animalKinds")]
	public List<IncidentOptionMessage> AnimalKinds { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "simpleIncidents")]
	public List<IncidentOptionMessage> SimpleIncidents { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "questFactions")]
	public List<IncidentOptionMessage> QuestFactions { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "questTemplates")]
	public List<QuestTemplateMessage> QuestTemplates { get; set; } = new List<QuestTemplateMessage>();

	[DataMember(Name = "raidPawnKinds")]
	public List<IncidentOptionMessage> RaidPawnKinds { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "skills")]
	public List<IncidentOptionMessage> Skills { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "traits")]
	public List<IncidentOptionMessage> Traits { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "childhoodBackstories")]
	public List<IncidentOptionMessage> ChildhoodBackstories { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "adulthoodBackstories")]
	public List<IncidentOptionMessage> AdulthoodBackstories { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "xenotypes")]
	public List<IncidentOptionMessage> Xenotypes { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "bodyTypes")]
	public List<IncidentOptionMessage> BodyTypes { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "headTypes")]
	public List<IncidentOptionMessage> HeadTypes { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "hairStyles")]
	public List<IncidentOptionMessage> HairStyles { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "beards")]
	public List<IncidentOptionMessage> Beards { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "tattoos")]
	public List<IncidentOptionMessage> Tattoos { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "genes")]
	public List<IncidentOptionMessage> Genes { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "hediffs")]
	public List<IncidentOptionMessage> Hediffs { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "weapons")]
	public List<IncidentOptionMessage> Weapons { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "apparel")]
	public List<IncidentOptionMessage> Apparel { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "inventoryItems")]
	public List<IncidentOptionMessage> InventoryItems { get; set; } = new List<IncidentOptionMessage>();

	[DataMember(Name = "stuffs")]
	public List<IncidentOptionMessage> Stuffs { get; set; } = new List<IncidentOptionMessage>();

	public IncidentCatalogMessage Copy()
	{
		return new IncidentCatalogMessage
		{
			Sequence = Sequence,
			RaidFactions = CopyOptions(RaidFactions),
			RaidArrivalModes = CopyOptions(RaidArrivalModes),
			RaidStrategies = CopyOptions(RaidStrategies),
			TraderFactions = CopyOptions(TraderFactions),
			TraderKinds = CopyOptions(TraderKinds),
			AnimalKinds = CopyOptions(AnimalKinds),
			SimpleIncidents = CopyOptions(SimpleIncidents),
			QuestFactions = CopyOptions(QuestFactions),
			QuestTemplates = QuestTemplates.ConvertAll((QuestTemplateMessage template) => template.Copy()),
			RaidPawnKinds = CopyOptions(RaidPawnKinds),
			Skills = CopyOptions(Skills),
			Traits = CopyOptions(Traits),
			ChildhoodBackstories = CopyOptions(ChildhoodBackstories),
			AdulthoodBackstories = CopyOptions(AdulthoodBackstories),
			Xenotypes = CopyOptions(Xenotypes),
			BodyTypes = CopyOptions(BodyTypes),
			HeadTypes = CopyOptions(HeadTypes),
			HairStyles = CopyOptions(HairStyles),
			Beards = CopyOptions(Beards),
			Tattoos = CopyOptions(Tattoos),
			Genes = CopyOptions(Genes),
			Hediffs = CopyOptions(Hediffs),
			Weapons = CopyOptions(Weapons),
			Apparel = CopyOptions(Apparel),
			InventoryItems = CopyOptions(InventoryItems),
			Stuffs = CopyOptions(Stuffs)
		};
	}

	private static List<IncidentOptionMessage> CopyOptions(List<IncidentOptionMessage> source)
	{
		List<IncidentOptionMessage> list = new List<IncidentOptionMessage>(source.Count);
		foreach (IncidentOptionMessage item in source)
		{
			list.Add(item.Copy());
		}
		return list;
	}
}
