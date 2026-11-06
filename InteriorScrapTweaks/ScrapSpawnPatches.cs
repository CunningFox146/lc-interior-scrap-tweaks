using System;
using HarmonyLib;
using UnityEngine;

namespace InteriorScrapTweaks;

/// <summary>
///     Scales scrap count and per-item value for the interior being generated,
///     then restores the level's original values so other moons/levels are unaffected.
///     Implementation: RoundManager.SpawnScrapInLevel reads
///     SelectableLevel.minScrap/maxScrap and RoundManager.scrapValueMultiplier.
///     The prefix applies interior multipliers to those fields; the postfix restores them.
///     SelectableLevel is a shared ScriptableObject, so restoring is required.
///     Scrap spawning is host-authoritative; clients skip the adjustment.
/// </summary>
[HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
internal static class ScrapSpawnPatches
{
    private static int _originalMinimumScrapCount;
    private static int _originalMaximumScrapCount;
    private static int _originalMinimumTotalScrapValue;
    private static int _originalMaximumTotalScrapValue;
    private static float _originalScrapValueMultiplier;
    private static bool _scrapValuesWereAdjusted;

    // Spawning creates one networked object per scrap item, so an absurd count
    // (e.g. from a "1e20" typo in the config) would freeze or crash the game.
    // Cap the spawned count at a generous-but-safe maximum instead.
    private const int MaximumSpawnedScrapCount = 1000;

    // Per-item value is computed as itemValue * scrapValueMultiplier, so cap the
    // effective value multiplier to keep the product safely inside int range.
    private const float MaximumScrapValueMultiplier = 100000f;

    private static void Prefix(RoundManager __instance)
    {
        var roundManager = __instance;
        _scrapValuesWereAdjusted = false;
        try
        {
            if (roundManager == null || roundManager.currentLevel == null)
                return;

            bool isServer;
            try
            {
                isServer = roundManager.IsServer;
            }
            catch (Exception exception)
            {
                Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Failed to check IsServer, assuming host: {exception}");
                isServer = true;
            }

            if (!isServer)
                return;

            var interiorNameCandidates = InteriorResolver.GetInteriorNameCandidates(roundManager);

            var scrapCountMultipliers = ScrapMultiplierConfig.ParseMultiplierTable(Plugin.ScrapCountMultipliers.Value, Plugin.ModLogger, "ScrapCountMultipliers");
            var scrapValueMultipliers = ScrapMultiplierConfig.ParseMultiplierTable(Plugin.ScrapValueMultipliers.Value, Plugin.ModLogger, "ScrapValueMultipliers");

            var interiorScrapCountMultiplier = ScrapMultiplierConfig.FindMultiplierForInterior(interiorNameCandidates, scrapCountMultipliers, Plugin.DefaultScrapCountMultiplier.Value);
            var interiorScrapValueMultiplier = ScrapMultiplierConfig.FindMultiplierForInterior(interiorNameCandidates, scrapValueMultipliers, Plugin.DefaultScrapValueMultiplier.Value);

            if (Plugin.LogDetectedInteriorNames.Value)
                Plugin.ModLogger?.LogInfo($"[InteriorScrapTweaks] Interior keys: {string.Join(" | ", interiorNameCandidates)} " +
                                         $"-> scrap amount x{interiorScrapCountMultiplier}, scrap value x{interiorScrapValueMultiplier} " +
                                         $"(base min={roundManager.currentLevel.minScrap} max={roundManager.currentLevel.maxScrap} " +
                                         $"valueMult={roundManager.scrapValueMultiplier}).");

            if (Mathf.Approximately(interiorScrapCountMultiplier, 1f) && Mathf.Approximately(interiorScrapValueMultiplier, 1f))
                return;

            var currentLevel = roundManager.currentLevel;
            _originalMinimumScrapCount = currentLevel.minScrap;
            _originalMaximumScrapCount = currentLevel.maxScrap;
            _originalMinimumTotalScrapValue = currentLevel.minTotalScrapValue;
            _originalMaximumTotalScrapValue = currentLevel.maxTotalScrapValue;
            _originalScrapValueMultiplier = roundManager.scrapValueMultiplier;

            var scaledMinimumScrapCount = Math.Min(MaximumSpawnedScrapCount, Math.Max(0, RoundToInteger(_originalMinimumScrapCount * interiorScrapCountMultiplier)));
            var scaledMaximumScrapCount = Math.Min(MaximumSpawnedScrapCount, Math.Max(scaledMinimumScrapCount, RoundToInteger(_originalMaximumScrapCount * interiorScrapCountMultiplier)));
            if (scaledMinimumScrapCount == MaximumSpawnedScrapCount || scaledMaximumScrapCount == MaximumSpawnedScrapCount)
                Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Scrap count for this interior hit the safety cap of {MaximumSpawnedScrapCount} items; check your ScrapCountMultipliers for typos.");

            var effectiveScrapValueMultiplier = Math.Min(MaximumScrapValueMultiplier, _originalScrapValueMultiplier * interiorScrapValueMultiplier);
            if (effectiveScrapValueMultiplier < _originalScrapValueMultiplier * interiorScrapValueMultiplier)
                Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Scrap value multiplier for this interior hit the safety cap of x{MaximumScrapValueMultiplier}; check your ScrapValueMultipliers for typos.");

            currentLevel.minScrap = scaledMinimumScrapCount;
            currentLevel.maxScrap = scaledMaximumScrapCount;
            currentLevel.minTotalScrapValue = Math.Max(0, RoundToInteger(_originalMinimumTotalScrapValue * interiorScrapCountMultiplier * interiorScrapValueMultiplier));
            currentLevel.maxTotalScrapValue =
                Math.Max(currentLevel.minTotalScrapValue, RoundToInteger(_originalMaximumTotalScrapValue * interiorScrapCountMultiplier * interiorScrapValueMultiplier));
            roundManager.scrapValueMultiplier = effectiveScrapValueMultiplier;

            _scrapValuesWereAdjusted = true;
        }
        catch (Exception exception)
        {
            Plugin.ModLogger?.LogError($"[InteriorScrapTweaks] Failed to apply interior scrap multipliers: {exception}");
        }
    }

    private static void Postfix(RoundManager __instance)
    {
        var roundManager = __instance;
        if (!_scrapValuesWereAdjusted)
            return;

        try
        {
            if (roundManager == null || roundManager.currentLevel == null)
                return;
            var currentLevel = roundManager.currentLevel;
            currentLevel.minScrap = _originalMinimumScrapCount;
            currentLevel.maxScrap = _originalMaximumScrapCount;
            currentLevel.minTotalScrapValue = _originalMinimumTotalScrapValue;
            currentLevel.maxTotalScrapValue = _originalMaximumTotalScrapValue;
            roundManager.scrapValueMultiplier = _originalScrapValueMultiplier;
        }
        catch (Exception exception)
        {
            Plugin.ModLogger?.LogError($"[InteriorScrapTweaks] Failed to restore scrap values: {exception}");
        }
        finally
        {
            _scrapValuesWereAdjusted = false;
        }
    }

    private static int RoundToInteger(double valueToRound)
    {
        if (double.IsNaN(valueToRound) || valueToRound <= 0d)
            return 0;
        if (valueToRound >= int.MaxValue)
            return int.MaxValue;
        return (int)Math.Round(valueToRound, MidpointRounding.AwayFromZero);
    }
}
