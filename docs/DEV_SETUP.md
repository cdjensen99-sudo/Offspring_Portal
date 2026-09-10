# Dev setup — Valheim 1.0.7

Canonical environment for fixing and testing this mod.

## Paths

| Item | Path |
|------|------|
| **Valheim (game assemblies)** | `D:\SteamLibrary\steamapps\common\Valheim` |
| **Managed DLLs** | `D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed` |
| **Gale profile (test/fix)** | `C:\Users\cdjen\AppData\Roaming\com.kesomannen.gale\valheim\profiles\New Release` |
| **BepInEx core** | `...\New Release\BepInEx\core` (BepInExPack **5.4.2350**) |
| **Jotunn** | `...\New Release\BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll` |
| **Deploy target** | `...\New Release\BepInEx\plugins\Hardwire99-Offspring_Portal\` |

## Build

```powershell
cd /d "D:\ValheimProjects\OffspringPortal"
.\build.ps1
```

Deploy to the **New Release** profile:

```powershell
.\build.ps1 -Deploy
```

## Overrides

Copy `Directory.Build.props.user.example` → `Directory.Build.props.user` (gitignored) to change paths on another PC without editing shared files.

## Valheim version

- **Game:** 1.0.7 (network version 39)
- **Unity:** 6000.0.75
- **Target framework:** `net472` (unchanged)

## Valheim 1.0 API notes

- **`Hoverable`** requires `float GetHoverOffset()` — omitting it breaks vtable setup and spams JIT errors every frame.
- **`UseHoverMarker()`** is not on the 1.0 `Hoverable` interface; do not implement it on custom hover types.
- **`OPTeleportWorld`** returns `m_hoverOffset` (copied from vanilla `portal_wood` on clone, otherwise `0f`).
- **`Terminal.ConsoleCommand`** — 1.0 ctor adds a 13th parameter (`hideBehindDevCommands` before `ConsoleOptionsFetcher`). **This mod does not register console commands** — no source changes; retarget + rebuild is sufficient.

## Jotunn — Valheim 1.0 (step 4)

**Status:** Shipped **1.0.0** against Jotunn **2.30.0** on Valheim **1.0.7**. Require Jotunn 2.30.0+ on Thunderstore/manifest for 1.0 installs.

### What this mod uses Jotunn for

| API | File | Risk on 1.0 |
|-----|------|----------------|
| `PrefabManager.CreateClonedPrefab` + `AddPrefab` | `OffspringPortalPrefabs.cs` | Clone timing / ZNet registration |
| `PieceManager.RegisterPieceInPieceTable` | `OffspringPortalPrefabs.cs` | Hammer menu / `ObjectDB` timing |
| `PrefabManager.OnVanillaPrefabsAvailable` | `OffspringPortalPrefabs.cs` | Event order vs `ObjectDB` init |
| `GUIManager` (panel, inputs, dropdowns) | `SpeciesConfigPanel.cs` | UI prefab / font paths on Unity 6 |

Existing mitigations in our code:

- Piece registration is **deferred** on failure (`RegisterPiece` try/catch + `EnsurePieceRegistered` on `Player.OnSpawned`).
- Prefab conversion to `OPTeleportWorld` happens after clone, before `AddPrefab`.

### Pre-ship checklist (Jotunn-dependent)

- [ ] Hammer shows **Offspring Portal** piece after world load
- [ ] Placed portal is visible to **client and host** (multiplayer)
- [ ] **Press E** opens config UI (Name / Type / Receives / Destination)
- [ ] Recipe costs apply (Fine Wood / Greydwarf Eye / Surtling Core)
- [ ] Re-test after **each Jotunn update** — timing changes are the main regression vector

### Thunderstore dependencies (update before public 1.0 ship)

Current `manifest.json` still pins pre-1.0 minimums:

- `denikson-BepInExPack_Valheim-5.4.2202` → should become **5.4.2350** (Unity 6 pack)
- `ValheimModding-Jotunn-2.26.0` → bump to **confirmed 1.0 Jotunn** when available (≥ 2.29.2 interim only if tested)

### If Jotunn breaks on a game patch

Symptoms: missing hammer piece, purple prefab, config UI fails to open, `Failed to clone portal_wood via Jotunn` in log.

1. Update Jotunn in Gale profile first.
2. Rebuild Offspring Portal against current `assembly_valheim`.
3. If still broken, check [Jotunn GitHub/issues](https://github.com/Valheim-Modding/Jotunn) before patching our timing hooks.
4. Long-term fallback (not started): vanilla `ObjectDB` + asset bundle registration without Jotunn — large refactor.

## World saves — Valheim 1.0 (step 5)

**Status:** No source changes required. Valheim handles the new on-disk layout internally.

### What changed in 1.0

| Old | New |
|-----|-----|
| Single `.db` + `.fwl` pair | World **folder** with chunk files (e.g. `_main.0.db2`) |
| `ZDOMan.Load(...)` (old signature) | `ZDOMan.Load(BinaryReader, Version.World)` — **game code only** |

### What Offspring Portal does

- Stores portal config in **ZDO custom fields** (`op_portal_name`, `op_portal_role`, etc.) on placed pieces.
- Reads them at **runtime** via `ZDOMan.instance.GetZDO(...)` and `GetAllZDOsWithPrefabIterative("offspring_portal", ...)`.
- Rebuilds the portal registry on **world load** (`Game.Start`) and **player spawn** (`Player.OnSpawned`).

This mod never opens `.db`, `.fwl`, or chunk files — persistence is format-agnostic as long as ZDO APIs behave the same.

### Smoke-test after recompile

- [ ] Load an **existing world** that already has offspring portals placed
- [ ] Confirm portal **names, roles, and species** settings survived save/load
- [ ] Confirm juveniles still route after load (registry rebuild)
- [ ] Save, quit, reload — settings unchanged

## Juvenile follow (adding species)

See [`JUVENILE_FOLLOW.md`](JUVENILE_FOLLOW.md) and [`NOTES-Asksvin-juveniles.md`](../NOTES-Asksvin-juveniles.md).

---

## P0 fixes — Valheim 1.0.7

| Issue | Fix |
|-------|-----|
| `Hoverable.GetHoverOffset()` | Implemented on `OPTeleportWorld`; removed obsolete `UseHoverMarker()` |
| Registry NRE on `Game.Start` / `Player.OnSpawned` | Wait for `ZDOMan` + guard `RefreshCapWarnings`; defer full rebuild; lighter rebuild on spawn |
| Prefab registration | Validate `portal_wood` in `ZNetScene` before clone; log success/failure |
| Moose (1.0) | Added to species catalog + discovery fragments (`Moose`, `Moose_Calf`) |
| Seals | Not breedable — excluded from catalog (wild/ambient) |
| Config UI | Null-safe Unity UI component checks; log when Jotunn panel init succeeds/fails |
