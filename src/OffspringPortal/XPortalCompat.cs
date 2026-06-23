using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace OffspringPortal;

internal static class XPortalCompat
{
    public static void TryApply(Harmony harmony)
    {
        Type xPortalType = AccessTools.TypeByName("XPortal.XPortal");
        if (xPortalType == null)
        {
            return;
        }

        Patch(harmony, xPortalType, "OnPrePortalHover", nameof(OnPrePortalHoverPrefix));
        Patch(harmony, xPortalType, "OnPortalPlaced", nameof(OnPortalPlacedPrefix));
        Patch(harmony, xPortalType, "OnPortalRequestText", nameof(OnPortalRequestTextPrefix));
        OffspringPortalPlugin.Log.LogInfo("XPortal compatibility enabled: offspring portals excluded from travel network.");
    }

    private static void Patch(Harmony harmony, Type targetType, string methodName, string handlerName)
    {
        MethodInfo target = AccessTools.Method(targetType, methodName);
        MethodInfo handler = AccessTools.Method(typeof(XPortalCompat), handlerName);
        if (target == null || handler == null)
        {
            return;
        }

        harmony.Patch(target, prefix: new HarmonyMethod(handler));
    }

    private static bool ShouldBlock(ZDOID portalId)
    {
        ZDO zdo = ZDOMan.instance?.GetZDO(portalId);
        return PortalTravelGuard.IsOffspringPortalZdo(zdo);
    }

    private static bool OnPrePortalHoverPrefix(ZDOID portalId)
    {
        return !ShouldBlock(portalId);
    }

    private static bool OnPortalPlacedPrefix(ZDOID portalId)
    {
        if (!ShouldBlock(portalId))
        {
            return true;
        }

        TeleportWorld portal = FindPortal(portalId);
        PortalTravelGuard.ClearTravelBindings(portal);
        return false;
    }

    private static bool OnPortalRequestTextPrefix(ZDOID portalId)
    {
        return !ShouldBlock(portalId);
    }

    private static TeleportWorld FindPortal(ZDOID portalId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(portalId);
        return instance != null ? instance.GetComponent<TeleportWorld>() : null;
    }
}
