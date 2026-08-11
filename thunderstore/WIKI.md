# Offspring Portal — Wiki

> **Automate breeding pens in Valheim.** Teleport tamed juveniles and breedable eggs from breeder areas to maturing pens automatically — optionally route dual-purpose eggs to collection pens, draw mates together, and forward grown adults to farm or cull yards.

**Current version:** 0.4.0 · **Author:** HW · **Team:** HW

---

## Quick links

| Topic | Jump to |
|-------|---------|
| First-time setup | [Quick start](#quick-start) |
| Portal types | [Portal reference](#portal-reference) |
| Species discovery | [Dynamic species discovery](#dynamic-species-discovery) |
| Egg routing | [Dual-purpose vs single-purpose eggs](#dual-purpose-vs-single-purpose-eggs) |
| Mod compatibility | [For mod creators — breedable criteria](#for-mod-creators--breedable-criteria) |
| Mate draw | [Mate draw](#mate-draw) |
| Example farms | [Example setups](#example-setups) |
| Config UI | [Configuring a portal](#configuring-a-portal) |
| Follow command | [Juvenile follow (E key)](#juvenile-follow-e-key) |
| Player travel / XPortal | [Player travel & compatibility](#player-travel--compatibility) |
| Config file | [Advanced config](#advanced-config) |
| Multiplayer | [Multiplayer](#multiplayer) |
| Problems | [FAQ & troubleshooting](#faq--troubleshooting) |
| Community | [Discord & support](#discord--support) |

---

## What this mod does

Offspring Portal adds a **custom buildable portal** (Hammer menu) that **does not work like a normal travel portal**. Instead, it automates animal logistics:

- **Breeder portals** scan for tamed juveniles and eggs nearby, discover breedable species from nearby adults, and optionally **draw mates together**.
- **Maturing portals** receive juveniles and **single-purpose** eggs; optionally send **adults** to Farm or Cull when they grow up.
- **Egg Collector portals** receive **dual-purpose** eggs (recipe + hatch, e.g. hen eggs).
- **Farm** and **Cull** portals receive routed adults (Cull is shared across all species).

Species dropdowns are **built dynamically** from tamed adults near breeder portals — including most modded creatures that use vanilla breeding components.

**Build cost:** 20 Fine Wood · 10 Greydwarf Eyes · 2 Surtling Cores

---

## Quick start

### Requirements

Install on **server/host and every client**:

- [BepInEx Pack for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
- **Offspring Portal** (this mod)

### Minimum setup (2 portals)

1. **Build** an Offspring Portal at your breeding area (Hammer → Building).
2. **Press E** → set **Type: Breeder**, give it a **Name** → OK.
3. **Build** a second portal at your grow-up pen.
4. **Press E** → set **Type: Maturing**, **Receives: Boar** (or any discovered species), **Destination: None** → OK.
5. When a tamed **juvenile** walks within range of the Breeder portal, it teleports to the Maturing pen.

For **hen eggs**, add a third portal as **Egg Collector → Egg Hen** for recipe eggs, plus **Maturing → Hen** for chicks.

---

## Portal reference

| Type | Purpose | Receives setting | Destination setting |
|------|---------|------------------|---------------------|
| **Breeder** | Sends juveniles/eggs; discovers species; mate draw | Fixed: *All juveniles* | — |
| **Maturing** | Receives juveniles and single-purpose eggs; forwards adults | Discovered species or **All** | **None** / **Farm** / **Cull** |
| **Egg Collector** | Receives dual-purpose eggs only | **Egg Hen**, etc. | — |
| **Farm** | Holding area for routed adults | Discovered species or **All** | — |
| **Cull** | Slaughter yard for routed adults | Fixed: *All adults* | — |

### Species lists (Receives dropdown)

Lists are **not hardcoded**. Breeder portals scan for tamed breedable adults and register each species once. Vanilla species (Boar, Wolf, Lox, Hen, Asksvin) appear when those animals are present; mod creatures appear under their prefab names when they pass the [breedable criteria](#for-mod-creators--breedable-criteria).

**All** is always available as a catch-all on Maturing and Farm portals.

Egg Collector dropdowns show only **dual-purpose** egg layers (e.g. **Egg Hen**).

### Maturing → adult routing

| Destination | What happens |
|-------------|--------------|
| **None** *(default)* | Adults stay on the maturing pen |
| **Farm** | Adults teleport to a **Farm** portal matching their species (or **All**) |
| **Cull** | Adults teleport to your **Cull** yard |

### Round-robin

Multiple portals with the **same type and species/egg key** share load automatically.

---

## Dynamic species discovery

Each **Breeder** portal scans within **discovery range** (default: max of `BreederScanRange` and `MateDrawRange`) for tamed adults that qualify as breedable.

- One pen of four boars → one **Boar** entry.
- Discovery runs every breeder scan — new mod creatures appear in dropdowns as soon as they are tamed and in range.
- Maturing and Farm lists include live-birth species and single-purpose egg layers.
- Egg Collector lists include dual-purpose egg layers only.

Configure `DiscoveryScanRange` to override the automatic radius.

---

## Dual-purpose vs single-purpose eggs

When an egg layer lays an egg in range of a **Breeder** portal:

| Type | Detection | Routing |
|------|-----------|---------|
| **Dual-purpose** | Egg item is an ingredient in at least one **enabled** ObjectDB recipe | **Egg Collector** (species egg key) → fallback **Maturing** (species → **All**) |
| **Single-purpose** | Egg is hatch-only (not in any enabled recipe) | **Maturing** (species → **All**) — same as live-born juveniles |

### Vanilla examples

| Animal | Egg type | Typical setup |
|--------|----------|---------------|
| **Hen** | Dual-purpose | **Egg Collector → Egg Hen** for recipe eggs; **Maturing → Hen** for chicks / overflow |
| **Asksvin** | Single-purpose | **Maturing → Asksvin** or **All** only — no Egg Collector |

### Egg Collector vs Maturing

- **Egg Collector** = dual-purpose eggs only. Use this when you want recipe eggs sent to a chest/auto-collect area separate from the hatch pen.
- **Maturing** = juveniles, single-purpose eggs, and dual-purpose egg **fallback** when no collector is registered.

---

## For mod creators — breedable criteria

Offspring Portal discovers species at runtime. No manual registration or compatibility patch is required if your creature follows Valheim's breeding component model.

### Shared requirements

1. **Tamed** adult within discovery range of a **Breeder** portal.
2. **`Procreation`** component on the adult.
3. **`Procreation.m_offspring`** references a valid prefab.

### Live-birth chain

```
Adult (Procreation)
  └── m_offspring → Juvenile Character
                        └── Growup
                              └── m_grownPrefab / m_altGrownPrefabs → Adult Character
```

### Egg-layer chain

```
Adult (Procreation)
  └── m_offspring → Egg prefab
                        └── EggGrow
                              └── m_grownPrefab → Hatchling Character
                                                    └── Growup → Adult Character
```

- If the egg item is used in any **enabled crafting recipe** → **dual-purpose** (Egg Collector eligible).
- Otherwise → **single-purpose** (Maturing routing only).

### Fallback

If the full chain cannot be parsed but the tamed adult has **`Procreation`** and is not a juvenile (no **`Growup`** on self), the mod registers the species from the **adult prefab name**.

### Species keys

- Normalized prefab names (Unity clone suffixes stripped).
- Known vanilla aliases (e.g. **Hen** prefabs → **Chicken** key, displayed as **Hen**).
- Mod creatures typically use their adult prefab name as the portal key.

### Mod author checklist

| Requirement | Live birth | Egg layer |
|-------------|:----------:|:---------:|
| `Procreation` on tamed adult | ✓ | ✓ |
| `m_offspring` assigned | ✓ | ✓ |
| Juvenile + `Growup` | ✓ | — |
| Egg + `EggGrow` | — | ✓ |
| Hatchling `Growup` → adult | ✓ | ✓ |
| Egg in enabled recipe | — | dual-purpose |

If your creature meets these rules and still does not appear, open a [GitHub issue](https://github.com/cdjensen99-sudo/Offspring_Portal/issues) with prefab names and component setup.

---

## Mate draw

Breeder portals can nudge **fed, ready-to-breed** adults toward same-species mates when they are too far apart for vanilla breeding.

**Eligible when:**

- `Procreation.ReadyForProcreation()` is true
- Not hungry, not alerted, not following the player
- Adult (no `Growup` on self)
- Within `MateDrawRange` of another eligible same-species adult

**Stops when:** already within vanilla partner range, pregnant, or no longer eligible.

Toggle with `EnableMateDraw` in config (default: **true**).

---

## Configuring a portal

**Press E** on an Offspring Portal to open the config panel.

| Field | Breeder | Maturing | Egg Collector | Farm | Cull |
|-------|:-------:|:--------:|:-------------:|:----:|:----:|
| **Name** | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Type** | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Receives** | All juveniles | ✓ | ✓ (egg types) | ✓ | All adults |
| **Destination** | — | None/Farm/Cull | — | — | — |

---

## Example setups

### Starter — juveniles only

| Location | Type | Receives | Destination |
|----------|------|----------|-------------|
| Breeding area | Breeder | — | — |
| Grow-up pen | Maturing | Boar | **None** |

### Hen farm — dual-purpose eggs

| Location | Type | Receives | Destination |
|----------|------|----------|-------------|
| Coop | Breeder | — | — |
| Egg sorting | Egg Collector | **Egg Hen** | — |
| Chick grow-out | Maturing | Hen | **None** |

### Boar production + automatic cull

| Location | Type | Receives | Destination |
|----------|------|----------|-------------|
| Breeding pen | Breeder | — | — |
| Grow-up pen | Maturing | Boar | **Cull** |
| Slaughter area | Cull | — | — |

### Mixed farm — cull boars, keep wolves

| Location | Type | Receives | Destination |
|----------|------|----------|-------------|
| Main breeder | Breeder | — | — |
| Grow-up pen | Maturing | Boar | **Cull** |
| Grow-up pen | Maturing | Wolf | **None** |
| Slaughter area | Cull | — | — |

---

## Juvenile follow (E key)

1. Aim at a **tamed juvenile** (still growing).
2. Hover shows **Follow** or **Stay**.
3. Press **E** to toggle.

**Config:** `EnableFollowCommand` (default: `true`).

Portals **teleport** juveniles when they enter breeder range. Follow helps you **move** them there.

---

## Player travel & compatibility

**Offspring portals are automation-only — not player travel nodes.**

- Players **cannot** walk through offspring portals.
- **Portal travel mods** (XPortal, Z-Portal, map-based portal UIs, etc.) — offspring portals are excluded from travel lists.

---

## Advanced config

`BepInEx/config/offspringportal.mod.cfg`

### General

| Setting | Default | Description |
|---------|---------|-------------|
| `BreederScanRange` | `10` | Breeder scan radius (m) for juveniles and eggs |
| `BreederScanIntervalSec` | `0.5` | Breeder scan interval |
| `MaturingAdultScanIntervalSec` | `30` | Adult forward scan on maturing portals |
| `TeleportCooldownSec` | `2` | Cooldown after teleport |
| `DistantTeleportTimeoutSec` | `15` | Distant zone load timeout |
| `AllowRetransport` | `false` | Allow repeat teleports |
| `EnableCapWarning` | `true` | Breeding cap radius warning |
| `EnableFollowCommand` | `true` | E key follow/stay |

### Breeding

| Setting | Default | Description |
|---------|---------|-------------|
| `DiscoveryScanRange` | `0` | Species discovery radius (0 = auto) |
| `EnableMateDraw` | `true` | Mate nudging at breeders |
| `MateDrawRange` | `15` | Max mate separation before nudging (m) |
| `MateDrawIntervalSec` | `1.5` | Mate draw scan interval |
| `MateDrawStopDistance` | `2.5` | Stop nudging when this close (m) |

Restart Valheim after changes.

---

## Multiplayer

| Rule | Detail |
|------|--------|
| **Who needs the mod** | Server/host **and every client** |
| **Where logic runs** | Server/host |
| **Dependencies** | BepInEx + Jotunn on all sides |

---

## Discord & support

- **Discord** — [Team Extreme Discord](https://discord.gg/cCNG8xKXMn)
- **GitHub Issues:** [Offspring Portal issues](https://github.com/cdjensen99-sudo/Offspring_Portal/issues)

Include mod version, SP vs MP, and other portal mods when reporting issues.

---

## FAQ & troubleshooting

### My mod creature does not appear in the dropdown

- Confirm it is **tamed** and within **discovery range** of a **Breeder** portal.
- Verify **`Procreation`** + valid offspring chain (see [breedable criteria](#for-mod-creators--breedable-criteria)).
- Wait one breeder scan cycle or walk the animal closer to the breeder portal.
- Check server log for discovery messages.

### Hen eggs go to Maturing instead of Egg Collector

- The receiving portal must be **Type: Egg Collector**, not Maturing.
- Set **Receives** to **Egg Hen** on the collector portal.
- Confirm an Egg Collector portal is registered (hover shows role).

### Asksvin eggs — do I need Egg Collector?

No. Asksvin eggs are **single-purpose** (hatch only). Route with **Maturing → Asksvin** or **All**.

### Juveniles are not teleporting

- Source = **Breeder**, destination = **Maturing**.
- **Receives** matches species or is **All**.
- Juvenile must be **tamed** and still growing.
- Within `BreederScanRange` (default 10 m).

### Eggs are not teleporting

- Egg must have **`EggGrow`** and a valid hatchling → adult chain.
- Breeder portal must be **Type: Breeder**.
- Egg within breeder scan range when laid / scanned.

### Adults are not moving to Farm / Cull

- Maturing **Destination** must be **Farm** or **Cull**.
- Matching receiver portal must exist.
- Adult scan runs every **30 s** by default.

### Mate draw not working

- `EnableMateDraw` must be **true**.
- Animals must be **fed** and **`ReadyForProcreation()`**.
- Hungry, alerted, or following animals are skipped.
- Partners must be within `MateDrawRange` (default 15 m).

### Yellow cap-radius warning

Move maturing pens farther from active breeders when possible.

### Can I travel through offspring portals?

**No.** Use a standard Valheim portal for player travel.

---

## Changelog (recent)

| Version | Highlights |
|---------|------------|
| **0.4.0** | Offspring portals excluded from player portal networks globally |
| **0.3.5** | Removed pregnant glow visual |
| **0.3.4** | Improved species discovery (single animal, wider radius, per-scan refresh) |
| **0.3.3** | Breeder mate draw; breeding config section |
| **0.3.0** | Dynamic species discovery; egg routing; Egg Collector role; mod creature support |
| **0.2.15** | Discord link and documentation updates |
| **0.2.14** | Stronger XPortal exclusion |
| **0.2.9** | Block player travel; XPortal list exclusion |

Full changelog: `CHANGELOG.md` in the mod package.

---

## Credits

**Offspring Portal** by **HW**

Questions and setup help: **[Team Extreme Discord](https://discord.gg/cCNG8xKXMn)** or **GitHub Issues**.
