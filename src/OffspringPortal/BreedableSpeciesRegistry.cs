using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OffspringPortal;

public static class BreedableSpeciesRegistry
{
    private static readonly Dictionary<string, BreedableSpeciesInfo> RegisteredSpecies =
        new Dictionary<string, BreedableSpeciesInfo>(System.StringComparer.OrdinalIgnoreCase);

    public static void Clear()
    {
        RegisteredSpecies.Clear();
    }

    public static void Register(BreedableSpeciesInfo info)
    {
        if (info == null || string.IsNullOrWhiteSpace(info.Key))
        {
            return;
        }

        info.Key = SpeciesKey.Canonicalize(info.Key);
        if (string.IsNullOrEmpty(info.DisplayName))
        {
            info.DisplayName = SpeciesKey.GetDisplayName(info.Key);
        }

        if (info.IsDualPurposeEgg)
        {
            info.EggCollectorKey = SpeciesKey.BuildEggCollectorKey(info.Key);
        }

        RegisteredSpecies[info.Key] = info;
    }

    public static bool TryGet(string key, out BreedableSpeciesInfo info)
    {
        if (RegisteredSpecies.TryGetValue(key, out info))
        {
            return true;
        }

        if (SpeciesKey.IsEggCollectorKey(key))
        {
            string speciesPart = key.Substring(SpeciesKey.EggPrefix.Length);
            return RegisteredSpecies.TryGetValue(speciesPart, out info);
        }

        return false;
    }

    public static bool HasDualPurposeEggLayers()
    {
        return RegisteredSpecies.Values.Any(species => species.IsDualPurposeEgg);
    }

    public static IReadOnlyList<string> GetMaturingOptionKeys()
    {
        List<string> keys = RegisteredSpecies.Values
            .Select(species => species.Key)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => SpeciesKey.GetDisplayName(key), System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        keys.Add(SpeciesKey.All);
        return keys;
    }

    public static IReadOnlyList<string> GetEggCollectorOptionKeys()
    {
        return RegisteredSpecies.Values
            .Where(species => species.IsDualPurposeEgg && !string.IsNullOrEmpty(species.EggCollectorKey))
            .Select(species => species.EggCollectorKey)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => SpeciesKey.GetEggCollectorDisplayName(key), System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> GetMaturingOptionKeysWithFallback()
    {
        if (RegisteredSpecies.Count == 0)
        {
            return SpeciesCatalog.LegacyDestinationOptionKeys;
        }

        return GetMaturingOptionKeys();
    }

    public static IReadOnlyList<string> GetDestinationOptionKeysWithFallback()
    {
        return GetMaturingOptionKeysWithFallback();
    }

    public static void DiscoverInRange(Vector3 center, float range)
    {
        float rangeSquared = range * range;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsPlayer())
            {
                continue;
            }

            if ((character.transform.position - center).sqrMagnitude > rangeSquared)
            {
                continue;
            }

            if (SpeciesDiscovery.TryDiscoverFromCharacter(character, out BreedableSpeciesInfo info)
                || SpeciesDiscovery.TryDiscoverFallback(character, out info))
            {
                Register(info);
            }
        }
    }

    public static void RefreshFromAllBreeders()
    {
        RegisteredSpecies.Clear();
        float range = ModConfig.GetDiscoveryScanRange();
        foreach (PortalRecord breeder in DestinationRegistry.GetBreederPortals())
        {
            DiscoverInRange(breeder.Position, range);
        }
    }
}
