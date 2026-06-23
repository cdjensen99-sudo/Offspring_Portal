# Offspring Portal — Wiki

> **Automate breeding pens in Valheim.** Teleport tamed juveniles from breeder areas to maturing pens automatically — and optionally route grown adults to farm or cull yards.

**Current version:** 0.2.9 · **Author:** HW · **Team:** HW

---

## Quick links

| Topic | Jump to |
|-------|---------|
| First-time setup | [Quick start](#quick-start) |
| Portal types | [Portal reference](#portal-reference) |
| Example farms | [Example setups](#example-setups) |
| Config UI | [Configuring a portal](#configuring-a-portal) |
| Follow command | [Juvenile follow (E key)](#juvenile-follow-e-key) |
| Player travel / XPortal | [Player travel & compatibility](#player-travel--compatibility) |
| Config file | [Advanced config](#advanced-config) |
| Multiplayer | [Multiplayer](#multiplayer) |
| Problems | [FAQ & troubleshooting](#faq--troubleshooting) |

---

## What this mod does

Offspring Portal adds a **custom buildable portal** (Hammer menu) that **does not work like a normal travel portal**. Instead, it automates animal logistics:

- **Breeder portals** scan for tamed juveniles nearby and teleport them to the correct **Maturing** pen.
- **Maturing portals** receive juveniles and can optionally send **adults** to a **Farm** or **Cull** portal when they grow up.
- **Farm** and **Cull** portals receive routed adults (Cull is shared across all species).

This keeps vanilla breeding caps from stalling production when your grow-up pen is far from your breeder.

**Build cost:** 20 Fine Wood · 10 Greydwarf Eyes · 2 Surtling Cores

---

## Quick start

### Requirements

Install on **server/host and every client**:

- [BepInEx Pack for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
- **Offspring Portal** (this mod)

Most mod managers install dependencies automatically.

### Minimum setup (2 portals)

1. **Build** an Offspring Portal at your breeding area (Hammer → Building).
2. **Press E** on the portal → set **Type: Breeder**, give it a **Name** (e.g. `Boar Farm`) → OK.
3. **Build** a second portal at your grow-up pen.
4. **Press E** → set **Type: Maturing**, **Receives: Boar**, **Destination: None**, name it (e.g. `Boar Maturing`) → OK.
5. When a tamed **juvenile boar** walks within range of the Breeder portal, it teleports to the Maturing pen.

```
  Breeding pen                         Grow-up pen
 ┌─────────────┐                      ┌─────────────┐
 │  Breeder    │  ── juvenile ──►   │  Maturing   │
 │  portal     │     teleports       │  portal     │
 └─────────────┘                      └─────────────┘
```

Hover any portal to see its name, role, species, and warnings.

---

## Portal reference

| Type | Purpose | Receives setting | Destination setting |
|------|---------|------------------|---------------------|
| **Breeder** | Sends juveniles out to matching maturing pens | Fixed: *All juveniles* | — |
| **Maturing** | Receives juveniles; optionally forwards adults | Species or **All** | **None** / **Farm** / **Cull** |
| **Farm** | Holding area for routed adults | Species or **All** | — |
| **Cull** | Slaughter yard for routed adults | Fixed: *All adults* | — |

### Supported species (Receives dropdown)

| UI label | Species |
|----------|---------|
| Boar | Boar piglets → adult boars |
| Wolf | Wolf cubs → adult wolves |
| Lox | Lox calves → adult lox |
| Hen | Chicken chicks → hens |
| Asksvin | Asksvin calves → adult asksvin |
| All | Any supported juvenile/adult species |

### Maturing → adult routing

When juveniles **grow up** on a Maturing portal, the portal checks its **Destination**:

| Destination | What happens |
|-------------|--------------|
| **None** *(default)* | Adults stay on the maturing pen |
| **Farm** | Adults teleport to a **Farm** portal matching their species (or All) |
| **Cull** | Adults teleport to your **Cull** yard (one Cull portal serves every species) |

If Destination is **Farm** or **Cull** but no matching receiver portal exists, hover shows a **yellow warning** and adults are **not** teleported until you place one.

### Round-robin

Multiple portals with the **same type and species** share load automatically. Example: two **Maturing / Boar** pens — juveniles alternate between them.

---

## Configuring a portal

**Press E** on an Offspring Portal to open the config panel.

| Field | Breeder | Maturing | Farm | Cull |
|-------|:-------:|:--------:|:----:|:----:|
| **Name** | ✓ | ✓ | ✓ | ✓ |
| **Type** | ✓ | ✓ | ✓ | ✓ |
| **Receives** | All juveniles *(fixed)* | ✓ | ✓ | All adults *(fixed)* |
| **Destination** | — | None / Farm / Cull | — | — |

**Tips**

- **Name your portals** — hover text uses your custom names.
- **Destination: None** is the safe default until you are ready for adult automation.
- For **mixed species with different outcomes**, use **one Maturing portal per species**. They can sit in the **same building**; routing is by portal settings, not physical location.

---

## Example setups

### Starter — juveniles only (2 portals)

| Portal location | Type | Receives | Destination | Example name |
|-----------------|------|----------|-------------|--------------|
| Breeding area | Breeder | — | — | `Boar Farm` |
| Grow-up pen | Maturing | Boar | **None** | `Boar Maturing` |

---

### Boar production + automatic cull

| Portal location | Type | Receives | Destination | Example name |
|-----------------|------|----------|-------------|--------------|
| Breeding pen | Breeder | — | — | `Boar Farm` |
| Grow-up pen | Maturing | Boar | **Cull** | `Boar Maturing` |
| Slaughter area | Cull | — | — | `Cull Yard` |

One **Cull** portal is enough for every species you mark for culling.

---

### Mixed farm — cull boars, keep wolves on-site

Use **one Maturing portal per species** when adults need different destinations. Both maturing portals can share one grow-up pen.

| Portal location | Type | Receives | Destination | Example name |
|-----------------|------|----------|-------------|--------------|
| Main breeder | Breeder | — | — | `Mixed Farm` |
| Grow-up pen | Maturing | Boar | **Cull** | `Boar Maturing` |
| Grow-up pen | Maturing | Wolf | **None** | `Wolf Maturing` |
| Slaughter area | Cull | — | — | `Cull Yard` |

**Routing**

- Breeder sends boar juveniles → Boar Maturing; wolf juveniles → Wolf Maturing.
- Boars that grow up → Cull yard.
- Wolves that grow up → stay on maturing pen.

---

### Return adults to the farm

| Portal location | Type | Receives | Destination | Example name |
|-----------------|------|----------|-------------|--------------|
| Breeder | Breeder | — | — | `Boar Farm` |
| Distant grow-up | Maturing | Boar | **Farm** | `Boar Maturing` |
| Holding pen | Farm | Boar | — | `Boar Holding` |

Match **Receives** on the Farm portal to the species on the Maturing portal.

---

## Juvenile follow (E key)

Separate from portal automation — useful for **leading** juveniles into range before they grow up.

1. Aim at a **tamed juvenile** (still growing, not adult form).
2. Hover shows **Follow** or **Stay**.
3. Press **E** to toggle.

Works on boar piglets, wolf cubs, lox calves, hen chicks, asksvin calves, and other supported juveniles.

**Config toggle:** `EnableFollowCommand` in `BepInEx/config/offspringportal.mod.cfg` (default: `true`).

| Action | Effect |
|--------|--------|
| Follow | Juvenile follows you (vanilla follow behavior) |
| Stay | Juvenile holds position |

Portals **teleport** juveniles when they enter a Breeder scan radius. Follow helps you **move** them there.

---

## Player travel & compatibility

**Offspring portals are automation-only — not player travel nodes.**

- Players **cannot** walk through offspring portals to travel the map.
- Vanilla portal pairing to offspring portals is **blocked**.
- When **[XPortal](https://thunderstore.io/c/valheim/p/OdinPlus/XPortal/)** is installed, offspring portals are **excluded** from XPortal's travel list (v0.2.9+).

Most portal-network mods that use vanilla `TeleportWorld` mechanics or `ZDOMan.GetPortals()` are covered by the same guards. Mods that teleport players via custom code (e.g. direct coordinate teleport) may behave differently.

**Future:** Named XPortal / travel integration may be added as an optional feature. For now, use a **standard portal** for player travel.

---

## Advanced config

After first launch, edit:

`BepInEx/config/offspringportal.mod.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| `BreederScanRange` | `10` | Scan radius (meters) around Breeder portals |
| `BreederScanIntervalSec` | `0.5` | How often breeders scan for juveniles |
| `MaturingAdultScanIntervalSec` | `30` | How often maturing portals scan for adults to forward |
| `TeleportCooldownSec` | `2` | Per-animal cooldown after teleport |
| `DistantTeleportTimeoutSec` | `15` | Wait for distant zones to load before giving up |
| `AllowRetransport` | `false` | Allow the same animal to teleport again |
| `EnableCapWarning` | `true` | Warn when a maturing pen is inside breeding cap radius |
| `EnableFollowCommand` | `true` | E key follow/stay on juveniles |

**Restart Valheim** after changing config values.

---

## Multiplayer

| Rule | Detail |
|------|--------|
| **Who needs the mod** | Server/host **and every client** |
| **Why** | Custom portal piece, config UI, and hover text are synced assets |
| **Where logic runs** | Teleport and routing run on the **server/host** |
| **Dependencies** | BepInEx + Jotunn on all sides |

Dedicated servers: install the mod on the server and ensure all connecting players have matching client installs.

---

## FAQ & troubleshooting

### Juveniles are not teleporting

- Confirm the source portal is **Breeder** and the destination is **Maturing**.
- Confirm **Receives** on the Maturing portal matches the juvenile's species (or is **All**).
- The juvenile must be **tamed** and still in **juvenile** form (not adult).
- The juvenile must be within **BreederScanRange** (default 10 m) of the Breeder portal.
- Check server log for routing messages if you are hosting.

### Adults are not moving to Farm / Cull

- Maturing portal **Destination** must be **Farm** or **Cull** (not None).
- A matching **Farm** or **Cull** portal must exist and be registered.
- Adult scan runs every **30 seconds** by default — wait one scan cycle.
- Hover the Maturing portal — a **yellow warning** means no receiver was found.

### Yellow cap-radius warning on hover

Vanilla breeding caps apply near active breeders. If a Maturing portal is inside another species' cap radius, hover warns you. **Move the grow-up pen farther from the breeder** when possible.

### Portal shows wrong hover text / XPortal text

Update to **0.2.9+**. Offspring hover text should win over XPortal. If issues persist, report your mod list.

### Player bounced back after using XPortal on offspring portal

Fixed in **0.2.9**. Update server and all clients.

### Custom portal missing / purple cube / clients can't build

- All players need **Offspring Portal + Jotunn** installed.
- Version mismatch between server and clients can cause desync — keep versions aligned.

### Follow command not working on juveniles

- Confirm `EnableFollowCommand` is `true`.
- Aim directly at the juvenile — hover must show Follow/Stay before pressing E.
- Wolf cubs and similar **AnimalAI** juveniles are supported from **0.2.8+**.

### Can I travel through offspring portals?

**No.** Use a standard Valheim portal (or XPortal on a normal portal) for player travel.

---

## Changelog (recent)

| Version | Highlights |
|---------|------------|
| **0.2.9** | Block player travel to/from offspring portals; XPortal list exclusion |
| **0.2.8** | Follow command fix for wolf cubs / AnimalAI juveniles |
| **0.2.7** | Follow command crosshair and server-side apply fixes |
| **0.2.2** | Warning when Farm/Cull destination has no receiver |
| **0.2.1** | Farm portal type; Destination dropdown (None/Farm/Cull) |
| **0.2.0** | Breeder / Maturing / Cull portal types; adult forwarding |

Full changelog is included in the mod package (`CHANGELOG.md`).

---

## Screenshots

Thunderstore wiki pages support **hosted image URLs** only — images inside the mod zip will not render here.

To add screenshots to this wiki:

1. Upload images to GitHub, Imgur, or similar.
2. Edit this wiki page and insert:

```markdown
![Boar farm setup](https://your-host.example/screenshot.png)
```

**Suggested screenshots**

- Hammer menu showing Offspring Portal piece
- Config UI (Name / Type / Receives / Destination)
- Two-portal starter layout (Breeder + Maturing)
- Hover text showing portal name and role
- Mixed-species setup with two Maturing portals in one pen

---

## Credits

**Offspring Portal** by **HW**

Questions and setup help: use the mod's **Discussions** tab on Thunderstore or your community thread.
