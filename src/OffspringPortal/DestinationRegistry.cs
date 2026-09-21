using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OffspringPortal;

public sealed class PortalRecord
{
    public ZDOID Id;
    public Vector3 Position;
    public Quaternion Rotation = Quaternion.identity;
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
    private static readonly Dictionary<string, int> EggCollectorRoundRobinIndex =
        new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

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
        record.DeclaredSpeciesKey = NormalizeDeclaredSpeciesKey(role, declaredSpeciesKey);
        record.DeclaredSpecies = SpeciesCatalog.FromStorageValue(record.DeclaredSpeciesKey);
        record.AdultDestination = adultDestination;
    }

    public static void RegisterSnapshot(ZDOID id, Vector3 position, Quaternion rotation)
    {
        if (id == ZDOID.None)
        {
            return;
        }

        if (!Portals.TryGetValue(id, out PortalRecord record))
        {
            record = new PortalRecord { Id = id };
            Portals[id] = record;
        }

        record.Position = position;
        record.Rotation = rotation;
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
            record.Rotation = zdo.GetRotation();
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
        return TryResolveEggCollectorRoundRobin(
            new[] { eggCollectorKey ?? string.Empty },
            BuildEggCollectorRoundRobinKey(eggCollectorKey),
            out destination);
    }

    public static bool TryResolveEggCollectorRoundRobin(
        IReadOnlyList<string> eggCollectorKeys,
        string roundRobinKey,
        out PortalRecord destination)
    {
        destination = null;
        if (eggCollectorKeys == null || eggCollectorKeys.Count == 0)
        {
            return false;
        }

        List<PortalRecord> matches = CollectMatchingEggCollectors(eggCollectorKeys);
        if (matches.Count == 0)
        {
            return false;
        }

        string key = string.IsNullOrWhiteSpace(roundRobinKey)
            ? BuildEggCollectorRoundRobinKey(eggCollectorKeys[0])
            : roundRobinKey;
        destination = PickRoundRobin(key, matches, EggCollectorRoundRobinIndex);
        RefreshPortalPosition(destination);
        return destination != null;
    }

    internal static List<PortalRecord> CollectMatchingEggCollectors(IReadOnlyList<string> eggCollectorKeys)
    {
        List<PortalRecord> matches = new List<PortalRecord>();
        HashSet<string> seenPortalIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (eggCollectorKeys == null)
        {
            return matches;
        }

        foreach (string eggCollectorKey in eggCollectorKeys)
        {
            if (string.IsNullOrWhiteSpace(eggCollectorKey))
            {
                continue;
            }

            foreach (PortalRecord record in GetMatchingEggCollectors(eggCollectorKey))
            {
                string portalId = record.Id.ToString();
                if (seenPortalIds.Add(portalId))
                {
                    matches.Add(record);
                }
            }
        }

        matches.Sort((left, right) => string.Compare(left.Id.ToString(), right.Id.ToString(), System.StringComparison.Ordinal));
        return matches;
    }

    private static List<PortalRecord> GetMatchingEggCollectors(string eggCollectorKey)
    {
        string normalizedEggKey = SpeciesKey.BuildEggCollectorKey(eggCollectorKey);
        string speciesKey = SpeciesKey.IsEggCollectorKey(normalizedEggKey)
            ? normalizedEggKey.Substring(SpeciesKey.EggPrefix.Length)
            : SpeciesKey.Canonicalize(eggCollectorKey);

        return Portals.Values
            .Where(p => IsLivePortalRecord(p)
                        && p.Role == PortalRole.EggCollector
                        && PortalAcceptsEggCollector(p.DeclaredSpeciesKey, normalizedEggKey, speciesKey))
            .OrderBy(p => p.Id.ToString())
            .ToList();
    }

    private static string BuildEggCollectorRoundRobinKey(string eggCollectorKey)
    {
        string normalized = SpeciesKey.BuildEggCollectorKey(eggCollectorKey);
        return string.IsNullOrWhiteSpace(normalized)
            ? SpeciesKey.Canonicalize(eggCollectorKey)
            : normalized;
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

    public static string BuildSummaryCounts()
    {
        int breeders = 0;
        int maturing = 0;
        int farm = 0;
        int cull = 0;
        int eggCollectors = 0;
        foreach (PortalRecord record in Portals.Values)
        {
            switch (record.Role)
            {
                case PortalRole.Maturing:
                    maturing++;
                    break;
                case PortalRole.Farm:
                    farm++;
                    break;
                case PortalRole.Cull:
                    cull++;
                    break;
                case PortalRole.EggCollector:
                    eggCollectors++;
                    break;
                default:
                    breeders++;
                    break;
            }
        }

        return $"{breeders} breeder(s), {maturing} maturing, {farm} farm, {cull} cull, {eggCollectors} egg collector(s)";
    }

    public static string FormatPortalRecord(PortalRecord record)
    {
        if (record == null)
        {
            return "  (null portal record)";
        }

        string species = string.IsNullOrEmpty(record.DeclaredSpeciesKey)
            ? "(none)"
            : record.DeclaredSpeciesKey;
        string live = IsLivePortalRecord(record) ? "live" : "missing-zdo";
        return
            $"  {record.Role} receives={species} id={record.Id} pos=({record.Position.x:F1}, {record.Position.y:F1}, {record.Position.z:F1}) [{live}]";
    }

    public static void LogMaturingResolutionFailure(string speciesKey, string context)
    {
        DiagnosticLog.Warning(
            $"No maturing portal registered for '{speciesKey}' during {context}. Registry: {BuildSummaryCounts()}.");
        foreach (PortalRecord record in Portals.Values.Where(p => p.Role == PortalRole.Maturing))
        {
            DiagnosticLog.Warning(FormatPortalRecord(record));
        }

        if (!Portals.Values.Any(p => p.Role == PortalRole.Maturing))
        {
            DiagnosticLog.Warning(
                "No Maturing portals are registered on this game instance. Place/configure a Maturing portal and confirm the server log shows it after configuration.");
        }
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
            .Where(p => IsLivePortalRecord(p)
                        && p.Role == requiredRole
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
            .Where(p => IsLivePortalRecord(p)
                        && p.Role == requiredRole
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

    internal static bool IsLivePortalRecord(PortalRecord record)
    {
        if (record == null)
        {
            return false;
        }

        if (ZNet.instance != null && !ZNet.instance.IsServer())
        {
            return true;
        }

        if (ZDOMan.instance == null)
        {
            return true;
        }

        // A portal ZDO can be absent while its zone is unloaded. Keep the record
        // so distant maturing portals remain routable until actually destroyed.
        ZDO zdo = ZDOMan.instance.GetZDO(record.Id);
        if (zdo == null)
        {
            return true;
        }

        if (!zdo.IsValid())
        {
            return false;
        }

        bool isOffspringPrefab = zdo.GetPrefab() == PrefabNames.OffspringPortal.GetStableHashCode();
        bool hasPortalRole = !string.IsNullOrEmpty(zdo.GetString(ZdoFields.PortalRole, string.Empty));
        return isOffspringPrefab || hasPortalRole;
    }

    private static string NormalizeDeclaredSpeciesKey(PortalRole role, string declaredSpeciesKey)
    {
        if (role == PortalRole.EggCollector)
        {
            return SpeciesKey.BuildEggCollectorKey(declaredSpeciesKey);
        }

        return declaredSpeciesKey ?? string.Empty;
    }

    private static bool PortalAcceptsEggCollector(
        string portalSpeciesKey,
        string normalizedEggKey,
        string speciesKey)
    {
        if (string.IsNullOrWhiteSpace(portalSpeciesKey))
        {
            return false;
        }

        if (SpeciesKey.PortalAcceptsEgg(portalSpeciesKey, normalizedEggKey))
        {
            return true;
        }

        if (SpeciesKey.PortalAcceptsSpecies(portalSpeciesKey, speciesKey))
        {
            return true;
        }

        return SpeciesKey.Canonicalize(portalSpeciesKey).Equals(
            speciesKey,
            System.StringComparison.OrdinalIgnoreCase);
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
