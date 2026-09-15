using System.Collections.Generic;

using System.Linq;

using System.Text;



namespace OffspringPortal;



public static class PortalConnectionStatus

{

    public static bool IsRoutingReady(PortalRole role, string speciesKey, AdultDestination adultDestination)

    {

        switch (role)

        {

            case PortalRole.Breeder:

                return HasMaturingDestinationForBreeder();

            case PortalRole.Maturing:

                if (adultDestination == AdultDestination.Farm && !DestinationRegistry.HasFarmReceiver(speciesKey))

                {

                    return false;

                }



                if (adultDestination == AdultDestination.Cull && !DestinationRegistry.HasCullReceiver())

                {

                    return false;

                }



                return HasBreederPortal();

            case PortalRole.Farm:

                return HasMaturingForwardingToFarm(speciesKey);

            case PortalRole.Cull:

                return HasMaturingForwardingToCull();

            case PortalRole.EggCollector:

                return HasBreederPortal();

            default:

                return false;

        }

    }



    public static string GetStatusSummary(PortalRole role, string speciesKey, AdultDestination adultDestination)
    {
        return GetStatusSummary(role, speciesKey, adultDestination, ZDOID.None);
    }

    public static string GetStatusSummary(
        PortalRole role,
        string speciesKey,
        AdultDestination adultDestination,
        ZDOID portalId)
    {
        EnsureRegistryForStatus(portalId);

        if (IsRoutingReady(role, speciesKey, adultDestination))
        {
            return GetConnectedSummary(role, speciesKey, adultDestination, portalId);
        }

        return GetUnconnectedSummary(role, speciesKey, adultDestination);
    }



    public static string GetHoverConnectionLine(PortalRole role, string speciesKey, AdultDestination adultDestination)
    {
        return GetHoverConnectionLine(role, speciesKey, adultDestination, ZDOID.None);
    }

    public static string GetHoverConnectionLine(
        PortalRole role,
        string speciesKey,
        AdultDestination adultDestination,
        ZDOID portalId)
    {
        EnsureRegistryForStatus(portalId);

        bool ready = IsRoutingReady(role, speciesKey, adultDestination);
        string status = ready ? "[Connected]" : "[Unconnected]";
        string route = ready
            ? GetConnectedSummary(role, speciesKey, adultDestination, portalId)
            : GetUnconnectedSummary(role, speciesKey, adultDestination);
        return $"{status}\n{route}";
    }



    private static string GetConnectedSummary(
        PortalRole role,
        string speciesKey,
        AdultDestination adultDestination,
        ZDOID portalId = default)
    {
        List<string> lines = BuildConnectedRouteLines(role, speciesKey, adultDestination, portalId);

        if (lines.Count == 0)

        {

            return "Connected";

        }



        return string.Join("\n", lines);

    }



    private static void EnsureRegistryForStatus(ZDOID portalId)
    {
        PortalHelper.SyncRegistryFromAllPortalZdos();
        if (portalId == ZDOID.None || ZDOMan.instance == null)
        {
            return;
        }

        ZDO zdo = ZDOMan.instance.GetZDO(portalId);
        if (zdo != null)
        {
            PortalHelper.SyncRegistryFromZdo(zdo);
        }
    }

    private static List<string> BuildConnectedRouteLines(
        PortalRole role,
        string speciesKey,
        AdultDestination adultDestination,
        ZDOID portalId = default)
    {
        List<string> lines = new List<string>();

        switch (role)
        {
            case PortalRole.Breeder:
                TryAddRouteLine(lines, "Routes to", GetMaturingPortalNames(excludePortalId: portalId));
                break;

            case PortalRole.Maturing:
                TryAddRouteLine(lines, "Receives from", GetBreederPortalNames(excludePortalId: portalId));

                if (adultDestination == AdultDestination.Farm)

                {

                    TryAddRouteLine(lines, "Routes to", GetFarmPortalNames(speciesKey));

                }

                else if (adultDestination == AdultDestination.Cull)

                {

                    TryAddRouteLine(lines, "Routes to", GetCullPortalNames());

                }



                break;



            case PortalRole.Farm:

                TryAddRouteLine(

                    lines,

                    "Receives from",

                    GetMaturingPortalNames(speciesKey, AdultDestination.Farm));

                break;



            case PortalRole.Cull:

                TryAddRouteLine(

                    lines,

                    "Receives from",

                    GetMaturingPortalNames(null, AdultDestination.Cull));

                break;



            case PortalRole.EggCollector:
                TryAddRouteLine(lines, "Receives from", GetBreederPortalNames(excludePortalId: portalId));
                break;

        }



        return lines;

    }



    private static void TryAddRouteLine(List<string> lines, string prefix, IEnumerable<string> portalNames)

