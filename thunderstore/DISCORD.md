# Offspring Portal — Discord channel copy

Use the sections below for your **#offspring-portal** (or similar) channel topic, pinned post, or mod-info bot.

Replace `https://discord.gg/cCNG8xKXMn` in `manifest.json` → `website_url` if the invite ever changes.

---

## Channel description (topic / About)

Offspring Portal automates breeding pens in Valheim. Build offspring portals at your breeder and grow-up areas — tamed juveniles and eggs teleport to the right **Maturing** or **Egg Collector** pen by species. Species lists are discovered automatically from tamed adults near breeder portals (mod creature friendly). Optionally route grown adults to **Farm** or **Cull**, and nudge ready mates together at breeders. Press **E** on juveniles to toggle follow/stay. Animal automation only — players cannot travel through these portals. Multiplayer: host + all clients. Requires BepInEx and Jotunn.

**Thunderstore:** https://thunderstore.io/c/valheim/p/HW/Offspring_Portal/  
**Issues:** https://github.com/cdjensen99-sudo/Offspring_Portal/issues  
**Discord:** https://discord.gg/cCNG8xKXMn

---

## Features (pinned message / channel list)

- **Custom offspring portal** — Hammer build piece (20 Fine Wood, 10 Greydwarf Eyes, 2 Surtling Cores)
- **Five portal roles** — Breeder, Maturing, Farm, Egg Collector, and Cull
- **Dynamic species discovery** — Dropdowns built from tamed adults near breeder portals (deduped)
- **Mod creature support** — Creatures with Procreation + Growup/EggGrow chains auto-register
- **Automatic juvenile routing** — Breeder → Maturing by species (or All)
- **Automatic egg routing** — Dual-purpose eggs → Egg Collector; single-purpose → Maturing
- **Mate draw** — Fed, ready adults nudge toward mates near breeder portals
- **Round-robin** — Multiple portals of the same type share load
- **Optional adult routing** — Maturing → None / Farm / Cull
- **Portal config UI** — Press **E** on a portal
- **Juvenile follow** — Press **E** on juveniles for Follow / Stay
- **Portal network compatible** — Offspring portals excluded from player travel lists
- **Configurable** — Scan range, mate draw, discovery radius, and more

---

## Minimum setup (quick reference)

1. Place a portal at the **breeding area** → **E** → Type **Breeder** → name it → OK  
2. Place a portal at the **grow-up pen** → **E** → Type **Maturing** → **Receives** = species → **Destination** = None → OK  
3. For **hen eggs**: add **Egg Collector → Egg Hen** for recipe eggs; **Maturing → Hen** for chicks  
4. Tamed juveniles and eggs within breeder range teleport automatically.

**Dual-purpose egg** = used in crafting recipes (hen). **Single-purpose** = hatch only (asksvin) → Maturing only.

---

## Mod creators (short)

Breedable if tamed adult has **Procreation** + valid chain: live birth (`Growup` juvenile → adult) or egg layer (`EggGrow` → hatchling `Growup` → adult). Dual-purpose eggs must appear in an enabled ObjectDB recipe. Species key = adult prefab name (vanilla aliases like Hen → Chicken).
