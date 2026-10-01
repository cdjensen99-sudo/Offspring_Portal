using BepInEx;
using BepInEx.Configuration;

namespace OffspringPortal;

public static class ModConfig
{
    public static ConfigEntry<float> TeleportCooldownSec;
    public static ConfigEntry<float> BreederScanRange;
    public static ConfigEntry<float> BreederScanIntervalSec;
    public static ConfigEntry<float> DistantTeleportTimeoutSec;
    public static ConfigEntry<bool> AllowRetransport;
    public static ConfigEntry<bool> EnableFollowCommand;
    public static ConfigEntry<UnityEngine.KeyCode> FollowInteractButton;
    public static ConfigEntry<bool> EnableCapWarning;
    public static ConfigEntry<float> MaturingAdultScanIntervalSec;
    public static ConfigEntry<float> DiscoveryScanRange;
    public static ConfigEntry<bool> EnableMateDraw;
    public static ConfigEntry<float> MateDrawRange;
    public static ConfigEntry<float> MateDrawIntervalSec;
    public static ConfigEntry<float> MateDrawStopDistance;
    public static ConfigEntry<bool> VerboseLogging;
    public static ConfigEntry<bool> EnableZNetSceneRemoveObjectsPatch;
    public static ConfigEntry<float> PlayerSpawnTravelGuardGraceSec;
    public static ConfigEntry<bool> EnableStartupPortalAudit;
    public static ConfigEntry<bool> StartupAuditRemoveOrphans;
    public static ConfigEntry<bool> StartupAuditEntireWorld;
    public static ConfigEntry<bool> StartupAuditUseWorldOrigin;
    public static ConfigEntry<float> StartupAuditRadiusMeters;
    public static ConfigEntry<float> SpawnCleanupRadiusMeters;
    public static ConfigEntry<float> SpawnSkyMinY;
    public static ConfigEntry<float> OrphanVoidMinY;
    public static ConfigEntry<bool> RemoveAllUnconfiguredPortalZdos;
    public static ConfigEntry<bool> EnablePortalConsoleCommands;
    public static ConfigEntry<bool> ConsoleCommandsRequireAdmin;
    public static ConfigEntry<bool> ConsoleCommandsServerOnly;
    public static ConfigEntry<float> CapWarningRefreshIntervalSec;
    public static ConfigEntry<bool> CleanSpawnViewsOnPlayerSpawn;
    public static ConfigEntry<bool> EnableOrphanRejectionOnLoad;
    public static ConfigEntry<bool> PurgeSpawnOrphansOnServerConnect;
    public static ConfigEntry<bool> SaveWorldAfterSpawnOrphanPurge;
    public static ConfigEntry<bool> ConsoleCommandsAllowClientScan;

