# Offspring Portal

**Automate your breeding pens.** Offspring Portal teleports tamed juveniles from your breeder area to distant maturing pens the moment they wander into range — keeping vanilla population caps from stalling production.

**Press E on juveniles to toggle follow/stay** — useful for leading piglets and wolf cubs through portals or into pens before they grow up.

Build a portal, name it, configure it once, and let the mod handle the logistics.

---

## At a glance

| Portal type | What it does |
|-------------|--------------|
| **Breeder** | Sends juveniles out to matching maturing pens |
| **Maturing** | Receives juveniles; optionally forwards grown adults |
| **Farm** | Receives adults routed back from maturing pens (by species) |
| **Cull** | One shared yard — receives adults marked for slaughter |

**Juvenile follow:** With `EnableFollowCommand` on (default), look at a tamed juvenile and press **E** to toggle **Follow** / **Stay**. Works on boar piglets, wolf cubs, and other juveniles still growing up.

**Minimum setup:** 2 portals (Breeder + Maturing). Farm and Cull are optional when you want adult automation.

**Build cost:** 20 Fine Wood · 10 Greydwarf Eyes · 2 Surtling Cores

---

## Juvenile follow command

When a juvenile is still growing (has not reached its adult form), you can command it directly:

1. Aim at the tamed juvenile — hover shows **Follow** or **Stay**
2. Press **E** to toggle
3. Press **E** again to make it stay

This is separate from portal automation: follow helps you **move** juveniles; portals **teleport** them when they enter a breeder's scan radius.

Toggle in config: `EnableFollowCommand` in `BepInEx/config/offspringportal.mod.cfg` (default: `true`).

---

## How juveniles move

```
Breeder Portal                    Maturing Portal
  (at breeding pen)    ────────►   (at grow-up pen)
  Role: Breeder                   Receives: Boar / Wolf / Hen / All …
```

1. Place a portal at your **breeding area** → configure as **Breeder**
2. Place a portal at your **grow-up area** → configure as **Maturing** → set **Receives** to a species (or All)
3. When a tamed juvenile enters the breeder portal's scan radius, it teleports to the next available maturing pen for its species (round-robin if you have several)

Hover any portal to see its **Name**, role, and warnings.

---

## Maturing pen destinations (adults)

Each **Maturing** portal has one **Receives** species (or All) and one **Destination** for adults that grow up on that portal. The mod applies that destination to every adult of the species that portal receives.

When juveniles grow up, the maturing portal can optionally move adults elsewhere:

| Destination | Behavior |
|-------------|----------|
| **None** | Adults stay on the maturing pen *(default — great for a simple 2-portal setup)* |
| **Farm** | Adults teleport to a **Farm** portal matching their species (or All) |
| **Cull** | Adults teleport to your **Cull** yard — one Cull portal serves every species |

If Destination is Farm or Cull but no receiver portal exists yet, you'll see a **yellow hover warning** and adults won't teleport until you place one.

**Mixed species, different outcomes:** Use one Maturing portal per species when adults need different destinations (e.g. boars → Cull, wolves → None). Place them in the **same building** if you like — the **Breeder** sends each juvenile to the Maturing portal whose **Receives** matches that species. You do not hand-sort animals; the portal network routes them.

---

## Example setups

### Starter — 2 portals (juveniles only)

Perfect first build. No adult routing needed.

| Portal | Type | Receives | Destination | Name example |
|--------|------|----------|-------------|--------------|
| Breeding area | Breeder | — | — | `Boar Farm` |
| Grow-up pen | Maturing | Boar | **None** | `Boar Maturing` |

Juveniles leave the breeder automatically. Adults stay on the maturing pen until you handle them.

---

### Boar production + cull automation

Cull grown boars without walking them across the map.

| Portal | Type | Receives | Destination | Name example |
|--------|------|----------|-------------|--------------|
| Breeding pen | Breeder | — | — | `Boar Farm` |
| Grow-up pen | Maturing | Boar | **Cull** | `Boar Maturing` |
| Slaughter area | Cull | — | — | `Boar Cull Yard` |

One **Cull** portal is enough for every species you mark for culling.

---

### Cull boars, keep wolves on-site

Mixed species with different adult outcomes. Use **one Maturing portal per species** — each portal sorts its own juveniles and adults. Both maturing portals can sit in the **same grow-up pen**; only the portal **Receives** / **Destination** settings differ.

