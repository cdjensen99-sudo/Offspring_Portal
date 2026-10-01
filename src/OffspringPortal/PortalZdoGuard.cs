using UnityEngine;

namespace OffspringPortal;

/// <summary>
/// Prevents unconfigured orphan offspring_portal ZDOs from staying in the world or blocking spawn/teleport.
/// </summary>
public static class PortalZdoGuard
{
    /// <summary>
    /// Server-only: destroys the ZDO when it matches orphan maintenance rules.
    /// </summary>
    /// <returns>True if the ZDO was destroyed.</returns>
    public static bool RejectOrDestroyOrphanIfServer(ZDO zdo, string context)
    {
        if (!ModConfig.EnableOrphanRejectionOnLoad.Value || zdo == null || !zdo.IsValid())
        {
            return false;
        }

        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return false;
        }

        if (!PortalZdoMaintenance.TryClassifyOrphan(zdo, out string reason))
        {
            return false;
        }

        Vector3 position = zdo.GetPosition();
        OPLog.Warning(
            $"[OP] Rejecting orphan offspring portal ({context}): id={ZdoIdUtility.Format(zdo.m_uid)} " +
            $"pos=({position.x:F1}, {position.y:F1}, {position.z:F1}) reason={reason}");
        PortalZdoMaintenance.DestroyPortalZdo(zdo.m_uid);
        return true;
    }

    /// <summary>
    /// Server-only: scan spawn area and destroy matching orphan ZDOs (persists after world save).
    /// </summary>
    public static int PurgeSpawnOrphansOnServer(string context, bool requestWorldSave)
    {
        if (!ModConfig.PurgeSpawnOrphansOnServerConnect.Value || ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return 0;
        }

        float radius = ModConfig.SpawnCleanupRadiusMeters.Value;
        Vector3 center = PortalZdoMaintenance.GetSpawnAuditCenter();
        var matches = PortalZdoMaintenance.Scan(radius, center, entireWorld: false);
        if (matches.Count == 0)
        {
            return 0;
        }

        PortalZdoMaintenance.Clean(matches, dryRun: false, out string summary);
        OPLog.Info($"[OP] Spawn orphan purge ({context}): {summary.Replace("\n", " ")}");

        if (requestWorldSave && ModConfig.SaveWorldAfterSpawnOrphanPurge.Value && matches.Count > 0)
        {
            ZNet.instance.Save(sync: false, saveOtherPlayerProfiles: false, waitForNextFrame: true);
            OPLog.Info("[OP] Requested world save after spawn orphan purge.");
        }

        return matches.Count;
    }
}
