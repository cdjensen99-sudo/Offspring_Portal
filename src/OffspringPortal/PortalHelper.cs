using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class PortalHelper
{
    public static bool TryGetPortal(TeleportWorld portal, out ZDOID id, out Vector3 position, out SpeciesType species)
    {
        id = ZDOID.None;
        position = Vector3.zero;
        species = SpeciesType.None;

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
        species = SpeciesCatalog.FromStorageValue(zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty));
        return true;
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
        SpeciesType species = SpeciesCatalog.FromStorageValue(zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty));
        AdultDestination adultDestination = PortalRoleCatalog.GetAdultDestination(zdo);
        DestinationRegistry.RegisterOrUpdate(zdo.m_uid, zdo.GetPosition(), role, species, adultDestination);
    }

    public static void RebuildRegistryFromWorld()
    {
        DestinationRegistry.Clear();
        RebuildRegistryFromLoadedPortals();
        RebuildRegistryFromAllPortalZdos();
        DestinationRegistry.RefreshCapWarnings();
        LogRegistryState("rebuilt");
    }

    private static void LogRegistryState(string reason)
    {
        int breeders = 0;
        int maturing = 0;
        int farm = 0;
        int cull = 0;
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
                default:
                    breeders++;
                    break;
            }
        }

        OffspringPortalPlugin.Log.LogInfo(
            $"Portal registry {reason}: {breeders} breeder(s), {maturing} maturing, {farm} farm, {cull} cull.");
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
