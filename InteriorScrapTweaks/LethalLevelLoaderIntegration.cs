using LethalLevelLoader;

namespace InteriorScrapTweaks;

/// <summary>
///     Direct access to the LethalLevelLoader API with no reflection.
///     LLL stays runtime-optional: these methods are only ever invoked from inside
///     a try block (see InteriorResolver), so a missing LLL assembly surfaces as a
///     catchable load exception instead of a crash.
///     Constraint: no LLL type may appear in any member signature here, otherwise
///     merely JIT-compiling the caller would require the LLL assembly to be present.
/// </summary>
internal static class LethalLevelLoaderIntegration
{
    internal static string GetCurrentDungeonName()
    {
        return DungeonManager.CurrentExtendedDungeonFlow?.DungeonName;
    }
}
