namespace OffspringPortal;

public static class EggRecipeHelper
{
    public static bool IsUsedInRecipe(ItemDrop.ItemData.SharedData shared)
    {
        if (shared == null || ObjectDB.instance == null)
        {
            return false;
        }

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

                if (requirement.m_resItem.m_itemData.m_shared.m_name == shared.m_name)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsUsedInRecipe(ItemDrop itemDrop)
    {
        return itemDrop != null && IsUsedInRecipe(itemDrop.m_itemData?.m_shared);
    }
}