    public static void Bind(ConfigFile config)
    {
        TeleportCooldownSec = config.Bind("General", "TeleportCooldownSec", 2f,
            "Seconds between successive teleports at the same source portal.");
        BreederScanRange = config.Bind("General", "BreederScanRange", 10f,
            "Breeder portals automatically teleport tamed juveniles within this radius (meters).");
        BreederScanIntervalSec = config.Bind("General", "BreederScanIntervalSec", 0.5f,
            "How often breeder portals scan for nearby juveniles.");
        DistantTeleportTimeoutSec = config.Bind("General", "DistantTeleportTimeoutSec", 15f,
            "How long to wait for a distant pen zone to load before giving up.");
        AllowRetransport = config.Bind("General", "AllowRetransport", false,
            "If true, a juvenile can use the portal more than once.");
        EnableFollowCommand = config.Bind("General", "EnableFollowCommand", true,
            "Press E on tamed juveniles to toggle follow/stay (Boar, Wolf, Lox, Hen, Asksvin, Moose).");
        FollowInteractButton = config.Bind("General", "FollowInteractButton", UnityEngine.KeyCode.E,
            "Keyboard button used to toggle follow/stay on an eligible juvenile. Default is E.");
        EnableCapWarning = config.Bind("General", "EnableCapWarning", true,
            "Show warning when a maturing portal is within species cap radius of a breeder portal.");
        MaturingAdultScanIntervalSec = config.Bind("General", "MaturingAdultScanIntervalSec", 30f,
            "How often maturing portals scan for nearby adults to forward to cull pens.");
        DiscoveryScanRange = config.Bind("Breeding", "DiscoveryScanRange", 0f,
            "Radius for discovering breedable species at breeder portals (0 = max of breeder scan and mate draw range).");
        EnableMateDraw = config.Bind("Breeding", "EnableMateDraw", true,
            "Draw fed, ready-to-breed adults toward each other within breeder portal range.");
        MateDrawRange = config.Bind("Breeding", "MateDrawRange", 15f,
            "How far apart mates can be before breeder portals nudge them together (meters).");
        MateDrawIntervalSec = config.Bind("Breeding", "MateDrawIntervalSec", 1.5f,
            "How often breeder portals scan for mate-draw pairing.");
        MateDrawStopDistance = config.Bind("Breeding", "MateDrawStopDistance", 2.5f,
            "How close mates move before stopping (should be within vanilla breeding range).");
        VerboseLogging = config.Bind("Diagnostics", "VerboseLogging", false,
            "Log detailed routing, registry sync, scanner, and follow diagnostics to BepInEx/LogOutput.log. Enable only while troubleshooting.");
        CapWarningRefreshIntervalSec = config.Bind("Diagnostics", "CapWarningRefreshIntervalSec", 5f,
            "Minimum seconds between maturing cap-warning ZDO updates (reduces work during hover/load).");

        EnableZNetSceneRemoveObjectsPatch = config.Bind("Maintenance", "EnableZNetSceneRemoveObjectsPatch", true,
            "Safe ZNetScene.RemoveObjects replacement that skips destroyed/null ZNetViews (prevents spawn/teleport NRE loops). Set false only to use vanilla unload behavior.");
        PlayerSpawnTravelGuardGraceSec = config.Bind("Maintenance", "PlayerSpawnTravelGuardGraceSec", 20f,
            "Seconds after player spawn where player TeleportTo is never blocked near offspring portals (fixes spawn hangs with orphan OP ZDOs).");
        EnableStartupPortalAudit = config.Bind("Maintenance", "EnableStartupPortalAudit", true,
            "On dedicated/listen server startup, scan for orphan offspring_portal ZDOs (support tooling).");
        StartupAuditRemoveOrphans = config.Bind("Maintenance", "StartupAuditRemoveOrphans", false,
            "When startup audit finds orphan portal ZDOs, delete them automatically. Backup your world before enabling.");
        StartupAuditEntireWorld = config.Bind("Maintenance", "StartupAuditEntireWorld", false,
            "Scan the entire world on startup instead of a radius around spawn/origin.");
        StartupAuditUseWorldOrigin = config.Bind("Maintenance", "StartupAuditUseWorldOrigin", true,
            "When StartupAuditEntireWorld is false, scan around world origin (0,0). Set false to scan around the start altar icon.");
        StartupAuditRadiusMeters = config.Bind("Maintenance", "StartupAuditRadiusMeters", 120f,
            "Radius for startup audit when StartupAuditEntireWorld is false.");
        SpawnCleanupRadiusMeters = config.Bind("Maintenance", "SpawnCleanupRadiusMeters", 120f,
            "Horizontal distance from world origin used to classify spawn-area orphan portal ZDOs.");
        SpawnSkyMinY = config.Bind("Maintenance", "SpawnSkyMinY", 8f,
            "Unconfigured portal ZDOs above this Y near spawn are treated as sky orphans.");
        OrphanVoidMinY = config.Bind("Maintenance", "OrphanVoidMinY", -500f,
            "Portal ZDOs below this Y are treated as void orphans.");
        RemoveAllUnconfiguredPortalZdos = config.Bind("Maintenance", "RemoveAllUnconfiguredPortalZdos", false,
            "If true, any unconfigured offspring_portal ZDO anywhere is eligible for audit/cleanup. Dangerous on live worlds.");
        EnablePortalConsoleCommands = config.Bind("Maintenance", "EnablePortalConsoleCommands", true,
            "Register opscan and opclean console/chat commands for orphan offspring_portal ZDO maintenance.");
        ConsoleCommandsRequireAdmin = config.Bind("Maintenance", "ConsoleCommandsRequireAdmin", true,
            "Require Valheim admin/host for opscan/opclean (adminlist.txt on dedicated servers).");
        ConsoleCommandsServerOnly = config.Bind("Maintenance", "ConsoleCommandsServerOnly", true,
            "Only allow opscan/opclean on the game instance that is server/host (recommended on dedicated servers).");
        CleanSpawnViewsOnPlayerSpawn = config.Bind("Maintenance", "CleanSpawnViewsOnPlayerSpawn", false,
            "Legacy diagnostic: destroy unconfigured offspring portal views near spawn on player spawn. Default off — on clients that do not own the ZDO this can leave zdo.Created set without a live view and break IsAreaReady at world spawn (same hang class as the old hammer-icon bug).");
        EnableOrphanRejectionOnLoad = config.Bind("Maintenance", "EnableOrphanRejectionOnLoad", true,
            "On the server, destroy unconfigured orphan offspring_portal ZDOs as soon as they load or are placed (prevents sky floaters from persisting).");
        PurgeSpawnOrphansOnServerConnect = config.Bind("Maintenance", "PurgeSpawnOrphansOnServerConnect", true,
            "On dedicated/listen server, purge spawn-area orphan portal ZDOs when a player joins and after each player spawn.");
        SaveWorldAfterSpawnOrphanPurge = config.Bind("Maintenance", "SaveWorldAfterSpawnOrphanPurge", true,
            "After a server spawn orphan purge removes ZDOs, request a world save so they do not reappear on next boot.");
        ConsoleCommandsAllowClientScan = config.Bind("Maintenance", "ConsoleCommandsAllowClientScan", true,
            "Allow admins to run read-only opscan on the game client (opclean still requires server unless ConsoleCommandsServerOnly=false).");
    }

    public static float GetDiscoveryScanRange()
    {
        if (DiscoveryScanRange.Value > 0f)
        {
            return DiscoveryScanRange.Value;
        }

        float breederRange = BreederScanRange.Value;
        if (!EnableMateDraw.Value)
        {
            return breederRange;
        }

        return UnityEngine.Mathf.Max(breederRange, MateDrawRange.Value);
    }
}
