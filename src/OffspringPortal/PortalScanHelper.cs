using UnityEngine;

namespace OffspringPortal;

public static class PortalScanHelper
{
    private static float lastSkipLogTime;

    public static bool ShouldScanPortal(OPTeleportWorld portal)
    {
        if (portal == null || !PortalHelper.IsWorldReady() || ZNet.instance == null || ZNetScene.instance == null)
        {
            LogScanSkip(portal, "world not ready");
            return false;
        }

        ZDO portalZdo = portal.GetComponent<ZNetView>()?.GetZDO();
        if (portalZdo != null
            && portalZdo.GetPrefab() == PrefabNames.OffspringPortal.GetStableHashCode()
            && !PortalConfigGate.IsConfiguredForAutomation(portalZdo))
        {
            LogScanSkip(portal, "portal not configured yet");
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
            LogScanSkip(portal, "portal outside local active area on client");
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

        LogScanSkip(portal, "portal outside all connected players' active zones on dedicated server");
        return false;
    }

    private static void LogScanSkip(OPTeleportWorld portal, string reason)
    {
        if (!DiagnosticLog.ShouldLogRateLimited(ref lastSkipLogTime, 15f))
        {
            return;
        }

        ZDO zdo = portal?.GetComponent<ZNetView>()?.GetZDO();
        string portalId = zdo != null ? zdo.m_uid.ToString() : "(unknown)";
        Vector3 position = portal != null
            ? (portal.m_proximityRoot != null ? portal.m_proximityRoot.position : portal.transform.position)
            : Vector3.zero;
        DiagnosticLog.Verbose(
            $"Portal scan skipped for {portalId} at ({position.x:F1}, {position.y:F1}, {position.z:F1}): {reason}.");
    }
}
