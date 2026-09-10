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
    public static ConfigEntry<bool> EnableCapWarning;
    public static ConfigEntry<float> MaturingAdultScanIntervalSec;
    public static ConfigEntry<float> DiscoveryScanRange;
    public static ConfigEntry<bool> EnableMateDraw;
    public static ConfigEntry<float> MateDrawRange;
    public static ConfigEntry<float> MateDrawIntervalSec;
    public static ConfigEntry<float> MateDrawStopDistance;

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