    {

        string formatted = FormatPortalNames(portalNames);

        if (string.IsNullOrEmpty(formatted))

        {

            return;

        }



        lines.Add($"{prefix}: {formatted}");

    }



    private static string GetUnconnectedSummary(PortalRole role, string speciesKey, AdultDestination adultDestination)

    {

        switch (role)

        {

            case PortalRole.Breeder:

                return "No Maturing portal registered.";

            case PortalRole.Maturing:

                if (adultDestination == AdultDestination.Farm)

                {

                    return $"No Farm portal for {SpeciesKey.GetDisplayName(speciesKey)}.";

                }



                if (adultDestination == AdultDestination.Cull)

                {

                    return "No Cull portal registered.";

                }



                return "No Breeder portal registered.";

            case PortalRole.Farm:

                return $"No Maturing portal forwarding {SpeciesKey.GetDisplayName(speciesKey)} adults.";

            case PortalRole.Cull:

                return "No Maturing portal forwarding adults to Cull.";

            case PortalRole.EggCollector:

                return "No Breeder portal registered.";

            default:

                return "Routing incomplete.";

        }

    }



    private static bool HasMaturingDestinationForBreeder()

    {

        foreach (PortalRecord record in DestinationRegistry.GetAll())

        {

            if (record.Role == PortalRole.Maturing)

            {

                return true;

            }

        }



        return false;

    }



    private static bool HasBreederPortal()

    {

        return DestinationRegistry.GetBreederPortals().Any();

    }



    private static bool HasMaturingForwardingToFarm(string speciesKey)

    {

        return DestinationRegistry.GetAll().Any(record =>

            record.Role == PortalRole.Maturing

            && record.AdultDestination == AdultDestination.Farm

            && SpeciesKey.PortalAcceptsSpecies(record.DeclaredSpeciesKey, speciesKey));

    }



    private static bool HasMaturingForwardingToCull()

    {

        return DestinationRegistry.GetAll().Any(record =>

            record.Role == PortalRole.Maturing && record.AdultDestination == AdultDestination.Cull);

    }



    private static IEnumerable<string> GetMaturingPortalNames(
        string speciesKey = null,
        AdultDestination? destination = null,
        ZDOID excludePortalId = default)
    {
        foreach (PortalRecord record in DestinationRegistry.GetAll())
        {
            if (record.Role != PortalRole.Maturing)
            {
                continue;
            }

            if (excludePortalId != ZDOID.None && record.Id == excludePortalId)
            {
                continue;
            }



            if (destination.HasValue && record.AdultDestination != destination.Value)

            {

                continue;

            }



            if (!string.IsNullOrEmpty(speciesKey)

                && !SpeciesKey.PortalAcceptsSpecies(record.DeclaredSpeciesKey, speciesKey))

            {

                continue;

            }



            yield return GetPortalName(record.Id);

        }

    }



    private static IEnumerable<string> GetBreederPortalNames(ZDOID excludePortalId = default)
    {
        foreach (PortalRecord record in DestinationRegistry.GetBreederPortals())
        {
            if (excludePortalId != ZDOID.None && record.Id == excludePortalId)
            {
                continue;
            }

            yield return GetPortalName(record.Id);
        }
    }



    private static IEnumerable<string> GetFarmPortalNames(string speciesKey)

    {

        foreach (PortalRecord record in DestinationRegistry.GetAll())

        {

            if (record.Role != PortalRole.Farm)

            {

                continue;

            }



            if (!SpeciesKey.PortalAcceptsSpecies(record.DeclaredSpeciesKey, speciesKey))

            {

                continue;

            }



            yield return GetPortalName(record.Id);

        }

    }



    private static IEnumerable<string> GetCullPortalNames()

    {

        foreach (PortalRecord record in DestinationRegistry.GetAll())

        {

            if (record.Role == PortalRole.Cull)

            {

                yield return GetPortalName(record.Id);

            }

        }

    }



    private static string GetPortalName(ZDOID portalId)

    {

        if (ZDOMan.instance == null)

        {

            return string.Empty;

        }



        ZDO zdo = ZDOMan.instance.GetZDO(portalId);

        string name = PortalDisplayHelper.GetDisplayName(zdo);

        return name == PortalDisplayHelper.UnnamedDisplay ? string.Empty : name;

    }



    private static string FormatPortalNames(IEnumerable<string> names)

    {

        List<string> list = names

            .Where(name => !string.IsNullOrEmpty(name) && name != PortalDisplayHelper.UnnamedDisplay)

            .Distinct()

            .ToList();

        if (list.Count == 0)

        {

            return string.Empty;

        }



        if (list.Count <= 3)

        {

            return string.Join(", ", list);

        }



        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < 3; i++)

        {

            if (i > 0)

            {

                builder.Append(", ");

            }



            builder.Append(list[i]);

        }



        builder.Append($" (+{list.Count - 3} more)");

        return builder.ToString();

    }

}


