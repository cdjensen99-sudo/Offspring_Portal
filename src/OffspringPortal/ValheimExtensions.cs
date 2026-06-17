using UnityEngine;

namespace OffspringPortal;

internal static class ValheimExtensions
{
    public static ZNetView GetNview(this Character character)
    {
        return character != null ? character.GetComponent<ZNetView>() : null;
    }

    public static ZDO GetZdo(this Character character)
    {
        ZNetView nview = character.GetNview();
        return nview != null ? nview.GetZDO() : null;
    }
}
