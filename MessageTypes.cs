namespace StorytellYourself.Protocol;

public static class MessageTypes
{
	public const string Hello = "hello";

	public const string HelloAck = "helloAck";

	public const string ProtocolError = "protocolError";

	public const string GameState = "gameState";

	public const string ColonySnapshot = "colonySnapshot";

	public const string IncidentCatalog = "incidentCatalog";

	public const string UpsertPawnBlueprint = "upsertPawnBlueprint";

	public const string PawnBlueprintResult = "pawnBlueprintResult";

	public const string RequestPawnRegistry = "requestPawnRegistry";

	public const string PawnInstance = "pawnInstance";

	public const string PawnRegistryComplete = "pawnRegistryComplete";

	public const string TriggerIncident = "triggerIncident";

	public const string CommandResult = "commandResult";
}
