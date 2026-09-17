using UnityEngine;

namespace OffspringPortal;

public static class PortalScanHelper
{
    public static bool ShouldScanPortal(OPTeleportWorld portal)
    {
        if (portal == null || !PortalHelper.IsWorldReady() || ZNet.instance == null || ZNetScene.instance == null)
        {
            return false;
        }

        Vector3 position = portal.m_proximityRoot != null
            ? portal.m_proximityRoot.position
            : portal.transform.position;

        if (!ZNetScene.instance.OutsideActiveArea(position))
        {
            return true;
        }

        if (!ZNet.instance.IsServer())
        {
            return false;
        }

        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            if (peer == null || !peer.IsReady())
            {
                continue;
            }

            Vector2s zone = ZoneSystem.GetZone(peer.GetRefPos());
            if (!ZNetScene.OutsideActiveArea(position, zone))
            {
                return true;
            }
        }

        return false;
    }
}
