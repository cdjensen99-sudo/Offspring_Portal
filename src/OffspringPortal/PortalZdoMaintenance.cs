using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OffspringPortal;

public sealed class PortalMaintenanceEntry
{
    public ZDOID Id;
    public Vector3 Position;
    public string PrefabName;
    public bool Configured;
    public string Reason;
}

public static class PortalZdoMaintenance
{
    public static List<PortalMaintenanceEntry> Scan(float radiusMeters, Vector3 center, bool entireWorld)
    {
        List<PortalMaintenanceEntry> results = new List<PortalMaintenanceEntry>();
        if (ZDOMan.instance == null)
        {
            return results;
        }

        List<ZDO> portalZdos = new List<ZDO>();
        int index = 0;
        while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(PrefabNames.OffspringPortal, portalZdos, ref index))
        {
        }

        float radiusSq = radiusMeters > 0f ? radiusMeters * radiusMeters : 0f;
        foreach (ZDO zdo in portalZdos)
        {
            if (zdo == null || !zdo.IsValid())
            {
                continue;
            }

            Vector3 position = zdo.GetPosition();
            if (!entireWorld && radiusMeters > 0f && (position - center).sqrMagnitude > radiusSq)
            {
                continue;
            }

            if (!TryClassifyOrphan(zdo, out string reason))
            {
                continue;
            }

            results.Add(new PortalMaintenanceEntry
            {
                Id = zdo.m_uid,
                Position = position,
                PrefabName = PrefabNames.OffspringPortal,
                Configured = PortalConfigGate.IsConfiguredForAutomation(zdo),
                Reason = reason,
            });
        }

