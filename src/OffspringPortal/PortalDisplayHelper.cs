using UnityEngine;

namespace OffspringPortal;

public static class PortalDisplayHelper
{
    public const string UnnamedDisplay = "(No Name)";
    public const int MaxNameLength = 32;

    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        name = name.Trim();
        return name.Length <= MaxNameLength ? name : name.Substring(0, MaxNameLength);
    }

    public static string GetDisplayName(ZDO zdo)
    {
        if (zdo == null)
        {
            return UnnamedDisplay;
        }

        string name = SanitizeName(zdo.GetString(ZdoFields.PortalName, string.Empty));
        return string.IsNullOrEmpty(name) ? UnnamedDisplay : name;
    }

    public static string GetDetailLine(PortalRole role, string speciesKey, AdultDestination adultDestination)
    {
        switch (role)
        {
            case PortalRole.Maturing:
                string line = $"Receives: {SpeciesKey.GetDisplayName(speciesKey)} juveniles";
                if (adultDestination != AdultDestination.None)
                {
                    line += $"\nDestination: {PortalRoleCatalog.GetDisplayName(adultDestination)}";
                }

                return line;
            case PortalRole.Farm:
                return $"Receives: {SpeciesKey.GetDisplayName(speciesKey)} adults";
            case PortalRole.Cull:
                return "Role: Cull yard";
            case PortalRole.EggCollector:
                return $"Receives: {SpeciesKey.GetDisplayName(speciesKey)}";
            default:
                return "Role: Breeder";
        }
    }

    public static string GetDetailLine(PortalRole role, SpeciesType species, AdultDestination adultDestination)
    {
        string speciesKey = species == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(species);
        return GetDetailLine(role, speciesKey, adultDestination);
    }

    public static string GetHoverText(
        ZDO zdo,
        PortalRole role,
        string speciesKey,
        AdultDestination adultDestination,
        bool capWarning)
    {
        string name = GetDisplayName(zdo);
        string detail = GetDetailLine(role, speciesKey, adultDestination);
        string warning = BuildWarnings(role, speciesKey, adultDestination, capWarning);

        return Localization.instance.Localize(
            $"Name: {name}\n{detail}{warning}\n[<color=yellow><b>$KEY_Use</b></color>] Configure");
    }

    public static string GetHoverText(
        ZDO zdo,
        PortalRole role,
        SpeciesType species,
        AdultDestination adultDestination,
        bool capWarning)
    {
        string speciesKey = species == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(species);
        return GetHoverText(zdo, role, speciesKey, adultDestination, capWarning);
    }

    private static string BuildWarnings(
        PortalRole role,
        string speciesKey,
        AdultDestination adultDestination,
        bool capWarning)
    {
        string warning = string.Empty;
        if (capWarning)
        {
            warning += "\n<color=yellow>Warning: maturing pen is within breeding cap radius of a breeder portal.</color>";
        }

        if (role == PortalRole.Maturing && adultDestination != AdultDestination.None)
        {
            bool missingReceiver = adultDestination == AdultDestination.Cull
                ? !DestinationRegistry.HasCullReceiver()
                : !DestinationRegistry.HasFarmReceiver(speciesKey);

            if (missingReceiver)
            {
                string destinationName = PortalRoleCatalog.GetDisplayName(adultDestination);
                warning += $"\n<color=yellow>Warning: No {destinationName} portal exists. Adults will not be teleported.</color>";
            }
        }

        if (role == PortalRole.Breeder && BreedableSpeciesRegistry.HasDualPurposeEggLayers()
            && !DestinationRegistry.HasEggCollector())
        {
            warning += "\n<color=yellow>Warning: Dual-purpose egg layers detected but no Egg Collector portal exists.</color>";
        }

        return warning;
    }
}
