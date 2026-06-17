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
    public AdultDestination AdultDestination;
}

public static class DestinationRegistry
{
    private static readonly Dictionary<ZDOID, PortalRecord> Portals = new Dictionary<ZDOID, PortalRecord>();
    private static readonly Dictionary<SpeciesType, int> MaturingRoundRobinIndex = new Dictionary<SpeciesType, int>();
    private static readonly Dictionary<SpeciesType, int> FarmRoundRobinIndex = new Dictionary<SpeciesType, int>();
    private static int cullRoundRobinIndex;

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
        if (!Portals.TryGetValue(id, out PortalRecord record))
        {
            record = new PortalRecord { Id = id };
            Portals[id] = record;
        }

        record.Position = position;
        record.Role = role;
        record.DeclaredSpecies = declaredSpecies;
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
        if (record == null)
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
        return TryResolveDestination(
            juvenileSpecies,
            PortalRole.Maturing,
            MaturingRoundRobinIndex,
            out destination);
    }

    public static bool TryResolveFarmDestination(SpeciesType adultSpecies, out PortalRecord destination)
    {
        return TryResolveDestination(
            adultSpecies,
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

    public static bool HasCullReceiver()
    {
        return Portals.Values.Any(p => p.Role == PortalRole.Cull);
    }

    public static bool HasFarmReceiver(SpeciesType maturingReceives)
    {
        if (maturingReceives != SpeciesType.None
            && maturingReceives != SpeciesType.All
            && TryResolveFarmDestination(maturingReceives, out _))
        {
            return true;
        }

        if (TryResolveFarmDestination(SpeciesType.All, out _))
        {
            return true;
        }

        return maturingReceives == SpeciesType.All
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
        if (!ModConfig.EnableCapWarning.Value)
        {
            return;
        }

        foreach (PortalRecord destination in Portals.Values.Where(
                     p => p.Role == PortalRole.Maturing && p.DeclaredSpecies != SpeciesType.All))
        {
            bool tooClose = GetBreederPortals().Any(source =>
                Vector3.Distance(source.Position, destination.Position)
                <= BreedingCapData.GetCapRadius(destination.DeclaredSpecies));

            ZDO zdo = ZDOMan.instance.GetZDO(destination.Id);
            if (zdo != null)
            {
                zdo.Set(ZdoFields.CapWarning, tooClose);
            }
        }
    }

    private static bool TryResolveDestination(
        SpeciesType species,
        PortalRole requiredRole,
        Dictionary<SpeciesType, int> roundRobinIndex,
        out PortalRecord destination)
    {
        destination = null;
        if (species == SpeciesType.None)
        {
            return false;
        }

        List<PortalRecord> specific = Portals.Values
            .Where(p => p.Role == requiredRole && p.DeclaredSpecies == species)
            .OrderBy(p => p.Id.ToString())
            .ToList();

        if (specific.Count > 0)
        {
            destination = PickRoundRobin(species, specific, roundRobinIndex);
            RefreshPortalPosition(destination);
            return destination != null;
        }

        List<PortalRecord> catchAll = Portals.Values
            .Where(p => p.Role == requiredRole && p.DeclaredSpecies == SpeciesType.All)
            .OrderBy(p => p.Id.ToString())
            .ToList();

        if (catchAll.Count > 0)
        {
            destination = PickRoundRobin(SpeciesType.All, catchAll, roundRobinIndex);
            RefreshPortalPosition(destination);
            return destination != null;
        }

        return false;
    }

    private static PortalRecord PickRoundRobin(
        SpeciesType key,
        List<PortalRecord> candidates,
        Dictionary<SpeciesType, int> roundRobinIndex)
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
