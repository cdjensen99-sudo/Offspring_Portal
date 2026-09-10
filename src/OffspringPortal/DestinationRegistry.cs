using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OffspringPortal;

public sealed class PortalRecord
{
    public ZDOID Id;
    public Vector3 Position;
    public PortalRole Role;
    public SpeciesType DeclaredSpecies;
    public string DeclaredSpeciesKey;
    public AdultDestination AdultDestination;
}

public static class DestinationRegistry
{
    private static readonly Dictionary<ZDOID, PortalRecord> Portals = new Dictionary<ZDOID, PortalRecord>();
    private static readonly Dictionary<string, int> MaturingRoundRobinIndex = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> FarmRoundRobinIndex = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    private static int cullRoundRobinIndex;
    private static int eggCollectorRoundRobinIndex;

    public static void Clear()
    {
        Portals.Clear();
    }

    public static void RegisterOrUpdate(
        ZDOID id,
        Vector3 position,
        PortalRole role,
        SpeciesType declaredSpecies,
        AdultDestination adultDestination)
    {
        string speciesKey = declaredSpecies == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(declaredSpecies);
        RegisterOrUpdate(id, position, role, speciesKey, adultDestination);
    }

    public static void RegisterOrUpdate(
        ZDOID id,
        Vector3 position,
        PortalRole role,
        string declaredSpeciesKey,
        AdultDestination adultDestination)
    {
        if (!Portals.TryGetValue(id, out PortalRecord record))
        {
            record = new PortalRecord { Id = id };
            Portals[id] = record;
        }

        record.Position = position;
        record.Role = role;
        record.DeclaredSpeciesKey = declaredSpeciesKey ?? string.Empty;
        record.DeclaredSpecies = SpeciesCatalog.FromStorageValue(record.DeclaredSpeciesKey);
        record.AdultDestination = adultDestination;
    }

    public static void Remove(ZDOID id)
    {
        Portals.Remove(id);
    }

    public static PortalRecord Get(ZDOID id)
    {
        Portals.TryGetValue(id, out PortalRecord record);
        return record;
    }

    public static void RefreshPortalPosition(PortalRecord record)
    {
        if (record == null || ZDOMan.instance == null)
        {
            return;
        }

        ZDO zdo = ZDOMan.instance.GetZDO(record.Id);
        if (zdo != null)
        {
            record.Position = zdo.GetPosition();
        }
    }

    public static bool TryResolveMaturingDestination(SpeciesType juvenileSpecies, out PortalRecord destination)
    {
        string speciesKey = juvenileSpecies == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(juvenileSpecies);
        return TryResolveMaturingDestination(speciesKey, out destination);
    }

    public static bool TryResolveMaturingDestination(string speciesKey, out PortalRecord destination)
    {
        return TryResolveDestination(
            speciesKey,
            PortalRole.Maturing,
            MaturingRoundRobinIndex,
            out destination);
    }

    public static bool TryResolveFarmDestination(SpeciesType adultSpecies, out PortalRecord destination)
    {
        string speciesKey = adultSpecies == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(adultSpecies);
        return TryResolveFarmDestination(speciesKey, out destination);
    }

    public static bool TryResolveFarmDestination(string speciesKey, out PortalRecord destination)
    {
        return TryResolveDestination(
            speciesKey,
            PortalRole.Farm,
            FarmRoundRobinIndex,
            out destination);
    }

    public static bool TryResolveCullDestination(out PortalRecord destination)
    {
        destination = null;
        List<PortalRecord> cullPortals = Portals.Values
            .Where(p => p.Role == PortalRole.Cull)
            .OrderBy(p => p.Id.ToString())
            .ToList();

        if (cullPortals.Count == 0)
        {
            return false;
        }

        destination = cullPortals[cullRoundRobinIndex % cullPortals.Count];
        cullRoundRobinIndex++;
        RefreshPortalPosition(destination);
        return true;
    }

    public static bool TryResolveEggCollectorDestination(string eggCollectorKey, out PortalRecord destination)
    {
        destination = null;
        if (string.IsNullOrWhiteSpace(eggCollectorKey))
        {
            return false;
        }

        string speciesKey = SpeciesKey.IsEggCollectorKey(eggCollectorKey)
            ? eggCollectorKey.Substring(SpeciesKey.EggPrefix.Length)
            : eggCollectorKey;

        List<PortalRecord> matches = Portals.Values
            .Where(p => p.Role == PortalRole.EggCollector
                        && (SpeciesKey.PortalAcceptsEgg(p.DeclaredSpeciesKey, eggCollectorKey)
                            || SpeciesKey.PortalAcceptsSpecies(p.DeclaredSpeciesKey, speciesKey)))
            .OrderBy(p => p.Id.ToString())
            .ToList();

        if (matches.Count == 0)
        {
            return false;
        }

        destination = matches[eggCollectorRoundRobinIndex % matches.Count];
        eggCollectorRoundRobinIndex++;
        RefreshPortalPosition(destination);
        return true;
    }

