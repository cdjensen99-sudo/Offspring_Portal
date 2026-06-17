using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class SpeciesHelper
{
    private static readonly Dictionary<string, SpeciesType> AdultPrefabMap = new Dictionary<string, SpeciesType>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Boar", SpeciesType.Boar },
        { "Wolf", SpeciesType.Wolf },
        { "WolfCub", SpeciesType.Wolf },
        { "Lox", SpeciesType.Lox },
        { "Hen", SpeciesType.Chicken },
        { "Chicken", SpeciesType.Chicken },
        { "Asksvin", SpeciesType.Asksvin }
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

        string prefabName = character.name ?? string.Empty;
        return prefabName.IndexOf("wolfcub", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsEligibleAdult(Character character)
    {
        return character != null
            && character.IsTamed()
            && character.GetComponent<Growup>() == null
            && GetAdultSpecies(character) != SpeciesType.None;
    }

    public static SpeciesType GetAdultSpecies(Character character)
    {
        return MapAdultPrefab(character?.name);
    }

    public static SpeciesType GetJuvenileSpecies(Character character)
    {
        Growup growup = character.GetComponent<Growup>();
        if (growup == null)
        {
            return SpeciesType.None;
        }

        if (growup.m_grownPrefab != null)
        {
            SpeciesType mapped = MapAdultPrefab(growup.m_grownPrefab.name);
            if (mapped != SpeciesType.None)
            {
                return mapped;
            }
        }

        if (growup.m_altGrownPrefabs != null)
        {
            foreach (Growup.GrownEntry entry in growup.m_altGrownPrefabs)
            {
                if (entry?.m_prefab == null)
                {
                    continue;
                }

                SpeciesType mapped = MapAdultPrefab(entry.m_prefab.name);
                if (mapped != SpeciesType.None)
                {
                    return mapped;
                }
            }
        }

        return MapAdultPrefab(character.name);
    }

    public static bool SpeciesMatches(SpeciesType juvenile, SpeciesType destination)
    {
        if (destination == SpeciesType.None)
        {
            return false;
        }

        if (destination == SpeciesType.All)
        {
            return true;
        }

        return juvenile == destination;
    }

    private static SpeciesType MapAdultPrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return SpeciesType.None;
        }

        foreach (KeyValuePair<string, SpeciesType> pair in AdultPrefabMap)
        {
            if (prefabName.IndexOf(pair.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return pair.Value;
            }
        }

        return SpeciesType.None;
    }
}
