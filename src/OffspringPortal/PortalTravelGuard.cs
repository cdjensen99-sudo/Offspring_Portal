using UnityEngine;

namespace OffspringPortal;

public static class PortalTravelGuard
{
    public const string PlayerTravelBlockedMessage =
        "Offspring portals route juveniles only. Use a standard portal to travel.";

    private const string XPortalTargetField = "XPortal_TargetId";
    private const string XPortalPreviousField = "XPortal_PreviousId";

    public static bool IsOffspringPortalZdo(ZDO zdo)
    {
        if (zdo == null)
        {
            return false;
        }

        if (zdo.GetPrefab() == PrefabNames.OffspringPortal.GetStableHashCode())
        {
            return true;
        }

        return !string.IsNullOrEmpty(zdo.GetString(ZdoFields.PortalRole, string.Empty));
    }

    public static bool TryGetConnectedPortal(TeleportWorld portal, out TeleportWorld connected)
    {
        connected = null;
        if (portal == null)
        {
            return false;
        }

        ZDO zdo = portal.GetComponent<ZNetView>()?.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        ZDOID connectionId = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
        if (connectionId == ZDOID.None)
        {
            return false;
        }

        GameObject instance = ZNetScene.instance?.FindInstance(connectionId);
        connected = instance != null ? instance.GetComponent<TeleportWorld>() : null;
        return connected != null;
    }

    public static bool BlocksPlayerTravel(TeleportWorld source, Player player, out string message)
    {
        message = null;
        if (source == null || player == null)
        {
            return false;
        }

        if (OffspringPortalPrefabs.IsOffspringPortal(source))
        {
            message = PlayerTravelBlockedMessage;
            return true;
        }

        if (TryGetConnectedPortal(source, out TeleportWorld destination)
            && OffspringPortalPrefabs.IsOffspringPortal(destination))
        {
            message = PlayerTravelBlockedMessage;
            return true;
        }

        return false;
    }

    public static void ClearTravelBindings(ZDO zdo)
    {
        if (zdo == null)
        {
            return;
        }

        zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
        zdo.Set(XPortalTargetField, ZDOID.None);
        zdo.Set(XPortalPreviousField, ZDOID.None);
    }

    public static void ClearTravelBindings(TeleportWorld portal)
    {
        if (portal == null || !ZNet.instance.IsServer())
        {
            return;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo == null || !nview.IsOwner())
        {
            return;
        }

        ClearTravelBindings(zdo);
    }

    public static void ClearAllOffspringTravelBindings()
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        TeleportWorld[] portals = Object.FindObjectsByType<TeleportWorld>(FindObjectsSortMode.None);
        foreach (TeleportWorld portal in portals)
        {
            if (!OffspringPortalPrefabs.IsOffspringPortal(portal))
            {
                continue;
            }

            ClearTravelBindings(portal);
        }
    }
}
