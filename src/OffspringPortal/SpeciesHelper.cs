using UnityEngine;

namespace OffspringPortal;

public static class SpeciesHelper
{
    private static readonly string[] JuvenilePrefabNameFallbacks =
    {
        "wolfcub",
        "moose_calf",
        "moosecalf"
    };

    public static bool IsEligibleJuvenile(Character character)
    {
        if (character == null || !character.IsTamed())
        {
            return false;
        }

        if (character.GetComponent<Growup>() != null)
        {
            return true;
        }

        return HasJuvenilePrefabNameFallback(character.name);
    }

    private static bool HasJuvenilePrefabNameFallback(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return false;
        }

        foreach (string fragment in JuvenilePrefabNameFallbacks)
        {
            if (prefabName.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsEligibleAdult(Character character)
    {
        return character != null
            && character.IsTamed()
            && character.GetComponent<Growup>() == null
            && !string.IsNullOrEmpty(GetAdultSpeciesKey(character));
    }

    public static SpeciesType GetAdultSpecies(Character character)
    {
        return SpeciesKey.ToLegacySpecies(GetAdultSpeciesKey(character));
    }

    public static SpeciesType GetJuvenileSpecies(Character character)
    {
        return SpeciesKey.ToLegacySpecies(GetJuvenileSpeciesKey(character));
    }

    public static string GetAdultSpeciesKey(Character character)
    {
        return SpeciesDiscovery.GetAdultSpeciesKey(character);
    }

    public static string GetJuvenileSpeciesKey(Character character)
    {
        Growup growup = character?.GetComponent<Growup>();
        if (growup != null)
        {
            string mapped = SpeciesDiscovery.GetSpeciesKey(character);
            if (!string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }
        }

        string prefabName = character?.name ?? string.Empty;
        if (prefabName.IndexOf("wolfcub", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SpeciesCatalog.ToStorageValue(SpeciesType.Wolf);
        }

        if (prefabName.IndexOf("moose_calf", System.StringComparison.OrdinalIgnoreCase) >= 0
            || prefabName.IndexOf("moosecalf", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SpeciesCatalog.ToStorageValue(SpeciesType.Moose);
        }

        return string.Empty;
    }

    public static bool SpeciesMatches(SpeciesType juvenile, SpeciesType destination)
    {
        return SpeciesKey.Matches(
            SpeciesCatalog.ToStorageValue(juvenile),
            SpeciesCatalog.ToStorageValue(destination));
    }

    public static bool SpeciesKeyMatches(string creatureKey, string destinationKey)
    {
        return SpeciesKey.Matches(creatureKey, destinationKey);
    }
}
