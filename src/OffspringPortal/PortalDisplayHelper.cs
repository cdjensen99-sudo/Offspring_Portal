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

    public static string GetDetailLine(PortalRole role, SpeciesType species, AdultDestination adultDestination)
    {
        switch (role)
        {
            case PortalRole.Maturing:
                string line = $"Receives: {SpeciesCatalog.GetDisplayName(species)} juveniles";
                if (adultDestination != AdultDestination.None)
                {
                    line += $"\nDestination: {PortalRoleCatalog.GetDisplayName(adultDestination)}";
                }

                return line;
            case PortalRole.Farm:
                return $"Receives: {SpeciesCatalog.GetDisplayName(species)} adults";
            case PortalRole.Cull:
                return "Role: Cull yard";
            default:
                return "Role: Breeder";
        }
    }

    public static string GetHoverText(
        ZDO zdo,
        PortalRole role,
        SpeciesType species,
        AdultDestination adultDestination,
        bool capWarning)
    {
        string name = GetDisplayName(zdo);
        string detail = GetDetailLine(role, species, adultDestination);
        string warning = BuildWarnings(role, species, adultDestination, capWarning);

        return Localization.instance.Localize(
            $"Name: {name}\n{detail}{warning}\n[<color=yellow><b>$KEY_Use</b></color>] Configure");
    }

    private static string BuildWarnings(
        PortalRole role,
        SpeciesType species,
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
                : !DestinationRegistry.HasFarmReceiver(species);

            if (missingReceiver)
            {
                string destinationName = PortalRoleCatalog.GetDisplayName(adultDestination);
                warning += $"\n<color=yellow>Warning: No {destinationName} portal exists. Adults will not be teleported.</color>";
            }
        }

        return warning;
    }
}
