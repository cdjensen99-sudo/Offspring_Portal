using UnityEngine;

namespace OffspringPortal;

public static class SpeciesDiscovery
{
    // Valheim 1.0 seals are ambient/wild — no Procreation/Tameable breeding loop. Not a portal target.

    public static bool TryDiscoverFromCharacter(Character character, out BreedableSpeciesInfo info)
    {
        info = null;
        if (character == null || !character.IsTamed())
        {
            return false;
        }

        Procreation procreation = character.GetComponent<Procreation>();
        if (procreation == null || procreation.m_offspring == null)
        {
            return false;
        }

        GameObject offspringPrefab = ResolvePrefab(procreation.m_offspring);
        if (offspringPrefab == null)
        {
            return false;
        }

        Character offspringCharacter = offspringPrefab.GetComponent<Character>();
        if (offspringCharacter != null)
        {
            return TryDiscoverLiveBirth(offspringCharacter, out info);
        }

        if (offspringPrefab.GetComponent<ItemDrop>() != null || offspringPrefab.GetComponent<EggGrow>() != null)
        {
            return TryDiscoverEggLayer(offspringPrefab, out info);
        }

        return false;
    }

    public static bool TryDiscoverFallback(Character character, out BreedableSpeciesInfo info)
    {
        info = null;
        if (character == null || !character.IsTamed() || character.GetComponent<Growup>() != null)
        {
            return false;
        }

        Procreation procreation = character.GetComponent<Procreation>();
        if (procreation == null)
        {
            return false;
        }

        string speciesKey = GetAdultSpeciesKey(character);
        if (string.IsNullOrEmpty(speciesKey))
        {
            return false;
        }

        info = new BreedableSpeciesInfo
        {
            Key = speciesKey,
            DisplayName = SpeciesKey.GetDisplayName(speciesKey),
            IsEggLayer = false,
            IsDualPurposeEgg = false,
            AdultPrefabName = speciesKey
        };
        return true;
    }

    public static bool TryGetEggSpeciesInfo(ItemDrop egg, out BreedableSpeciesInfo info)
    {
        info = null;
        if (egg == null)
        {
            return false;
        }

        EggGrow eggGrow = egg.GetComponent<EggGrow>();
        if (eggGrow == null)
        {
            return false;
        }

        if (TryBuildEggSpeciesInfoFromGrowChain(egg, eggGrow, out info))
        {
            return true;
        }

        return BreedableSpeciesRegistry.TryGetByEggPrefab(egg.name, out info);
    }

    internal static bool TryBuildEggSpeciesInfoFromGrowChain(
        ItemDrop egg,
        EggGrow eggGrow,
        out BreedableSpeciesInfo info)
    {
        info = null;
        if (egg == null || eggGrow == null)
        {
            return false;
        }

        GameObject grownPrefab = ResolvePrefab(eggGrow.m_grownPrefab);
        if (grownPrefab == null)
        {
            return false;
        }

        Character hatchling = grownPrefab.GetComponent<Character>();
        if (hatchling == null)
        {
            hatchling = grownPrefab.GetComponentInChildren<Character>(true);
        }

        if (hatchling == null)
        {
            return false;
        }

        string adultKey = ResolveEggAdultKey(hatchling);
        if (string.IsNullOrEmpty(adultKey))
        {
            return false;
        }

        bool dualPurpose = EggRecipeHelper.IsUsedInRecipe(egg);
        if (!dualPurpose && BreedableSpeciesRegistry.TryGet(adultKey, out BreedableSpeciesInfo registered))
        {
            dualPurpose = registered.IsDualPurposeEgg;
        }

        info = new BreedableSpeciesInfo
        {
            Key = adultKey,
            DisplayName = SpeciesKey.GetDisplayName(adultKey),
            IsEggLayer = true,
            IsDualPurposeEgg = dualPurpose,
            EggCollectorKey = dualPurpose ? SpeciesKey.BuildEggCollectorKey(adultKey) : string.Empty,
            EggPrefabName = SpeciesKey.Normalize(egg.name),
            AdultPrefabName = adultKey,
            HatchlingPrefabName = SpeciesKey.Normalize(hatchling.name)
        };
        return true;
    }

    private static string ResolveEggAdultKey(Character hatchling)
    {
        if (hatchling == null)
        {
            return string.Empty;
        }

        Growup growup = hatchling.GetComponent<Growup>();
        if (growup != null)
        {
            string fromGrowup = GetAdultKeyFromGrowup(growup);
            if (!string.IsNullOrEmpty(fromGrowup))
            {
                return fromGrowup;
            }

            GameObject adultPrefab = ResolvePrefab(growup.m_grownPrefab);
            if (adultPrefab != null)
            {
                string fromAdultPrefab = ResolveAdultSpeciesKeyFromPrefab(adultPrefab.name);
                if (!string.IsNullOrEmpty(fromAdultPrefab))
                {
                    return fromAdultPrefab;
                }
            }
        }

        return GetAdultSpeciesKey(hatchling);
    }

