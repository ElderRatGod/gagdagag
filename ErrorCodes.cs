namespace StorytellYourself.Protocol;

public static class ErrorCodes
{
	public const string InvalidJson = "invalid_json";

	public const string UnsupportedMessage = "unsupported_message";

	public const string HandshakeRequired = "handshake_required";

	public const string ProtocolMismatch = "protocol_mismatch";

	public const string AuthenticationFailed = "authentication_failed";

	public const string InvalidRequest = "invalid_request";

	public const string InvalidPoints = "invalid_points";

	public const string InvalidOption = "invalid_option";

	public const string IncidentUnavailable = "incident_unavailable";

	public const string RequestIdConflict = "request_id_conflict";

	public const string GameUnavailable = "game_unavailable";

	public const string NoMap = "no_map";

	public const string IncidentRejected = "incident_rejected";

	public const string InternalError = "internal_error";

	public const string ServerStopped = "server_stopped";
}
