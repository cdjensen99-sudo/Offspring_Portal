using UnityEngine;

namespace OffspringPortal;

public static class PortalScanHelper
{
    public static bool ShouldScanPortal(OPTeleportWorld portal)
    {
        if (portal == null || !PortalHelper.IsWorldReady() || ZNet.instance == null)
        {
            return false;
        }

        Vector3 position = portal.m_proximityRoot != null
            ? portal.m_proximityRoot.position
            : portal.transform.position;

        return !ZNetScene.instance.OutsideActiveArea(position);
    }
}
