namespace OffspringPortal;

public static class EggRecipeHelper
{
    private static readonly string[] KnownDualPurposeEggPrefabs =
    {
        "Egg"
    };

    public static bool IsUsedInRecipe(ItemDrop.ItemData.SharedData shared)
    {
        if (shared == null || ObjectDB.instance == null)
        {
            return false;
        }

        string sharedName = shared.m_name;
        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (recipe == null || !recipe.m_enabled || recipe.m_resources == null)
            {
                continue;
            }

            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (requirement?.m_resItem == null)
                {
                    continue;
                }

                ItemDrop.ItemData.SharedData requiredShared = requirement.m_resItem.m_itemData?.m_shared;
                if (requiredShared == null)
                {
                    continue;
                }

                if (requiredShared.m_name == sharedName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsUsedInRecipe(ItemDrop itemDrop)
    {
        if (itemDrop == null)
        {
            return false;
        }

        if (IsKnownDualPurposeEggPrefab(itemDrop.name))
        {
            return true;
        }

        return IsUsedInRecipe(itemDrop.m_itemData?.m_shared);
    }

    private static bool IsKnownDualPurposeEggPrefab(string prefabName)
    {
        string normalized = SpeciesKey.Normalize(prefabName);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        foreach (string known in KnownDualPurposeEggPrefabs)
        {
            if (normalized.Equals(known, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
