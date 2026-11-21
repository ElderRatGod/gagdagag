using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorytellYourself.Protocol;

[DataContract]
public sealed class ColonySnapshotMessage
{
	[DataMember(Name = "type")]
	public string Type { get; set; } = "colonySnapshot";

	[DataMember(Name = "sequence")]
	public long Sequence { get; set; }

	[DataMember(Name = "gameTick")]
	public int GameTick { get; set; }

	[DataMember(Name = "colonyName")]
	public string ColonyName { get; set; } = string.Empty;

	[DataMember(Name = "mapName")]
	public string MapName { get; set; } = string.Empty;

	[DataMember(Name = "isPlayerHome")]
	public bool IsPlayerHome { get; set; }

	[DataMember(Name = "colonists")]
	public int Colonists { get; set; }

	[DataMember(Name = "prisoners")]
	public int Prisoners { get; set; }

	[DataMember(Name = "guests")]
	public int Guests { get; set; }

	[DataMember(Name = "colonyAnimals")]
	public int ColonyAnimals { get; set; }

	[DataMember(Name = "wealthTotal")]
	public float WealthTotal { get; set; }

	[DataMember(Name = "wealthItems")]
	public float WealthItems { get; set; }

	[DataMember(Name = "wealthBuildings")]
	public float WealthBuildings { get; set; }

	[DataMember(Name = "wealthPawns")]
	public float WealthPawns { get; set; }

	[DataMember(Name = "foodNutrition")]
	public float FoodNutrition { get; set; }

	[DataMember(Name = "mealCount")]
	public int MealCount { get; set; }

	[DataMember(Name = "medicineCount")]
	public int MedicineCount { get; set; }

	[DataMember(Name = "silver")]
	public int Silver { get; set; }

	[DataMember(Name = "steel")]
	public int Steel { get; set; }

	[DataMember(Name = "wood")]
	public int Wood { get; set; }

	[DataMember(Name = "components")]
	public int Components { get; set; }

	[DataMember(Name = "advancedComponents")]
	public int AdvancedComponents { get; set; }

	[DataMember(Name = "averageMood")]
	public float AverageMood { get; set; }

	[DataMember(Name = "moodPawnCount")]
	public int MoodPawnCount { get; set; }

	[DataMember(Name = "hostilePawns")]
	public int HostilePawns { get; set; }

	[DataMember(Name = "powerNetCount")]
	public int PowerNetCount { get; set; }

	[DataMember(Name = "storedEnergyWattDays")]
	public float StoredEnergyWattDays { get; set; }

	[DataMember(Name = "netPowerGainWatts")]
	public float NetPowerGainWatts { get; set; }

	[DataMember(Name = "collectionMilliseconds")]
	public double CollectionMilliseconds { get; set; }

	[DataMember(Name = "recentIncidents")]
	public List<RecentIncidentMessage> RecentIncidents { get; set; } = new List<RecentIncidentMessage>();

	public ColonySnapshotMessage Copy()
	{
		ColonySnapshotMessage colonySnapshotMessage = (ColonySnapshotMessage)MemberwiseClone();
		colonySnapshotMessage.RecentIncidents = new List<RecentIncidentMessage>(RecentIncidents.Count);
		foreach (RecentIncidentMessage recentIncident in RecentIncidents)
		{
			colonySnapshotMessage.RecentIncidents.Add(recentIncident.Copy());
		}
		return colonySnapshotMessage;
	}
}
