# InteriorScrapTweaks

Scales **scrap amount** (item count) and **scrap value** (average value per item)
depending on which **interior** (dungeon) is generated for the moon.

- Keyed by dungeon name, so modded interiors (e.g. Wesley's Interiors) work too:
  with LethalLevelLoader installed the LLL dungeon name is used,
  otherwise the DungeonFlow asset name (e.g. `Level1Flow`).
- Host-only: scrap is spawned by the host, so only the host needs the config.
  Clients don't need the mod (but having it is harmless).
- Original level values are restored right after spawning, so other moons
  and other mods are unaffected.

## Config

`BepInEx/config/CunningFox146.InteriorScrapTweaks.cfg`

| Section | Key | What it does |
|---|---|---|
| General | `DefaultScrapCountMultiplier` | Scrap-count multiplier for interiors not listed (default `1`) |
| General | `DefaultScrapValueMultiplier` | Scrap-value multiplier for interiors not listed (default `1`) |
| Interiors | `ScrapCountMultipliers` | `Name=Multiplier, ...` per-interior scrap-count multipliers |
| Interiors | `ScrapValueMultipliers` | `Name=Multiplier, ...` per-interior scrap-value multipliers |
| General | `LogDetectedInteriorNames` | Log detected interior keys on every generation (`true` by default) |

Example:

```ini
[Interiors]
ScrapCountMultipliers = Factory=1.0, Mansion=1.5, Mineshaft=0.7
ScrapValueMultipliers = Factory=1.0, Mansion=1.2, Mineshaft=1.0
```

Total scrap value on a moon roughly scales as
`amount multiplier x value multiplier`
(both compound: more items *and* richer items).

Safety caps: spawned scrap count is capped at 1000 items per moon and the
effective value multiplier at x100000, so a config typo can't freeze the game.
Hitting a cap logs a warning — legitimate values never come close to them.

## Finding interior names

1. Land on a moon with `LogDetectedInteriorNames = true`.
2. Open the BepInEx console (or `LogOutput.log`) and find the line:
   `Interior keys: Mansion | Level2Flow | Type_1 -> scrap amount x1.5, ...`
3. Use any of the logged keys in your multiplier tables
   (matching is case-insensitive, first match wins).

## Install

- Via Thunderstore / r2modman: install like any BepInEx mod.
- Manual: copy `InteriorScrapTweaks.dll` (plus `manifest.json`/`README.md`)
  into `Lethal Company/BepInEx/plugins/InteriorScrapTweaks/`.

## Build

Requires .NET 8 SDK. Game/reference assemblies are proprietary and not
committed — copy them into `libs/` (see `libs/README.md`), then:

```powershell
dotnet build InteriorScrapTweaks -c Release
```

Reference paths (`LethalCompanyDir`, `BepInExCoreDir`, `LethalLevelLoaderDir`)
default to `libs/...` and can be overridden per build, e.g.
`-p:LethalCompanyDir="<game Managed folder>"`.

LethalLevelLoader is referenced at compile time (any installed copy of
`LethalLevelLoader.dll` works) but stays
**runtime-optional**: the mod runs fine without LLL installed and simply
matches interiors by DungeonFlow name in that case. No Thunderstore
dependency on LLL is declared.

Optional: deploy the built DLL into a local Thunderstore profile after
build with `-p:ThunderstoreProfilePluginsDir="<profile BepInEx plugins folder>"`.

## How it works

`RoundManager.SpawnScrapInLevel` reads `SelectableLevel.minScrap/maxScrap`
and `RoundManager.scrapValueMultiplier`. A Harmony prefix applies the
interior's multipliers to those fields; a postfix restores the originals.
