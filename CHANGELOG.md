# Changelog

## 1.0.0

- Initial release.
- Per-interior scrap-count multipliers (`AmountMultipliers`).
- Per-interior scrap-value multipliers (`ValueMultipliers`).
- Default multipliers for unlisted interiors.
- Interior detection via LLL dungeon name (soft dependency, reflection only),
  DungeonFlow asset name, and numeric `Type_N` fallback.
- Detected-interior logging to make configuring modded interiors easy.
- Original level values restored after spawning.
