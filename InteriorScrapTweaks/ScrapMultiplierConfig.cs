using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;

namespace InteriorScrapTweaks;

/// <summary>
///     Parses and looks up per-interior multiplier tables.
///     Format: "InteriorKey=Multiplier, AnotherKey=Multiplier2"
///     Separators between entries: comma, semicolon or newline.
///     Separators between key and value: '=' or ':'.
///     Keys are case-insensitive. Invalid entries are skipped with a warning.
///     Multipliers must be finite and non-negative; negatives are clamped to 0.
///     When an interior name appears more than once, the last entry wins.
/// </summary>
internal static class ScrapMultiplierConfig
{
    public static Dictionary<string, float> ParseMultiplierTable(string rawTableText, ManualLogSource logger, string tableName)
    {
        var multipliersByInteriorName = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawTableText))
            return multipliersByInteriorName;

        var entryTexts = rawTableText.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var entryText in entryTexts)
        {
            var keyAndMultiplierParts = entryText.Split(['=', ':'], 2);
            if (keyAndMultiplierParts.Length != 2)
            {
                logger?.LogWarning(
                    $"[{Plugin.PluginName}] Ignoring malformed entry in {tableName}: \"{entryText.Trim()}\" (expected Key=Multiplier).");
                continue;
            }

            var interiorName = keyAndMultiplierParts[0].Trim();
            var multiplierText = keyAndMultiplierParts[1].Trim();
            if (interiorName.Length == 0)
            {
                logger?.LogWarning($"[{Plugin.PluginName}] Ignoring entry with empty key in {tableName}: \"{entryText.Trim()}\".");
                continue;
            }

            if (float.TryParse(multiplierText, NumberStyles.Float, CultureInfo.InvariantCulture, out var multiplier))
            {
                if (!float.IsFinite(multiplier))
                {
                    logger?.LogWarning(
                        $"[{Plugin.PluginName}] Ignoring non-finite multiplier in {tableName}: \"{entryText.Trim()}\".");
                    continue;
                }

                if (multiplier < 0f)
                {
                    logger?.LogWarning(
                        $"[{Plugin.PluginName}] Clamping negative multiplier to 0 in {tableName}: \"{entryText.Trim()}\".");
                    multiplier = 0f;
                }

                multipliersByInteriorName[interiorName] = multiplier;
            }
            else
            {
                logger?.LogWarning(
                    $"[{Plugin.PluginName}] Ignoring entry with non-numeric multiplier in {tableName}: \"{entryText.Trim()}\".");
            }
        }

        return multipliersByInteriorName;
    }

    /// <summary>
    ///     Returns the multiplier for the first matching interior name, or the default.
    /// </summary>
    public static float FindMultiplierForInterior(IReadOnlyList<string> interiorNameCandidates, Dictionary<string, float> multipliersByInteriorName, float defaultMultiplier)
    {
        if (interiorNameCandidates == null)
            return defaultMultiplier;

        foreach (var candidateName in interiorNameCandidates)
            if (candidateName != null && multipliersByInteriorName.TryGetValue(candidateName, out var multiplier))
                return multiplier;

        return defaultMultiplier;
    }
}