| Portal | Type | Receives | Destination | Name example |
|--------|------|----------|-------------|--------------|
| Main breeder | Breeder | — | — | `Mixed Farm` |
| Grow-up pen | Maturing | Boar | **Cull** | `Boar Maturing` |
| Grow-up pen | Maturing | Wolf | **None** | `Wolf Maturing` |
| Cull yard | Cull | — | — | `Cull Yard` |

**How routing works:**

- The **Breeder** sends boar juveniles to the Boar maturing portal and wolf juveniles to the Wolf maturing portal automatically.
- When boars grow up on the Boar maturing portal, they teleport to the **Cull yard**.
- When wolves grow up on the Wolf maturing portal, they **stay** (Destination: None).
- You only need **one Cull portal** for every species you mark for culling.

---

### Return adults to the farm

Send grown animals back to holding pens near your breeder.

| Portal | Type | Receives | Destination | Name example |
|--------|------|----------|-------------|--------------|
| Breeder | Breeder | — | — | `Boar Farm` |
| Distant grow-up | Maturing | Boar | **Farm** | `Boar Maturing` |
| Farm holding | Farm | Boar | — | `Boar Holding` |

Place the **Farm** portal where you want adults to land. Match **Receives** to the species on the maturing pen.

---

### Multiple maturing pens (round-robin)

Two boar maturing pens? Juveniles alternate between them automatically.

| Portal | Type | Receives |
|--------|------|----------|
| Breeder | Breeder | — |
| North pen | Maturing | Boar |
| South pen | Maturing | Boar |

Same species + same role = round-robin. Works for Farm and Cull routing too when you have multiple receivers.

---

## Config UI (press E on a portal)

| Field | Breeder | Maturing | Farm | Cull |
|-------|---------|----------|------|------|
| **Name** | ✓ | ✓ | ✓ | ✓ |
| **Type** | ✓ | ✓ | ✓ | ✓ |
| **Receives** | All juveniles *(fixed)* | ✓ | ✓ | All adults *(fixed)* |
| **Destination** | — | None / Farm / Cull | — | — |

**Species in Receives:** Boar · Wolf · Lox · Hen · Asksvin · All

---

## Config file

After first launch, edit:

`BepInEx/config/offspringportal.mod.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| `BreederScanRange` | 10 | Scan radius (meters) around portals |
| `BreederScanIntervalSec` | 0.5 | How often breeders scan for juveniles |
| `MaturingAdultScanIntervalSec` | 30 | How often maturing pens scan for adults to forward |
| `TeleportCooldownSec` | 2 | Per-animal cooldown after teleport |
| `DistantTeleportTimeoutSec` | 15 | Wait for distant zones to load before giving up |
| `AllowRetransport` | false | Allow the same animal to teleport again |
| `EnableCapWarning` | true | Warn when a maturing pen is inside breeding cap radius |
| `EnableFollowCommand` | true | Press E on tamed juveniles to toggle follow/stay |

Restart Valheim after changing config values.

---

## Multiplayer

Install on **server/host and every client**. Everyone needs the mod to see the custom portal piece, open the config UI, and get correct hover text. BepInEx and Jotunn are required on all sides.

Teleport logic runs on the server/host.

---

## Screenshots & images on Thunderstore

Thunderstore packages include **`icon.png`** (256×256) as the mod's listing icon — that's the main image upload in the zip.

The **README** is Markdown and renders on your mod page, but **inline screenshots must be hosted elsewhere** (GitHub, Imgur, etc.) and linked with a full URL:

```markdown
![Boar farm setup](https://example.com/your-screenshot.png)
```

Local images inside the zip won't display on the Thunderstore page. If you want a visual gallery later, host images online and add links here — or share setup screenshots in your Discord / mod discussion thread.

---

## Requirements

Installed automatically with most mod managers:

- [BepInEx Pack](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

### Manual install

1. Install BepInEx and Jotunn.
2. Place `OffspringPortal.dll` in `BepInEx/plugins/`.
3. Launch once to generate the config file.

---

## Tips

- **Name your portals** — hover text shows custom names (`Boar Farm`, `Wolf Maturing`, etc.)
- **Press E on juveniles** to toggle follow/stay when moving them into range of a breeder portal
- **Destination: None** is the safe default until you're ready for Farm or Cull automation
- **One Cull portal** can serve your entire world — boars, wolves, lox, etc.
- **One Maturing portal = one species rule** — for mixed keep/cull setups, add a Maturing portal per species (they can share one pen)
- Maturing pens too close to a breeder trigger a **cap radius warning** — keep grow-up pens outside the vanilla breeding cap range when possible
- **Players** cannot travel through offspring portals — juveniles only

---

## Credits

By **HW**
