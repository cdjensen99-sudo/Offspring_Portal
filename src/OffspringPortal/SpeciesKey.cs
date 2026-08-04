using System;

namespace OffspringPortal;

public static class SpeciesKey
{
    public const string All = "All";
    public const string EggPrefix = "Egg:";

    public static string Normalize(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return string.Empty;
        }

        string name = prefabName.Trim();
        int cloneIndex = name.IndexOf('(');
        if (cloneIndex > 0)
        {
            name = name.Substring(0, cloneIndex).Trim();
        }

        return name;
    }

    public static bool Matches(string creatureKey, string destinationKey)
    {
        if (string.IsNullOrWhiteSpace(destinationKey)
            || string.IsNullOrWhiteSpace(creatureKey))
        {
            return false;
        }

        if (destinationKey.Equals(All, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Canonicalize(creatureKey).Equals(Canonicalize(destinationKey), StringComparison.OrdinalIgnoreCase);
    }

    public static bool PortalAcceptsSpecies(string portalSpeciesKey, string creatureSpeciesKey)
    {
        if (string.IsNullOrWhiteSpace(portalSpeciesKey) || string.IsNullOrWhiteSpace(creatureSpeciesKey))
        {
            return false;
        }

        if (portalSpeciesKey.Equals(All, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Canonicalize(portalSpeciesKey).Equals(Canonicalize(creatureSpeciesKey), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAll(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
            && key.Equals(All, StringComparison.OrdinalIgnoreCase);
    }

    public static string Canonicalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        string normalized = Normalize(key);
        if (normalized.IndexOf("Hen", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SpeciesCatalog.ToStorageValue(SpeciesType.Chicken);
        }

        SpeciesType mapped = SpeciesCatalog.FromStorageValue(normalized);
        if (mapped != SpeciesType.None && mapped != SpeciesType.All)
        {
            return SpeciesCatalog.ToStorageValue(mapped);
        }

        foreach (string fragment in SpeciesCatalog.KnownAdultPrefabFragments)
        {
            if (normalized.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (fragment.Equals("Chicken", StringComparison.OrdinalIgnoreCase)
                    && normalized.IndexOf("Hen", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SpeciesCatalog.ToStorageValue(SpeciesType.Chicken);
                }

                SpeciesType fragmentSpecies = SpeciesCatalog.FromStorageValue(fragment);
                if (fragmentSpecies != SpeciesType.None)
                {
                    return SpeciesCatalog.ToStorageValue(fragmentSpecies);
                }
            }
        }

        return normalized;
    }

    public static string GetDisplayName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Unknown";
        }

        if (IsEggCollectorKey(key))
        {
            return GetEggCollectorDisplayName(key);
        }

        if (key.Equals(All, StringComparison.OrdinalIgnoreCase))
        {
            return "All";
        }

        SpeciesType mapped = SpeciesCatalog.FromStorageValue(key);
        if (mapped != SpeciesType.None)
        {
            return SpeciesCatalog.GetDisplayName(mapped);
        }

        return key;
    }

    public static bool IsEggCollectorKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
            && key.StartsWith(EggPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildEggCollectorKey(string speciesKey)
    {
        if (IsEggCollectorKey(speciesKey))
        {
            return EggPrefix + Canonicalize(speciesKey.Substring(EggPrefix.Length));
        }

        string canonical = Canonicalize(speciesKey);
        return string.IsNullOrEmpty(canonical) ? string.Empty : EggPrefix + canonical;
    }

    public static string GetEggCollectorDisplayName(string eggCollectorKey)
    {
        if (!IsEggCollectorKey(eggCollectorKey))
        {
            return GetDisplayName(eggCollectorKey);
        }

        string speciesPart = eggCollectorKey.Substring(EggPrefix.Length);
        return $"Egg {GetDisplayName(speciesPart)}";
    }

    public static bool PortalAcceptsEgg(string portalEggKey, string creatureEggKey)
    {
        if (string.IsNullOrWhiteSpace(portalEggKey) || string.IsNullOrWhiteSpace(creatureEggKey))
        {
            return false;
        }

        return BuildEggCollectorKey(portalEggKey).Equals(
            BuildEggCollectorKey(creatureEggKey),
            StringComparison.OrdinalIgnoreCase)
            || portalEggKey.Equals(creatureEggKey, StringComparison.OrdinalIgnoreCase);
    }

    public static SpeciesType ToLegacySpecies(string key)
    {
        return SpeciesCatalog.FromStorageValue(key);
    }
}
