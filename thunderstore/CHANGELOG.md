# Changelog

## 1.2.0
- **Valheim 1.0.x final release** — validated on solo, hosted, and dedicated servers
- **Dedicated server routing** — portal scans honor connected players' active zones (not the headless server origin)
- **Dedicated teleports** — server moves juveniles and eggs by ZDO when creatures are not instantiated locally; clients claim ownership before applying moves
- **Routing RPC** — execute teleports on both the requesting peer and the ZDO owner when they differ
- **Routing diagnostics** — log when no maturing or egg destination is registered on the server

## 1.1.1
- **Dedicated server routing** — scans use connected players' active zones (not the empty dedicated origin)
- **Dedicated teleports** — server moves juveniles by ZDO when the live creature is not instantiated; clients claim ownership before applying the move

## 1.1.0
- **Valheim 1.0.x production release** — stable, feature-complete build for 1.0.x (tested through 1.0.7+ hotfixes)
- **Multiplayer routing fix** — server refreshes portal registry from world ZDOs before resolving routes; execute teleports on the client with the active zone loaded
- **Client registry sync** — configuring a portal updates the local registry immediately (fixes dark runes and stale status on clients)
- **Connection status fix** — maturing/breeder portals no longer list themselves as upstream sources in the Configure Portal UI
- **Registry refresh** — hover text, rune glow, and config status rebuild from ZDO data before displaying connection state
- **Follow RPC fix** — juvenile follow executes on the requesting player's client; follow AI runs on ZDO owner (when LetsGo is not installed)
- Includes all fixes from 1.0.2 through 1.0.7 (MP RPC routing, connected glow, egg collector preference, LetsGo compatibility, hammer icon, log spam fixes)

## 1.0.7
- Hen eggs now prefer Egg Collector over Maturing when any Egg Collector portal exists
- Normalize Egg Collector portal species keys (Egg:Chicken) for reliable matching

## 1.0.6
- Cleaner hammer menu icon: no particle burst, subdued rune glow, isometric portal frame only
- Disable OP juvenile follow when LetsGo is installed (instead of Beast Hird)

## 1.0.5
- Fix config panel status for Breeder portals (was showing stale "Receives from" text)
- Maturing portals now show both upstream and downstream routes when configured
- Omit route lines when no matching destination portals exist

## 1.0.4
- Fix NullReferenceException spam from config retry running before ZNet exists (main menu / loading)

## 1.0.3
- Fix log spam/errors from ZNetView Awake hook running portal init during object load
- Guard portal emission updates when shader lacks `_EmissionColor`
- Guard routing RPCs and scans until world/network is ready; prevent duplicate RPC registration

## 1.0.2
- **Multiplayer routing fix** — Scans run on any peer with portals in their active zone; server validates destinations via RPC (listen server and dedicated)
- **Connected portal glow** — Runes light up when routing chain is complete (vanilla-style emission)
- **Connection status** — `[Connected]` / `[Unconnected]` on hover and live preview in Configure Portal UI
- **Config RPC retry** — Client-placed portal config retries when ZDO is not yet available on server
- **Hammer menu icon** — Custom rendered icon distinguishes offspring portal from vanilla portal
- Corrected multiplayer documentation (any player in active range, not host proximity)

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
