using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
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

        int patched = 0;
        patched += Patch(harmony, xPortalType, "OnPrePortalHover", nameof(OnPrePortalHoverPrefix)) ? 1 : 0;
        patched += Patch(harmony, xPortalType, "OnPortalPlaced", nameof(OnPortalPlacedPrefix)) ? 1 : 0;
        patched += Patch(harmony, xPortalType, "OnPortalRequestText", nameof(OnPortalRequestTextPrefix)) ? 1 : 0;

        Type managerType = AccessTools.TypeByName("XPortal.KnownPortalsManager");
        if (managerType != null)
        {
            patched += Patch(harmony, managerType, "AddOrUpdate", nameof(KnownPortalsAddOrUpdatePrefix)) ? 1 : 0;
            patched += Patch(harmony, managerType, "UpdateFromZDOList", nameof(KnownPortalsUpdateFromZdoListPrefix)) ? 1 : 0;
            patched += PatchPostfix(harmony, managerType, "GetList", nameof(KnownPortalsListPostfix)) ? 1 : 0;
            patched += PatchPostfix(harmony, managerType, "GetSortedList", nameof(KnownPortalsListPostfix)) ? 1 : 0;
        }

        OffspringPortalPlugin.Log.LogInfo(
            $"XPortal compatibility enabled: offspring portals excluded from travel network ({patched} patch(es)).");
    }

    public static void PurgeKnownPortals()
    {
        Type managerType = AccessTools.TypeByName("XPortal.KnownPortalsManager");
        if (managerType == null)
        {
            return;
        }

        object instance = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null);
        if (instance == null)
        {
            return;
        }

        MethodInfo getList = AccessTools.Method(managerType, "GetList");
        MethodInfo remove = AccessTools.Method(managerType, "Remove", new[] { typeof(ZDOID) });
        if (getList == null || remove == null)
        {
            return;
        }

        if (getList.Invoke(instance, null) is not IList list)
        {
            return;
        }

        int removed = 0;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            ZDOID id = GetKnownPortalId(list[i]);
            if (!ShouldBlock(id))
            {
                continue;
            }

            remove.Invoke(instance, new object[] { id });
            removed++;
        }

        if (removed > 0)
        {
            OffspringPortalPlugin.Log.LogInfo($"Removed {removed} offspring portal(s) from XPortal's known portal list.");
        }
    }

    private static bool Patch(Harmony harmony, Type targetType, string methodName, string handlerName)
    {
        MethodInfo target = AccessTools.Method(targetType, methodName);
        MethodInfo handler = AccessTools.Method(typeof(XPortalCompat), handlerName);
        if (target == null || handler == null)
        {
            return false;
        }

        harmony.Patch(target, prefix: new HarmonyMethod(handler));
        return true;
    }

    private static bool PatchPostfix(Harmony harmony, Type targetType, string methodName, string handlerName)
    {
        MethodInfo target = AccessTools.Method(targetType, methodName);
        MethodInfo handler = AccessTools.Method(typeof(XPortalCompat), handlerName);
        if (target == null || handler == null)
        {
            return false;
        }

        harmony.Patch(target, postfix: new HarmonyMethod(handler));
        return true;
    }

    private static bool ShouldBlock(ZDOID portalId)
    {
        if (portalId == ZDOID.None)
        {
            return false;
        }

        ZDO zdo = ZDOMan.instance?.GetZDO(portalId);
        if (PortalTravelGuard.IsOffspringPortalZdo(zdo))
        {
            return true;
        }

        TeleportWorld portal = FindPortal(portalId);
        return portal != null && OffspringPortalPrefabs.IsOffspringPortal(portal);
    }

    private static bool ShouldBlockZdo(ZDO zdo)
    {
        if (PortalTravelGuard.IsOffspringPortalZdo(zdo))
        {
            return true;
        }

        if (zdo == null)
        {
            return false;
        }

        TeleportWorld portal = FindPortal(zdo.m_uid);
        return portal != null && OffspringPortalPrefabs.IsOffspringPortal(portal);
    }

    private static bool OnPrePortalHoverPrefix(ZDOID portalId, Vector3 location, out string __result)
    {
        __result = string.Empty;
        if (!ShouldBlock(portalId))
        {
            return true;
        }

        ClearTravelBindings(portalId);
        return false;
    }

    private static bool OnPortalPlacedPrefix(ZDOID portalId, Vector3 location)
    {
        if (!ShouldBlock(portalId))
        {
            return true;
        }

        ClearTravelBindings(portalId);
        return false;
    }

    private static bool OnPortalRequestTextPrefix(ZDOID portalId)
    {
        return !ShouldBlock(portalId);
    }

    private static bool KnownPortalsAddOrUpdatePrefix(object portal)
    {
        return !ShouldBlock(GetKnownPortalId(portal));
    }

    private static void KnownPortalsUpdateFromZdoListPrefix(List<ZDO> zdoList)
    {
        if (zdoList == null)
        {
            return;
        }

        for (int i = zdoList.Count - 1; i >= 0; i--)
        {
            if (ShouldBlockZdo(zdoList[i]))
            {
                zdoList.RemoveAt(i);
            }
        }
    }

    private static void KnownPortalsListPostfix(ref object __result)
    {
        if (__result is IList list)
        {
            FilterKnownPortalList(list);
        }
    }

    private static void FilterKnownPortalList(IList list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (ShouldBlock(GetKnownPortalId(list[i])))
            {
                list.RemoveAt(i);
            }
        }
    }

    private static ZDOID GetKnownPortalId(object knownPortal)
    {
        if (knownPortal == null)
        {
            return ZDOID.None;
        }

        Type type = knownPortal.GetType();
        FieldInfo idField = type.GetField("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idField != null && idField.FieldType == typeof(ZDOID))
        {
            return (ZDOID)idField.GetValue(knownPortal);
        }

        PropertyInfo idProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProperty != null && idProperty.PropertyType == typeof(ZDOID))
        {
            return (ZDOID)idProperty.GetValue(knownPortal);
        }

        return ZDOID.None;
    }

    private static void ClearTravelBindings(ZDOID portalId)
    {
        ZDO zdo = ZDOMan.instance?.GetZDO(portalId);
        if (zdo != null)
        {
            PortalTravelGuard.ClearTravelBindings(zdo);
            return;
        }

        PortalTravelGuard.ClearTravelBindings(FindPortal(portalId));
    }

    private static TeleportWorld FindPortal(ZDOID portalId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(portalId);
        return instance != null ? instance.GetComponent<TeleportWorld>() : null;
    }
}
