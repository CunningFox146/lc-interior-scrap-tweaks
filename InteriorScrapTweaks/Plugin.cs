using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace InteriorScrapTweaks;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "CunningFox146.InteriorScrapTweaks";
    public const string PluginName = "InteriorScrapTweaks";
    public const string PluginVersion = "1.0.1";

    internal static ManualLogSource ModLogger;

    internal static ConfigEntry<float> DefaultScrapCountMultiplier;
    internal static ConfigEntry<float> DefaultScrapValueMultiplier;
    internal static ConfigEntry<string> ScrapCountMultipliers;
    internal static ConfigEntry<string> ScrapValueMultipliers;
    internal static ConfigEntry<bool> LogDetectedInteriorNames;

    private void Awake()
    {
        ModLogger = Logger;

        DefaultScrapCountMultiplier = Config.Bind(
            "General",
            "DefaultScrapCountMultiplier",
            1f,
            new ConfigDescription(
                "Scrap-count multiplier used for interiors not listed in ScrapCountMultipliers.",
                new AcceptableValueRange<float>(0f, 10f)));

        DefaultScrapValueMultiplier = Config.Bind(
            "General",
            "DefaultScrapValueMultiplier",
            1f,
            new ConfigDescription(
                "Scrap-value multiplier used for interiors not listed in ScrapValueMultipliers.",
                new AcceptableValueRange<float>(0f, 10f)));

        ScrapCountMultipliers = Config.Bind(
            "Interiors",
            "ScrapCountMultipliers",
            "Factory=1.0, Mansion=1.5, Mineshaft=0.7, Sewer=1.0, Level1Flow=1.0, Level2Flow=1.5, Level3Flow=0.7",
            "Per-interior scrap-COUNT multipliers. Format: \"Name=Multiplier, Name2=Multiplier2\" " +
            "(separators: comma/semicolon/newline between entries, '=' or ':' between key and value). " +
            "Keys match the LethalLevelLoader dungeon name when LLL is installed, otherwise the DungeonFlow asset name. " +
            "Check the BepInEx console/log for the exact \"Interior keys\" line logged on level generation.");

        ScrapValueMultipliers = Config.Bind(
            "Interiors",
            "ScrapValueMultipliers",
            "Factory=1.0, Mansion=1.0, Mineshaft=1.0, Sewer=1.0, Level1Flow=1.0, Level2Flow=1.0, Level3Flow=1.0",
            "Per-interior scrap-VALUE multipliers (average value per item). Same format and key matching as ScrapCountMultipliers.");

        LogDetectedInteriorNames = Config.Bind(
            "General",
            "LogDetectedInteriorNames",
            true,
            "Log the detected interior keys and applied multipliers every time scrap spawns. " +
            "Use the logged keys in ScrapCountMultipliers/ScrapValueMultipliers to target modded interiors. " +
            "You can turn this off once your config is settled.");

        new Harmony(PluginGuid).PatchAll(typeof(ScrapSpawnPatches));

        ModLogger.LogInfo($"{PluginName} v{PluginVersion} loaded. Scrap amount/value will be scaled per interior.");
    }
}
