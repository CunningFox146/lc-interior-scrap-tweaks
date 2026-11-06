using System;
using System.Collections.Generic;
using System.IO;

namespace InteriorScrapTweaks;

/// <summary>
///     Resolves which interior (dungeon) is currently being generated, as a list of
///     candidate config keys ordered from most specific to least specific:
///     1. LethalLevelLoader ExtendedDungeonFlow.DungeonName (when LLL is installed)
///     2. Vanilla DunGen DungeonFlow asset name (e.g. Level1Flow, or a custom flow name)
///     3. Numeric fallback "Type_N" (RoundManager.currentDungeonType)
///     LLL is referenced directly (see LethalLevelLoaderIntegration) but stays runtime-optional:
///     it is only ever touched inside a try block, so a missing LLL assembly
///     surfaces as a catchable load exception instead of a crash.
/// </summary>
internal static class InteriorResolver
{
    // Once true, the LLL path is never attempted again for this session, so a
    // missing or incompatible LLL can't spam the log on every moon.
    private static bool _lethalLevelLoaderResolutionFailed;

    internal static List<string> GetInteriorNameCandidates(RoundManager roundManager)
    {
        var interiorNameCandidates = new List<string>();

        try
        {
            var lethalLevelLoaderDungeonName = TryGetLethalLevelLoaderDungeonName();
            if (!string.IsNullOrEmpty(lethalLevelLoaderDungeonName))
                interiorNameCandidates.Add(lethalLevelLoaderDungeonName);
        }
        catch (Exception exception)
        {
            Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Failed to resolve LLL dungeon name, skipping it: {exception}");
        }

        try
        {
            var dungeonFlowName = roundManager.dungeonGenerator?.Generator?.DungeonFlow?.name;
            if (!string.IsNullOrEmpty(dungeonFlowName) && !ContainsInteriorName(interiorNameCandidates, dungeonFlowName))
                interiorNameCandidates.Add(dungeonFlowName);
        }
        catch (Exception exception)
        {
            Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Failed to read DungeonFlow name from dungeonGenerator, skipping it: {exception}");
        }

        try
        {
            if (roundManager.dungeonFlowTypes != null
                && roundManager.currentDungeonType >= 0
                && roundManager.currentDungeonType < roundManager.dungeonFlowTypes.Length)
            {
                var dungeonFlowName = roundManager.dungeonFlowTypes[roundManager.currentDungeonType]?.dungeonFlow?.name;
                if (!string.IsNullOrEmpty(dungeonFlowName) && !ContainsInteriorName(interiorNameCandidates, dungeonFlowName))
                    interiorNameCandidates.Add(dungeonFlowName);
            }
        }
        catch (Exception exception)
        {
            Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Failed to read DungeonFlow name from dungeonFlowTypes[{roundManager?.currentDungeonType}], skipping it: {exception}");
        }

        try
        {
            interiorNameCandidates.Add("Type_" + roundManager?.currentDungeonType);
        }
        catch (Exception exception)
        {
            Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Failed to add numeric interior fallback key: {exception}");
        }

        return interiorNameCandidates;
    }

    private static bool ContainsInteriorName(List<string> candidates, string candidateName)
    {
        foreach (var existingCandidate in candidates)
            if (string.Equals(existingCandidate, candidateName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string TryGetLethalLevelLoaderDungeonName()
    {
        if (_lethalLevelLoaderResolutionFailed)
            return null;

        try
        {
            return LethalLevelLoaderIntegration.GetCurrentDungeonName();
        }
        catch (FileNotFoundException exception)
        {
            _lethalLevelLoaderResolutionFailed = true;
            Plugin.ModLogger?.LogInfo($"[InteriorScrapTweaks] LethalLevelLoader not found, matching interiors by DungeonFlow name only. ({exception.Message})");
            return null;
        }
        catch (FileLoadException exception)
        {
            _lethalLevelLoaderResolutionFailed = true;
            Plugin.ModLogger?.LogInfo($"[InteriorScrapTweaks] LethalLevelLoader could not be loaded, matching interiors by DungeonFlow name only. ({exception.Message})");
            return null;
        }
        catch (Exception exception)
        {
            _lethalLevelLoaderResolutionFailed = true;
            Plugin.ModLogger?.LogWarning($"[InteriorScrapTweaks] Failed to read LethalLevelLoader CurrentExtendedDungeonFlow.DungeonName, LLL matching disabled for this session: {exception}");
            return null;
        }
    }
}
