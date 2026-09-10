# Juvenile follow (E key) — adding species

Press **E** on a tamed juvenile to toggle **Follow** / **Stay**. This doc explains how each species gets onto the follow list.

See also: [`NOTES-Asksvin-juveniles.md`](../NOTES-Asksvin-juveniles.md) (Asksvin hatchling + Beast Hird).

---

## How follow works (two AI paths)

| AI type | Examples | Toggle (`JuvenileFollow`) | Movement |
|---------|----------|---------------------------|----------|
| **`AnimalAI`** | Boar piggy, wolf cub, lox calf, hen chick, **moose calf** | `ApplyAnimalFollowToggle` → ZDO `op_follow_target` | `JuvenileFollowController` on `AnimalAI.UpdateAI` |
| **`MonsterAI`** (no `Tameable`) | **Asksvin hatchling** | `ApplyMonsterFollowToggle` → vanilla `SetFollowTarget` / `ZDOVars.s_follow` | Vanilla MonsterAI follow |

**Eligibility gate:** `SpeciesHelper.IsEligibleJuvenile` — tamed + (`Growup` **or** prefab-name fallback).

**Hover text:** `JuvenileFollowDisplay` — needs `AnimalAI` or `MonsterAI` on the creature.

---

## Reference: Wolf cub (already working)

| Prefab | Components |
|--------|------------|
| `Wolf_cub` | `Growup` + `AnimalAI` (no `Tameable`) |

1. `IsEligibleJuvenile` — passes via `Growup`; fallback if name contains `wolfcub`.
2. `ApplyFollowToggle` — `AnimalAI` branch.
3. `GetJuvenileSpeciesKey` — maps `wolfcub` → Wolf.

No separate “follow list” file — eligibility + AI path **is** the list.

---

## Moose calf (Valheim 1.0)

| Prefab | Expected components |
|--------|---------------------|
| `Moose_Calf` | `Growup` + `AnimalAI` (same pattern as `Lox_Calf`) |

### To add / verify Moose calf follow

1. **`SpeciesHelper.IsEligibleJuvenile`** — add prefab-name fallback `moose_calf` / `moosecalf` (same idea as wolf cub).
2. **No `JuvenileFollow` changes** if calf has `AnimalAI` — uses existing AnimalAI path automatically.
3. **Portal routing** — `GetJuvenileSpeciesKey` already maps moose calf names → `Moose`.
4. **In-game test:** tame moose calf → hover shows Follow/Stay → **E** toggles → calf moves with you.

If follow fails, run `printcreatures` / check components: missing `AnimalAI` means a different code path is needed (unlikely for moose).

---

## Asksvin hatchling (not “calf”)

| Prefab | Components |
|--------|------------|
| `Asksvin_hatchling` | `Growup` + `MonsterAI` — **no** `Tameable`, **no** `AnimalAI` |

### Problem

Portals already accept tamed hatchlings (`Growup`). Follow **failed** because `ApplyFollowToggle` required `Tameable` **and** `MonsterAI`.

### Fix (in `JuvenileFollow.ApplyFollowToggle`)

- If tamed + `MonsterAI`: allow follow **without** `Tameable`.
- Use `Character.GetHoverName()` for messages.
- Reuse vanilla monster follow: `SetFollowTarget` / clear, `ZDOVars.s_follow`.
- Do **not** patch `MonsterAI.UpdateAI`.

Optional: prefab name contains `asksvin` + `hatchling` in `IsEligibleJuvenile` (hatchlings already match via `Growup` when tamed).

### Beast Hird overlap

If **`hardwire99.training`** (Beast Hird) is loaded, OP follow is disabled — Beast Hird owns follow for all tamed creatures.

---

## Checklist: add a new live-birth juvenile

1. Confirm prefab components in-game (`Growup`? `AnimalAI` or `MonsterAI`?).
2. Add species to catalog if needed (`SpeciesType`, `KnownAdultPrefabFragments`).
3. Add **`IsEligibleJuvenile`** name fallback only if `Growup` can be missing at runtime.
4. Add **`GetJuvenileSpeciesKey`** name mapping if `Growup` → adult mapping is non-obvious.
5. If **`MonsterAI` without `Tameable`**: ensure `ApplyFollowToggle` monster branch runs (Asksvin pattern).
6. If **`AnimalAI`**: no follow toggle changes; verify `JuvenileFollowController` attaches on `Character.Awake`.
7. Test: hover → E → movement → portal routing.

---

## Spawned untamed juveniles (dev)

Console `spawn Wolf_cub` etc. skips egg/birth tame. Use dev command `op.tamejuveniles [radius]` (when implemented) or hatch/breed normally. See `NOTES-Asksvin-juveniles.md`.
