using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class PortalTravelGuard
{
    public const string PlayerTravelBlockedMessage =
        "Offspring portals route juveniles only. Use a standard portal to travel.";

    private const float PlayerTeleportBlockRadius = 4f;

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

    public static bool BlocksPlayerTeleportTo(Vector3 position, Player player, out string message)
    {
        message = null;
        if (player == null || !IsNearOffspringPortal(position))
        {
            return false;
        }

        message = PlayerTravelBlockedMessage;
        return true;
    }

    public static bool IsNearOffspringPortal(Vector3 position)
    {
        float radiusSq = PlayerTeleportBlockRadius * PlayerTeleportBlockRadius;

        OPTeleportWorld[] loadedPortals = Object.FindObjectsByType<OPTeleportWorld>(FindObjectsSortMode.None);
        foreach (OPTeleportWorld portal in loadedPortals)
        {
            if (!OffspringPortalPrefabs.IsOffspringPortal(portal))
            {
                continue;
            }

            if (IsNearPortalPosition(position, portal, radiusSq))
            {
                return true;
            }
        }

        if (ZDOMan.instance == null)
        {
            return false;
        }

        List<ZDO> portalZdos = new List<ZDO>();
        int index = 0;
        while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(PrefabNames.OffspringPortal, portalZdos, ref index))
        {
        }

        foreach (ZDO zdo in portalZdos)
        {
            if (IsNearZdoPortalPosition(position, zdo, radiusSq))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearPortalPosition(Vector3 position, OPTeleportWorld portal, float radiusSq)
    {
        if (portal == null)
        {
            return false;
        }

        if ((portal.transform.position - position).sqrMagnitude <= radiusSq)
        {
            return true;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        ZDOID portalId = nview?.GetZDO()?.m_uid ?? ZDOID.None;
        if (portalId == ZDOID.None)
        {
            return false;
        }

        Vector3 exitPosition = PortalPlacement.GetExitPosition(portalId, portal);
        return (exitPosition - position).sqrMagnitude <= radiusSq;
    }

    private static bool IsNearZdoPortalPosition(Vector3 position, ZDO zdo, float radiusSq)
    {
        if (zdo == null)
        {
            return false;
        }

        if ((zdo.GetPosition() - position).sqrMagnitude <= radiusSq)
        {
            return true;
        }

        Vector3 exitPosition = PortalPlacement.GetExitPosition(zdo.m_uid);
        return (exitPosition - position).sqrMagnitude <= radiusSq;
    }
}
