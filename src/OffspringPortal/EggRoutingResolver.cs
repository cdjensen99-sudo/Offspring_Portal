using System;

namespace OffspringPortal;

public static class EggRoutingResolver
{
    private static readonly string[] HenEggCollectorLookupKeys =
    {
        "Egg:Chicken",
        "Chicken",
        "Hen",
        "Egg:Hen"
    };

    public static bool IsHenEgg(BreedableSpeciesInfo speciesInfo)
    {
        if (speciesInfo == null)
        {
            return false;
        }

        string canonical = SpeciesKey.Canonicalize(speciesInfo.Key);
        return canonical.Equals(
            SpeciesCatalog.ToStorageValue(SpeciesType.Chicken),
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryResolveDestination(BreedableSpeciesInfo speciesInfo, out PortalRecord destination)
    {
        destination = null;
        if (speciesInfo == null)
        {
            return false;
        }

        if (speciesInfo.IsDualPurposeEgg && IsHenEgg(speciesInfo))
        {
            if (TryResolveHenEggCollector(out destination))
            {
                return true;
            }

            if (DestinationRegistry.HasEggCollector())
            {
                return false;
            }

            return DestinationRegistry.TryResolveMaturingDestination(speciesInfo.Key, out destination);
        }

        if (speciesInfo.IsDualPurposeEgg)
        {
            if (DestinationRegistry.TryResolveEggCollectorDestination(speciesInfo.EggCollectorKey, out destination)
                || DestinationRegistry.TryResolveMaturingDestination(speciesInfo.Key, out destination))
            {
                return destination != null;
            }

            return false;
        }

        return DestinationRegistry.TryResolveMaturingDestination(speciesInfo.Key, out destination);
    }

    public static string GetHenRoutingFailureMessage()
    {
        return "Hen egg not routed: an Egg Collector portal exists but none are configured for Hen eggs.";
    }

    private static bool TryResolveHenEggCollector(out PortalRecord destination)
    {
        foreach (string key in HenEggCollectorLookupKeys)
        {
            if (DestinationRegistry.TryResolveEggCollectorDestination(key, out destination))
            {
                return true;
            }
        }

        destination = null;
        return false;
    }
}
