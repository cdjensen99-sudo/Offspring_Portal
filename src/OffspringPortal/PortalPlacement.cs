using UnityEngine;

namespace OffspringPortal;

public static class PortalPlacement
{
    public const float PortalScale = 0.5f;
    public const float ScaledExitDistance = 1f;

    public static Vector3 GetExitPosition(ZDOID portalId, TeleportWorld fallbackPortal = null)
    {
        Vector3 center = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        float exitDistance = ScaledExitDistance;

        ZDO zdo = ZDOMan.instance?.GetZDO(portalId);
        if (zdo != null)
        {
            center = zdo.GetPosition();
            rotation = zdo.GetRotation();
        }
        else if (fallbackPortal != null)
        {
            center = fallbackPortal.transform.position;
            rotation = fallbackPortal.transform.rotation;
            exitDistance = fallbackPortal.m_exitDistance;
        }

        GameObject instance = ZNetScene.instance?.FindInstance(portalId);
        TeleportWorld portal = instance != null ? instance.GetComponent<TeleportWorld>() : null;
        if (portal != null)
        {
            center = portal.transform.position;
            rotation = portal.transform.rotation;
            exitDistance = portal.m_exitDistance;
        }

        return center + rotation * Vector3.forward * exitDistance;
    }

    public static Quaternion GetExitRotation(ZDOID portalId, TeleportWorld fallbackPortal = null)
    {
        ZDO zdo = ZDOMan.instance?.GetZDO(portalId);
        if (zdo != null)
        {
            return zdo.GetRotation();
        }

        GameObject instance = ZNetScene.instance?.FindInstance(portalId);
        TeleportWorld portal = instance != null ? instance.GetComponent<TeleportWorld>() : null;
        if (portal != null)
        {
            return portal.transform.rotation;
        }

        return fallbackPortal != null ? fallbackPortal.transform.rotation : Quaternion.identity;
    }
}
