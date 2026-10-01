# Changelog

**Valheim 1.0.x production release.** See below for prior version history.

## 1.4.4
- Fixed: Resolved hang when teleporting to or spawning at world spawn on the first login after launching the game.

## 1.4.0
- **Valheim 1.0.x release** — feature-complete, production-ready build for solo, hosted, and dedicated servers
- **Portal configuration gate** — automation and registry sync only apply to portals you have configured once with **E** (avoids treating stray world data as active breeders)
- **Egg Collector Configure UI** — Receives dropdown includes this portal’s saved egg type even when no hens are loaded nearby
- **Dedicated portal config** — **ClaimOwnership** + local ZDO writes (vanilla-style); server registry refresh after client edits
- **Removed** — server startup ZDO audit/cull config entries (support-only tooling used during 1.3.x troubleshooting)

## 1.3.2
- **Egg Collector Configure UI** — the Receives dropdown now includes this portal’s saved egg type (e.g. **Egg Hen**) even when no hens are loaded nearby, instead of showing “No dual-purpose egg layers discovered” for a correctly configured collector

## 1.3.1
- **Portal config networking** — Configure (E) now uses **ClaimOwnership** + local ZDO writes (vanilla-style), replacing the custom `SetConfig` routed RPC; dedicated servers refresh the portal registry snapshot after client edits

## 1.3.0
- **Valheim 1.0.x final release** — production-ready build validated on solo, hosted, and dedicated servers
- **Dedicated registry fix** — portal records in unloaded zones are no longer wiped during incremental server sync; distant maturing portals stay routable
- **Server-authoritative juvenile teleports** — the server moves the juvenile ZDO first, then notifies source client(s) to move the live creature (no false client-only success)
- **Follow (E) fix** — follow is handled through `Tameable.Interact`, which is how Valheim dispatches E on tamed creatures; portal Configure is no longer blocked by juvenile hover
- **Dedicated RPC routing** — juvenile route and follow requests are sent explicitly to the server peer; follow executes on the creature ZDO owner only
- **Egg Collector round-robin** — hen and dual-purpose eggs alternate across every matching Egg Collector portal, including collectors configured with different hen key aliases (`Egg:Chicken`, `Hen`, etc.)
- **Egg Collector config at distant bases** — the Receives dropdown includes egg types already configured on other Egg Collector portals, not only species discovered from loaded hens near breeders
- **Diagnostics** — server/client log prefixes via `OPLog`; `[Diagnostics] VerboseLogging` is off by default

## 1.2.2
- **Diagnostic logging** — verbose routing, registry sync, portal scan, and follow logs (configurable under `[Diagnostics] VerboseLogging`)
- **Maturing resolution failures** — warnings now include a full registry snapshot listing every maturing portal and whether its ZDO is live
- **Follow troubleshooting** — logs follow RPC sender/owner peers, execute failures, and LetsGo detection at startup
- **Dedicated scan visibility** — logs when breeder portal scans are skipped because the pen is outside all players' active zones

## 1.2.1
- **Dedicated server/client desync fix** — the server kept the full portal registry while clients only saw portals in loaded chunks, so breeders could show “No Maturing portal” even when routing worked
- **Server registry sync** — on join and when portals are placed, configured, or destroyed, the server pushes its portal list to clients so connection status and rune glow stay accurate for distant pens
- **Unloaded destination teleports** — execute routing RPC now includes maturing portal position and rotation so juveniles can teleport when the destination chunk is not loaded on the client
- **Stale portal cleanup** — destroyed or removed portals are pruned from the server registry so juveniles no longer route to dismantled portal sites
- **Follow on dedicated** — juvenile follow (E) invokes on the creature owner peer when LetsGo is not installed

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
