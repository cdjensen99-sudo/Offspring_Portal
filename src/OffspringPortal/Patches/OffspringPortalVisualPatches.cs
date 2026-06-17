using HarmonyLib;
using UnityEngine;

namespace OffspringPortal.Patches;

[HarmonyPatch(typeof(TeleportWorld), "HaveTarget")]
public static class OffspringPortalHaveTargetPatch
{
    private static void Postfix(TeleportWorld __instance, ref bool __result)
    {
        if (__result || !OffspringPortalPrefabs.IsOffspringPortal(__instance))
        {
            return;
        }

        ZDO zdo = __instance.GetComponent<ZNetView>()?.GetZDO();
        if (zdo != null)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(TeleportWorld), "UpdatePortal")]
public static class OffspringPortalUpdatePortalPatch
{
    private static void Postfix(TeleportWorld __instance)
    {
        if (!OffspringPortalPrefabs.IsOffspringPortal(__instance))
        {
            return;
        }

        ZDO zdo = __instance.GetComponent<ZNetView>()?.GetZDO();
        if (zdo == null)
        {
            return;
        }

        Traverse traverse = Traverse.Create(__instance);
        traverse.Field<bool>("m_hadTarget").Value = true;

        TextMesh textMesh = traverse.Field<TextMesh>("m_textMeshField").Value;
        if (textMesh != null)
        {
            textMesh.color = traverse.Field<Color>("m_colorTargetfound").Value;
        }
    }
}
