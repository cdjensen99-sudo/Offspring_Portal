using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class PortalHelper
{
    private static readonly HashSet<int> PendingInitialization = new HashSet<int>();

    public static bool TryGetPortal(TeleportWorld portal, out ZDOID id, out Vector3 position, out string speciesKey)
    {
        id = ZDOID.None;
        position = Vector3.zero;
        speciesKey = string.Empty;

        if (portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return false;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        id = zdo.m_uid;
        position = zdo.GetPosition();
        speciesKey = zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty);
        return true;
    }

    public static void EnsurePortalInitialized(TeleportWorld portal)
    {
        if (portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo == null)
        {
            OffspringPortalRuntime.Instance.StartCoroutine(DeferredEnsurePortalInitialized(portal));
            return;
        }

        ApplyPortalInitialization(portal, nview);
    }

    private static IEnumerator DeferredEnsurePortalInitialized(TeleportWorld portal)
    {
        int instanceId = portal.GetInstanceID();
        if (!PendingInitialization.Add(instanceId))
        {
            yield break;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        for (int i = 0; i < 60; i++)
        {
            if (portal == null)
            {
                PendingInitialization.Remove(instanceId);
                yield break;
            }

            if (nview?.GetZDO() != null)
            {
                break;
            }

            yield return null;
        }

        PendingInitialization.Remove(instanceId);
        if (portal != null && nview?.GetZDO() != null)
        {
            ApplyPortalInitialization(portal, nview);
        }
    }

    private static void ApplyPortalInitialization(TeleportWorld portal, ZNetView nview)
    {
        OffspringPortalPrefabs.EnsureIdentity(portal);
        portal.transform.localScale = Vector3.one * PortalPlacement.PortalScale;
        portal.m_exitDistance = PortalPlacement.ScaledExitDistance;

        if (!portal.enabled)
        {
            OffspringPortalInitializer.CompleteTeleportWorldAwake(portal, nview);
        }

        OffspringPortalPrefabs.EnsureRuntimeTriggers(portal);
        ZDO zdo = nview.GetZDO();
        if (zdo != null && ZNet.instance.IsServer())
        {
            PortalTravelGuard.ClearTravelBindings(zdo);
        }

        SyncRegistryFromPortal(portal);
    }

    public static void SyncRegistryFromPortal(TeleportWorld portal)
    {
        ZNetView nview = portal?.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        SyncRegistryFromZdo(zdo);
        PortalTravelGuard.ClearTravelBindings(zdo);
        DestinationRegistry.RefreshCapWarnings();
    }

    public static void SyncRegistryFromZdo(ZDO zdo)
    {
        if (zdo == null || zdo.GetPrefab() != PrefabNames.OffspringPortal.GetStableHashCode())
        {
            return;
        }

        PortalRole role = PortalRoleCatalog.FromZdo(zdo);
        string speciesKey = zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty);
        AdultDestination adultDestination = PortalRoleCatalog.GetAdultDestination(zdo);
        DestinationRegistry.RegisterOrUpdate(zdo.m_uid, zdo.GetPosition(), role, speciesKey, adultDestination);
    }

    public static void RebuildRegistryFromWorld()
    {
        DestinationRegistry.Clear();
        RebuildRegistryFromLoadedPortals();
        RebuildRegistryFromAllPortalZdos();
        BreedableSpeciesRegistry.RefreshFromAllBreeders();
        DestinationRegistry.RefreshCapWarnings();
        LogRegistryState("rebuilt");
    }

    private static void LogRegistryState(string reason)
    {
        int breeders = 0;
        int maturing = 0;
        int farm = 0;
        int cull = 0;
        int eggCollectors = 0;
        foreach (PortalRecord record in DestinationRegistry.GetAll())
        {
            switch (record.Role)
            {
                case PortalRole.Maturing:
                    maturing++;
                    break;
                case PortalRole.Farm:
                    farm++;
                    break;
                case PortalRole.Cull:
                    cull++;
                    break;
                case PortalRole.EggCollector:
                    eggCollectors++;
                    break;
                default:
                    breeders++;
                    break;
            }
        }

        OffspringPortalPlugin.Log.LogInfo(
            $"Portal registry {reason}: {breeders} breeder(s), {maturing} maturing, {farm} farm, {cull} cull, {eggCollectors} egg collector(s).");
    }

    private static void RebuildRegistryFromLoadedPortals()
    {
        if (ZNetScene.instance == null)
        {
            return;
        }

        TeleportWorld[] portals = Object.FindObjectsByType<TeleportWorld>(FindObjectsSortMode.None);
        foreach (TeleportWorld portal in portals)
        {
            if (OffspringPortalPrefabs.IsOffspringPortal(portal))
            {
                SyncRegistryFromPortal(portal);
            }
        }
    }

    private static void RebuildRegistryFromAllPortalZdos()
    {
        if (ZDOMan.instance == null)
        {
            return;
        }

        List<ZDO> portalZdos = new List<ZDO>();
        int index = 0;
        while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(PrefabNames.OffspringPortal, portalZdos, ref index))
        {
        }

        foreach (ZDO zdo in portalZdos)
        {
            SyncRegistryFromZdo(zdo);
        }
    }
}
