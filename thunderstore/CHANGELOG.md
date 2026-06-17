# Changelog

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
