# Changelog

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
- Config section `Breeding` for mate draw and pregnant glow settings

## 0.3.0
- Dynamic breedable species discovery from breeder portals (Procreation, Growup, egg layers)
- Maturing and Farm portal species lists built from discovered pens (deduped, includes All)
- Mod creature support via prefab-name species keys (no manual creature list)
- Egg routing at breeder: dual-purpose eggs to Egg Collector portal, else maturing (species then All)
- Single-use hatch-only eggs follow the same maturing routing as live-born juveniles
- New Egg Collector portal role for dual-purpose egg disposition (heat/no heat at destination)

## 0.2.15
- Thunderstore README and wiki: Team Extreme Discord link, feature list, and support section updates

## 0.2.14
- Fixed offspring portals reappearing in XPortal as "(No Name)" entries (stronger XPortal list filtering and purge on world load)
- README and wiki: Discord community section, feature list, and troubleshooting updates

## 0.2.13
- Fixed juvenile follow/stay command not working for players who joined a hosted game (routes follow toggle through server RPC)

## 0.2.12
- Fixed startup Harmony error: removed invalid ZNetView.Start patch (Valheim's ZNetView has no Start method)

## 0.2.11
- Fixed offspring portals placed by remote players in hosted games not routing juveniles until rebuilt
- Fixed portals spawned via Prefabhammer (or other tools that skip standard placement) not initializing scanners or registry entries

## 0.2.10
- Thunderstore README: GitHub Issues link for support, player-travel note, source repo link

## 0.2.9
- Block player travel to and from offspring portals (fixes XPortal bounce-back)
- Exclude offspring portals from XPortal's portal list when XPortal is installed

## 0.2.8
- Fixed follow on wolf cubs and other AnimalAI juveniles (no Tameable/MonsterAI — uses custom follow path)
- Juvenile crosshair detection no longer requires Tameable component

## 0.2.7
- Fixed follow command not firing in pens: scan full crosshair raycast for juveniles and set hover target before E is processed
- Apply follow directly on server/host instead of relying on misrouted client RPC

## 0.2.6
- Fixed juvenile follow command: use vanilla follow/stay, correct hover targeting in pens, and show follow hint on crosshair

## 0.2.5
- Fixed juvenile follow command for MonsterAI species (Boar, Wolf, Lox, Asksvin)

## 0.2.4
- Fixed portal hover still showing XPortal text — use Postfix after all mods and stronger offspring portal detection

## 0.2.3
- Fixed portal hover text overridden by XPortal (and similar mods) — Offspring Portal hover now wins

## 0.2.2
- Hover warning on maturing portals when Destination is Farm/Cull but no receiver portal exists

## 0.2.1
- Maturing portals: Destination dropdown (None / Farm / Cull) replaces forward-adults checkbox
- None disables adult teleporting; missing Farm/Cull portal shows a warning and skips routing
- Added Farm receiver portal type for species-separated adult routing
- Cull yard is shared across all species; Hen label in UI

## 0.2.0
- Added three portal types: Breeder, Maturing, and Cull
- Maturing portals can optionally forward adults to cull pens (checkbox in config UI)
- Adult scan runs every 30 seconds on maturing portals with forwarding enabled (configurable)
- Existing pen portals migrate automatically to Maturing type

## 0.1.18
- Reduced portal build cost to 20 Fine Wood, 10 Greydwarf Eyes, and 2 Surtling Cores

## 0.1.17
- Added custom portal Name field to the config UI
- Hover text now shows Name plus Role (breeder) or Accepts (pen)
- Renamed Destination dropdown label to Accepts; breeders show "All juveniles"

## 0.1.16
- Fixed config panel OK/Cancel buttons overlapping the Destination dropdown
- Fixed player warning message stuck on screen while standing in portal range

## 0.1.15
- Fixed juvenile floating after teleport
- Fixed follow command RPC for AnimalAI juveniles (follow deferred for polish)
- Ground snap for teleported juveniles

## 0.1.0
- Initial release: breeder/pen portals, species config UI, distant teleport, cap warnings
