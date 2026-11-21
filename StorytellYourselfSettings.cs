using System.Security.Cryptography;
using Verse;

namespace StorytellYourself;

public sealed class StorytellYourselfSettings : ModSettings
{
	public bool ServerEnabled = true;

	public int Port = 48912;

	public string AiStorytellerMode = "concurrent";

	public bool LanConnectionsEnabled;

	public string PairingCode = string.Empty;

	public override void ExposeData()
	{
		Scribe_Values.Look<bool>(ref ServerEnabled, "serverEnabled", true, false);
		Scribe_Values.Look<int>(ref Port, "port", 48912, false);
		Scribe_Values.Look<string>(ref AiStorytellerMode, "aiStorytellerMode", "concurrent", false);
		Scribe_Values.Look<bool>(ref LanConnectionsEnabled, "lanConnectionsEnabled", false, false);
		Scribe_Values.Look<string>(ref PairingCode, "pairingCode", string.Empty, false);
		Port = ClampPort(Port);
		AiStorytellerMode = NormalizeAiStorytellerMode(AiStorytellerMode);
		PairingCode = NormalizePairingCode(PairingCode);
	}

	internal static int ClampPort(int port)
	{
		if (port < 1024)
		{
			return 1024;
		}
		if (port <= 65535)
		{
			return port;
		}
		return 65535;
	}

	internal static string NormalizeAiStorytellerMode(string? mode)
	{
		if (!(mode == "questsOnly") && !(mode == "disabled"))
		{
			return "concurrent";
		}
		return mode;
	}

	internal static string NormalizePairingCode(string? code)
	{
		string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
		if (normalized.Length >= 6 && normalized.Length <= 32)
		{
			return normalized;
		}
		return GeneratePairingCode();
	}

	internal static string GeneratePairingCode()
	{
		byte[] bytes = new byte[8];
		using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
		{
			generator.GetBytes(bytes);
		}
		char[] characters = new char[bytes.Length];
		for (int index = 0; index < bytes.Length; index++)
		{
			characters[index] = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[bytes[index] % "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Length];
		}
		return new string(characters);
	}
}
