# Changelog

## 1.0.1
- **Stable Valheim 1.0.x ship release** (tested on 1.0.7; compatible with 1.0.x hotfixes)
- Fix Hammer build menu showing raw localization keys for the Offspring Portal piece name and description

## 1.0.0
- **Valheim 1.0.7 release** — first stable 1.0-compatible build
- Valheim 1.0 API fixes (`Hoverable.GetHoverOffset`, deferred prefab registration, portal piece registration retry)
- Egg routing: dual-purpose eggs to Egg Collector, single-purpose (asksvin) to Maturing; improved prefab chain resolution and registry fallbacks
- Mate draw yields to vanilla breeding AI when mates are in range (fixes lox/moose and other species not breeding)
- Juvenile follow cleared on grow-up so adults do not retain follow state
- Full end-to-end testing: boar, wolf, lox, hen, asksvin, moose — breed → maturing → farm round-robin
- Updated Thunderstore dependencies for BepInEx 5.4.2350 and Jotunn 2.30.0

## 0.4.0
- Offspring portals are excluded from player portal networks globally — they no longer appear in travel mod lists or map-based portal UIs

## 0.3.5
- Removed pregnant glow visual (out of scope for this mod)
- Updated README, wiki, and Discord docs: dynamic discovery, egg routing, mate draw, mod creator breedable criteria

## 0.3.4
- Fix species discovery with a single animal (wider discovery radius, fallback registration)
- Refresh breedable species list every breeder scan instead of every 5 seconds
- Mate draw skips hungry animals explicitly
- Pregnant glow attaches to existing animals on load; emissive sphere + stronger light

## 0.3.3
- Breeder portal mate draw: fed, ready adults within 15m nudge toward each other to reach breeding range
- Subtle warm glow on pregnant breedable adults
- Breeding config section in `offspringportal.mod.cfg`

## 0.3.0
- Dynamic species discovery from tamed adults near breeder portals (no hardcoded species list)
- Egg routing: breeder portals send laid eggs to Egg Collector or maturing pens
- Egg Collector portal role for dual-purpose eggs (e.g. hen eggs)
- Mod creature support via breedable criteria (Procreation, Growup, etc.)
- Juvenile follow command (press E on tamed juveniles)

## 0.2.14
- Fixed offspring portals reappearing in XPortal as "(No Name)" entries (stronger XPortal list filtering and purge on world load)

## 0.2.13
- Fixed portal hover text showing vanilla portal tag instead of offspring portal config

## 0.2.12
- Fixed portal config UI not opening on E key for some portal setups

## 0.2.11
- Fixed juvenile routing when destination pen is in an unloaded zone (deferred teleport)

## 0.2.10
- Fixed portal registry not rebuilding after world load in multiplayer

## 0.2.9
- Block player travel to and from offspring portals (fixes XPortal bounce-back)
- Exclude offspring portals from XPortal's portal list when XPortal is installed

## 0.2.8
- Adult routing from maturing portals (Farm, Cull, None)
- Shared Cull portal for all species

## 0.2.7
- Portal config UI (name, role, species, adult destination)
- Custom hover text per portal

## 0.2.6
- Round-robin load balancing across multiple portals of the same type and species

## 0.2.5
- Breeding cap warning on maturing portal hover when inside vanilla cap radius

## 0.2.4
- Distant pen teleport: wait for zone load before giving up

## 0.2.3
- Multiplayer: routing runs on server; config RPC for clients

## 0.2.2
- Fixed juveniles not routing when breeder portal has no declared species yet

## 0.2.1
- Initial Thunderstore release: breeder → maturing juvenile routing