    public static bool HasCullReceiver()
    {
        return Portals.Values.Any(p => p.Role == PortalRole.Cull);
    }

    public static bool HasEggCollector()
    {
        return Portals.Values.Any(p => p.Role == PortalRole.EggCollector);
    }

    public static bool HasFarmReceiver(SpeciesType maturingReceives)
    {
        string speciesKey = maturingReceives == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(maturingReceives);
        return HasFarmReceiver(speciesKey);
    }

    public static bool HasFarmReceiver(string maturingReceivesKey)
    {
        if (!string.IsNullOrEmpty(maturingReceivesKey)
            && !maturingReceivesKey.Equals(SpeciesKey.All, System.StringComparison.OrdinalIgnoreCase)
            && TryResolveFarmDestination(maturingReceivesKey, out _))
        {
            return true;
        }

        if (TryResolveFarmDestination(SpeciesKey.All, out _))
        {
            return true;
        }

        return maturingReceivesKey != null
            && maturingReceivesKey.Equals(SpeciesKey.All, System.StringComparison.OrdinalIgnoreCase)
            && Portals.Values.Any(p => p.Role == PortalRole.Farm);
    }

    public static IEnumerable<PortalRecord> GetBreederPortals()
    {
        return Portals.Values.Where(p => p.Role == PortalRole.Breeder);
    }

    public static IEnumerable<PortalRecord> GetAll()
    {
        return Portals.Values;
    }

    public static void RefreshCapWarnings()
    {
        if (!ModConfig.EnableCapWarning.Value || ZDOMan.instance == null)
        {
            return;
        }

        foreach (PortalRecord destination in Portals.Values.Where(
                     p => p.Role == PortalRole.Maturing
                          && !SpeciesKey.All.Equals(p.DeclaredSpeciesKey, System.StringComparison.OrdinalIgnoreCase)))
        {
            SpeciesType species = destination.DeclaredSpecies;
            if (species == SpeciesType.None)
            {
                continue;
            }

            bool tooClose = GetBreederPortals().Any(source =>
                Vector3.Distance(source.Position, destination.Position)
                <= BreedingCapData.GetCapRadius(species));

            ZDO zdo = ZDOMan.instance.GetZDO(destination.Id);
            if (zdo != null)
            {
                zdo.Set(ZdoFields.CapWarning, tooClose);
            }
        }
    }

    private static bool TryResolveDestination(
        string speciesKey,
        PortalRole requiredRole,
        Dictionary<string, int> roundRobinIndex,
        out PortalRecord destination)
    {
        destination = null;
        if (string.IsNullOrWhiteSpace(speciesKey))
        {
            return false;
        }

        List<PortalRecord> specific = Portals.Values
            .Where(p => p.Role == requiredRole
                        && !SpeciesKey.IsAll(p.DeclaredSpeciesKey)
                        && SpeciesKey.PortalAcceptsSpecies(p.DeclaredSpeciesKey, speciesKey))
            .OrderBy(p => p.Id.ToString())
            .ToList();

        if (specific.Count > 0)
        {
            destination = PickRoundRobin(speciesKey, specific, roundRobinIndex);
            RefreshPortalPosition(destination);
            return destination != null;
        }

        List<PortalRecord> catchAll = Portals.Values
            .Where(p => p.Role == requiredRole
                        && SpeciesKey.All.Equals(p.DeclaredSpeciesKey, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id.ToString())
            .ToList();

        if (catchAll.Count > 0)
        {
            destination = PickRoundRobin(SpeciesKey.All, catchAll, roundRobinIndex);
            RefreshPortalPosition(destination);
            return destination != null;
        }

        return false;
    }

    private static PortalRecord PickRoundRobin(
        string key,
        List<PortalRecord> candidates,
        Dictionary<string, int> roundRobinIndex)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (!roundRobinIndex.TryGetValue(key, out int index))
        {
            index = 0;
        }

        PortalRecord picked = candidates[index % candidates.Count];
        roundRobinIndex[key] = index + 1;
        return picked;
    }
}