    public static string GetSpeciesKey(Character character)
    {
        if (character == null)
        {
            return string.Empty;
        }

        Growup growup = character.GetComponent<Growup>();
        if (growup != null)
        {
            string juvenileKey = GetAdultKeyFromGrowup(growup);
            if (!string.IsNullOrEmpty(juvenileKey))
            {
                return juvenileKey;
            }
        }

        return GetAdultSpeciesKey(character);
    }

    public static string GetAdultSpeciesKey(Character character)
    {
        if (character == null)
        {
            return string.Empty;
        }

        string prefabName = SpeciesKey.Normalize(character.name);
        if (string.IsNullOrEmpty(prefabName))
        {
            return string.Empty;
        }

        if (prefabName.IndexOf("Hen", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SpeciesCatalog.ToStorageValue(SpeciesType.Chicken);
        }

        SpeciesType mapped = SpeciesCatalog.FromStorageValue(prefabName);
        if (mapped != SpeciesType.None && mapped != SpeciesType.All)
        {
            return SpeciesCatalog.ToStorageValue(mapped);
        }

        foreach (string fragment in SpeciesCatalog.KnownAdultPrefabFragments)
        {
            if (prefabName.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SpeciesType fragmentSpecies = SpeciesCatalog.FromStorageValue(fragment);
                if (fragmentSpecies == SpeciesType.Chicken
                    && prefabName.IndexOf("Hen", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SpeciesCatalog.ToStorageValue(SpeciesType.Chicken);
                }

                if (fragmentSpecies != SpeciesType.None)
                {
                    return SpeciesCatalog.ToStorageValue(fragmentSpecies);
                }
            }
        }

        return prefabName;
    }

    private static bool TryDiscoverLiveBirth(Character offspringCharacter, out BreedableSpeciesInfo info)
    {
        info = null;
        Growup growup = offspringCharacter.GetComponent<Growup>();
        if (growup == null)
        {
            return false;
        }

        string adultKey = GetAdultKeyFromGrowup(growup);
        if (string.IsNullOrEmpty(adultKey))
        {
            adultKey = SpeciesKey.Normalize(offspringCharacter.name);
        }

        if (string.IsNullOrEmpty(adultKey))
        {
            return false;
        }

        info = new BreedableSpeciesInfo
        {
            Key = adultKey,
            DisplayName = SpeciesKey.GetDisplayName(adultKey),
            IsEggLayer = false,
            IsDualPurposeEgg = false,
            AdultPrefabName = adultKey,
            HatchlingPrefabName = SpeciesKey.Normalize(offspringCharacter.name)
        };
        return true;
    }

    private static bool TryDiscoverEggLayer(GameObject eggPrefab, out BreedableSpeciesInfo info)
    {
        info = null;
        ItemDrop itemDrop = eggPrefab.GetComponent<ItemDrop>();
        EggGrow eggGrow = eggPrefab.GetComponent<EggGrow>();
        if (itemDrop == null || eggGrow == null)
        {
            return false;
        }

        if (!TryBuildEggSpeciesInfoFromGrowChain(itemDrop, eggGrow, out info))
        {
            return false;
        }

        info.EggPrefabName = SpeciesKey.Normalize(eggPrefab.name);
        return true;
    }

    public static string ResolveAdultSpeciesKeyFromPrefab(string prefabName)
    {
        return MapGrowupTarget(prefabName);
    }

    private static string GetAdultKeyFromGrowup(Growup growup)
    {
        if (growup?.m_grownPrefab != null)
        {
            string mapped = MapGrowupTarget(growup.m_grownPrefab.name);
            if (!string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }
        }

        if (growup?.m_altGrownPrefabs == null)
        {
            return string.Empty;
        }

        foreach (Growup.GrownEntry entry in growup.m_altGrownPrefabs)
        {
            if (entry?.m_prefab == null)
            {
                continue;
            }

            string mapped = MapGrowupTarget(entry.m_prefab.name);
            if (!string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }
        }

        return string.Empty;
    }

    private static string MapGrowupTarget(string prefabName)
    {
        string normalized = SpeciesKey.Normalize(prefabName);
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        if (normalized.IndexOf("Hen", System.StringComparison.OrdinalIgnoreCase) >= 0)
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
            if (normalized.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (fragment.Equals("Chicken", System.StringComparison.OrdinalIgnoreCase)
                    && normalized.IndexOf("Hen", System.StringComparison.OrdinalIgnoreCase) >= 0)
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

    private static GameObject ResolvePrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        if (ZNetScene.instance == null)
        {
            return prefab;
        }

        string prefabName = Utils.GetPrefabName(prefab.name);
        GameObject resolved = ZNetScene.instance.GetPrefab(prefabName);
        return resolved != null ? resolved : prefab;
    }
}