        return results;
    }

    public static int Clean(List<PortalMaintenanceEntry> entries, bool dryRun, out string summary)
    {
        StringBuilder builder = new StringBuilder();
        int removed = 0;
        if (entries == null || entries.Count == 0)
        {
            summary = "[OP] No orphan offspring_portal ZDOs matched the cleanup rules.";
            return 0;
        }

        foreach (PortalMaintenanceEntry entry in entries)
        {
            string idText = ZdoIdUtility.Format(entry.Id);
            string line =
                $"[OP] {(dryRun ? "Would remove" : "Removed")} {idText} ({entry.PrefabName}) " +
                $"at ({entry.Position.x:F1}, {entry.Position.y:F1}, {entry.Position.z:F1}) — {entry.Reason}";
            builder.AppendLine(line);

            if (!dryRun && DestroyPortalZdo(entry.Id))
            {
                removed++;
            }
        }

        if (dryRun)
        {
            builder.AppendLine($"[OP] Dry run complete: {entries.Count} orphan(s) matched.");
        }
        else
        {
            builder.AppendLine($"[OP] Cleanup complete: removed {removed} orphan(s).");
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                PortalHelper.RebuildRegistryFromWorld(includeLegacyMigration: false);
                PortalRegistrySync.BroadcastFromServer();
            }
        }

        summary = builder.ToString();
        return dryRun ? 0 : removed;
    }

    public static void RunStartupAuditIfEnabled()
    {
        if (!ModConfig.EnableStartupPortalAudit.Value || ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        float radius = ModConfig.StartupAuditRadiusMeters.Value;
        Vector3 center = GetSpawnAuditCenter();
        List<PortalMaintenanceEntry> matches = Scan(radius, center, entireWorld: ModConfig.StartupAuditEntireWorld.Value);
        if (matches.Count == 0)
        {
            OPLog.Info($"[OP] Startup portal audit: no orphan offspring_portal ZDOs found.");
            return;
        }

        foreach (PortalMaintenanceEntry entry in matches)
        {
            OPLog.Warning(
                $"[OP] Startup audit orphan: id={ZdoIdUtility.Format(entry.Id)} " +
                $"pos=({entry.Position.x:F1}, {entry.Position.y:F1}, {entry.Position.z:F1}) reason={entry.Reason}");
        }

        if (ModConfig.StartupAuditRemoveOrphans.Value)
        {
            Clean(matches, dryRun: false, out string summary);
            OPLog.Info(summary);
        }
        else
        {
            OPLog.Warning($"[OP] Startup audit found {matches.Count} orphan(s). Set StartupAuditRemoveOrphans=true to delete them.");
        }
    }

    public static bool DestroyPortalZdo(ZDOID id)
    {
        if (!ZdoIdUtility.IsValidId(id) || ZDOMan.instance == null)
        {
            return false;
        }

        ZDO zdo = ZdoIdUtility.TryGetZdo(id);
        if (zdo == null)
        {
            DestinationRegistry.Remove(id);
            return false;
        }

        if (ZNetScene.instance != null)
        {
            GameObject instance = ZNetScene.instance.FindInstance(id);
            if (instance != null)
            {
                ZNetView view = instance.GetComponent<ZNetView>();
                if (view != null)
                {
                    view.Destroy();
                }
                else
                {
                    Object.Destroy(instance);
                }
            }
        }

        DestinationRegistry.Remove(id);
        if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            ZDOMan.instance.DestroyZDO(zdo);
        }

        return true;
    }

    internal static bool TryClassifyOrphan(ZDO zdo, out string reason)
    {
        reason = null;
        if (zdo == null || !zdo.IsValid())
        {
            return false;
        }

        bool isOffspringPrefab = zdo.GetPrefab() == PrefabNames.OffspringPortal.GetStableHashCode();
        bool hasPortalRole = !string.IsNullOrEmpty(zdo.GetString(ZdoFields.PortalRole, string.Empty));
        if (!isOffspringPrefab && !hasPortalRole)
        {
            return false;
        }

        if (PortalConfigGate.IsConfiguredForAutomation(zdo))
        {
            return false;
        }

        Vector3 position = zdo.GetPosition();
        if (position.y < ModConfig.OrphanVoidMinY.Value)
        {
            reason = "void";
            return true;
        }

        float horizontalOrigin = new Vector2(position.x, position.z).magnitude;
        if (horizontalOrigin <= ModConfig.SpawnCleanupRadiusMeters.Value)
        {
            if (position.y >= ModConfig.SpawnSkyMinY.Value)
            {
                reason = "spawn_sky_unconfigured";
                return true;
            }

            reason = "spawn_unconfigured";
            return true;
        }

        if (ModConfig.RemoveAllUnconfiguredPortalZdos.Value)
        {
            reason = "unconfigured";
            return true;
        }

        return false;
    }

    public static int CleanLoadedViewsNearSpawn(float radiusMeters, bool dryRun)
    {
        OPTeleportWorld[] portals = Object.FindObjectsByType<OPTeleportWorld>(FindObjectsSortMode.None);
        if (portals == null || portals.Length == 0)
        {
            return 0;
        }

        float radiusSq = radiusMeters * radiusMeters;
        Vector3 center = GetWorldSpawnCenter();
        int removed = 0;
        foreach (OPTeleportWorld portal in portals)
        {
            if (portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
            {
                continue;
            }

            if ((portal.transform.position - center).sqrMagnitude > radiusSq)
            {
                continue;
            }

            ZDO zdo = portal.GetComponent<ZNetView>()?.GetZDO();
            if (PortalConfigGate.IsConfiguredForAutomation(zdo))
            {
                continue;
            }

            if (dryRun)
            {
                removed++;
                continue;
            }

            ZDOID id = portal.GetComponent<ZNetView>()?.GetZDO()?.m_uid ?? ZDOID.None;
            if (ZdoIdUtility.IsValidId(id))
            {
                DestroyPortalZdo(id);
            }
            else if (portal.gameObject != null)
            {
                Object.Destroy(portal.gameObject);
            }

            removed++;
        }

        return removed;
    }

    internal static Vector3 GetSpawnAuditCenter()
    {
        if (ModConfig.StartupAuditUseWorldOrigin.Value)
        {
            return Vector3.zero;
        }

        return GetWorldSpawnCenter();
    }

    private static Vector3 GetWorldSpawnCenter()
    {
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetLocationIcon("start", out Vector3 startPos))
        {
            return startPos;
        }

        return Vector3.zero;
    }
}
