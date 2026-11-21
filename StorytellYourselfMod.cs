using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace StorytellYourself;

public sealed class StorytellYourselfMod : Mod
{
	public const string PackageId = "flexible.storytellyourself";

	public const string Version = "0.9.1-quest-composer";

	private readonly StorytellYourselfSettings settings;

	private string portBuffer;

	internal static StorytellYourselfSettings? CurrentSettings { get; private set; }

	public StorytellYourselfMod(ModContentPack content)
		: base(content)
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		settings = ((Mod)this).GetSettings<StorytellYourselfSettings>();
		CurrentSettings = settings;
		settings.Port = StorytellYourselfSettings.ClampPort(settings.Port);
		settings.AiStorytellerMode = StorytellYourselfSettings.NormalizeAiStorytellerMode(settings.AiStorytellerMode);
		settings.PairingCode = StorytellYourselfSettings.NormalizePairingCode(settings.PairingCode);
		portBuffer = settings.Port.ToString();
		new Harmony("flexible.storytellyourself").PatchAll(Assembly.GetExecutingAssembly());
		LocalStorytellerServer.Instance.Configure(settings.ServerEnabled, settings.Port, settings.LanConnectionsEnabled, settings.PairingCode);
		Log.Message($"[Storytell Yourself] Loaded. Protocol v{4}.");
	}

	public override string SettingsCategory()
	{
		return "Storytell Yourself";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		Listing_Standard listing = new Listing_Standard();
		((Listing)listing).Begin(inRect);
		listing.CheckboxLabeled("Enable Human Storyteller server", ref settings.ServerEnabled, "Local-only by default. Enabling LAN/VPN access below requires the pairing code. Restarting the listener disconnects clients, which reconnect automatically.", 0f, 1f);
		((Listing)listing).Gap(12f);
		listing.Label("AI storyteller participation", -1f, (Nullable)null);
		if (listing.RadioButton("Active alongside Human Storyteller", settings.AiStorytellerMode == "concurrent", 0f, "The selected RimWorld storyteller continues generating all normal incidents and quests.", (float?)null))
		{
			settings.AiStorytellerMode = "concurrent";
		}
		if (listing.RadioButton("Quests only", settings.AiStorytellerMode == "questsOnly", 0f, "The AI may offer quests, but ordinary storyteller incidents are left to the Human Storyteller.", (float?)null))
		{
			settings.AiStorytellerMode = "questsOnly";
		}
		if (listing.RadioButton("Disabled", settings.AiStorytellerMode == "disabled", 0f, "The AI storyteller generates no new incidents or quests. Consequences from quests already in progress still run.", (float?)null))
		{
			settings.AiStorytellerMode = "disabled";
		}
		((Listing)listing).GapLine(12f);
		listing.Label("Local server port", -1f, (Nullable)null);
		listing.TextFieldNumeric<int>(ref settings.Port, ref portBuffer, 1024f, 65535f);
		((Listing)listing).Gap(12f);
		listing.CheckboxLabeled("Allow LAN/VPN connections", ref settings.LanConnectionsEnabled, "Allows a paired client on a trusted local network, Radmin VPN, or Tailscale network to connect. Never expose this port directly to the public internet.", 0f, 1f);
		settings.PairingCode = listing.TextEntryLabeled("Pairing code", settings.PairingCode, 1);
		if (listing.ButtonText("Generate new pairing code", (string)null, 1f))
		{
			settings.PairingCode = StorytellYourselfSettings.GeneratePairingCode();
		}
		listing.Label(settings.LanConnectionsEnabled ? $"LAN/VPN clients use this computer's VPN/LAN address and port {StorytellYourselfSettings.ClampPort(settings.Port)}." : $"Local client address: ws://127.0.0.1:{StorytellYourselfSettings.ClampPort(settings.Port)}/storyteller/", -1f, (Nullable)null);
		((Listing)listing).End();
	}

	public override void WriteSettings()
	{
		settings.Port = StorytellYourselfSettings.ClampPort(settings.Port);
		settings.AiStorytellerMode = StorytellYourselfSettings.NormalizeAiStorytellerMode(settings.AiStorytellerMode);
		settings.PairingCode = StorytellYourselfSettings.NormalizePairingCode(settings.PairingCode);
		portBuffer = settings.Port.ToString();
		((Mod)this).WriteSettings();
		LocalStorytellerServer.Instance.Configure(settings.ServerEnabled, settings.Port, settings.LanConnectionsEnabled, settings.PairingCode);
		Log.Message("[Storytell Yourself] AI storyteller mode: " + settings.AiStorytellerMode + ".");
	}
}
