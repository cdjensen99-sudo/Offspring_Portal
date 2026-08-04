namespace OffspringPortal;

public enum PortalRole
{
    Breeder,
    Maturing,
    Farm,
    Cull,
    EggCollector
}

public enum AdultDestination
{
    None,
    Farm,
    Cull
}

public static class PortalRoleCatalog
{
    public static PortalRole FromZdo(ZDO zdo)
    {
        if (zdo == null)
        {
            return PortalRole.Breeder;
        }

        string storedRole = zdo.GetString(ZdoFields.PortalRole, string.Empty);
        if (!string.IsNullOrWhiteSpace(storedRole)
            && System.Enum.TryParse(storedRole, ignoreCase: true, out PortalRole parsed))
        {
            return parsed;
        }

        SpeciesType species = SpeciesCatalog.FromStorageValue(zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty));
        return species == SpeciesType.None ? PortalRole.Breeder : PortalRole.Maturing;
    }

    public static AdultDestination GetAdultDestination(ZDO zdo)
    {
        if (zdo == null)
        {
            return AdultDestination.None;
        }

        string stored = zdo.GetString(ZdoFields.AdultDestination, string.Empty);
        if (!string.IsNullOrWhiteSpace(stored)
            && System.Enum.TryParse(stored, ignoreCase: true, out AdultDestination parsed))
        {
            return parsed;
        }

        return zdo.GetBool(ZdoFields.ForwardAdults) ? AdultDestination.Cull : AdultDestination.None;
    }

    public static string ToStorageValue(PortalRole role)
    {
        return role.ToString();
    }

    public static string ToStorageValue(AdultDestination destination)
    {
        return destination.ToString();
    }

    public static string GetDisplayName(PortalRole role)
    {
        switch (role)
        {
            case PortalRole.Maturing:
                return "Maturing";
            case PortalRole.Farm:
                return "Farm";
            case PortalRole.Cull:
                return "Cull";
            case PortalRole.EggCollector:
                return "Egg Collector";
            default:
                return "Breeder";
        }
    }

    public static string GetDisplayName(AdultDestination destination)
    {
        return destination.ToString();
    }

    public static string ResolveSpeciesKey(PortalRole role, string selectedSpeciesKey)
    {
        return role == PortalRole.Breeder || role == PortalRole.Cull
            ? string.Empty
            : selectedSpeciesKey ?? string.Empty;
    }

    public static SpeciesType ResolveSpecies(PortalRole role, SpeciesType selectedSpecies)
    {
        return role == PortalRole.Breeder || role == PortalRole.Cull
            ? SpeciesType.None
            : selectedSpecies;
    }

    public static AdultDestination ResolveAdultDestination(PortalRole role, AdultDestination selectedDestination)
    {
        return role == PortalRole.Maturing ? selectedDestination : AdultDestination.None;
    }

    public static string GetConfiguredMessage(PortalRole role, string speciesKey, AdultDestination adultDestination)
    {
        switch (role)
        {
            case PortalRole.Maturing:
                string receives = $"Maturing (receives {SpeciesKey.GetDisplayName(speciesKey)} juveniles)";
                return adultDestination == AdultDestination.None
                    ? receives
                    : $"{receives}, destination {GetDisplayName(adultDestination)}";
            case PortalRole.Farm:
                return $"Farm (receives {SpeciesKey.GetDisplayName(speciesKey)} adults)";
            case PortalRole.Cull:
                return "Cull yard";
            case PortalRole.EggCollector:
                return $"Egg Collector (receives {SpeciesKey.GetDisplayName(speciesKey)})";
            default:
                return "Breeder";
        }
    }

    public static string GetConfiguredMessage(PortalRole role, SpeciesType species, AdultDestination adultDestination)
    {
        string speciesKey = species == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(species);
        return GetConfiguredMessage(role, speciesKey, adultDestination);
    }
}
