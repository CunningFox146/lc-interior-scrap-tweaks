# Local reference assemblies (not committed)

Copy the proprietary DLLs here — they are gitignored. Any path can also be
overridden per build with `-p:<Name>=...`.

- `libs/LethalCompany/`: `Assembly-CSharp.dll`, `DunGen.dll`,
  `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `Unity.Netcode.Runtime.dll`
  (from `Lethal Company/Lethal Company_Data/Managed`)
- `libs/BepInExCore/`: `BepInEx.dll`, `0Harmony.dll`
  (from a BepInEx `core` folder)
- `libs/LethalLevelLoader/`: `LethalLevelLoader.dll` (compile-time only,
  runtime-optional)
