using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class PortalRpc
{
    private const float ConfigRetrySeconds = 10f;

    private static readonly List<PendingPortalConfig> PendingConfigs = new List<PendingPortalConfig>();

    private struct PendingPortalConfig
    {
        public ZDOID PortalId;
        public PortalRole Role;
        public string SpeciesKey;
        public string Name;
        public AdultDestination AdultDestination;
        public float ExpireTime;
    }

    public static bool SetPortalConfig(
        ZDOID portalId,
        PortalRole role,
        string speciesKey,
        string name,
        AdultDestination adultDestination)
    {
        string sanitizedName = PortalDisplayHelper.SanitizeName(name);
        string resolvedSpeciesKey = PortalRoleCatalog.ResolveSpeciesKey(role, speciesKey);
        AdultDestination resolvedDestination = PortalRoleCatalog.ResolveAdultDestination(role, adultDestination);

        if (ZNet.instance == null || ZDOMan.instance == null)
        {
            return false;
        }

        if (TryApplyPortalConfig(portalId, role, resolvedSpeciesKey, sanitizedName, resolvedDestination))
        {
            return true;
        }

        QueuePendingConfig(portalId, role, resolvedSpeciesKey, sanitizedName, resolvedDestination);
        return false;
    }

    public static void SetPortalConfig(
        ZDOID portalId,
        PortalRole role,
        SpeciesType species,
        string name,
        AdultDestination adultDestination)
    {
        string speciesKey = species == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(species);
        SetPortalConfig(portalId, role, speciesKey, name, adultDestination);
    }

    public static void ProcessPendingConfigs()
    {
        if (ZNet.instance == null || ZDOMan.instance == null || PendingConfigs.Count == 0)
        {
            return;
        }

        float now = Time.time;
        for (int i = PendingConfigs.Count - 1; i >= 0; i--)
        {
            PendingPortalConfig pending = PendingConfigs[i];
            if (now > pending.ExpireTime)
            {
                PendingConfigs.RemoveAt(i);
                continue;
            }

            if (TryApplyPortalConfig(
                    pending.PortalId,
                    pending.Role,
                    pending.SpeciesKey,
                    pending.Name,
                    pending.AdultDestination))
            {
                PendingConfigs.RemoveAt(i);
            }
        }
    }

    private static bool TryApplyPortalConfig(
        ZDOID portalId,
        PortalRole role,
        string speciesKey,
        string name,
        AdultDestination adultDestination)
    {
        OPTeleportWorld portal = FindPortal(portalId);
        ZNetView nview = portal != null ? portal.GetComponent<ZNetView>() : null;
        if (nview == null || !nview.IsValid())
        {
            ZDO zdoOnly = ZDOMan.instance?.GetZDO(portalId);
            if (zdoOnly == null)
            {
                return false;
            }

            nview = ZNetScene.instance?.FindInstance(zdoOnly);
        }

        if (nview == null || !nview.IsValid())
        {
            return false;
        }

        if (!TryClaimPortalOwnership(nview, out string claimError))
        {
            Player local = Player.m_localPlayer;
            if (local != null && !string.IsNullOrEmpty(claimError))
            {
                local.Message(MessageHud.MessageType.Center, claimError);
            }

            return false;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        string resolvedSpeciesKey = PortalRoleCatalog.ResolveSpeciesKey(role, speciesKey);
        AdultDestination resolvedDestination = PortalRoleCatalog.ResolveAdultDestination(role, adultDestination);

        zdo.Set(ZdoFields.PortalRole, PortalRoleCatalog.ToStorageValue(role));
        zdo.Set(ZdoFields.DeclaredSpecies, resolvedSpeciesKey ?? string.Empty);
        zdo.Set(ZdoFields.PortalName, name);
        zdo.Set(ZdoFields.AdultDestination, PortalRoleCatalog.ToStorageValue(resolvedDestination));
        zdo.Set(ZdoFields.ForwardAdults, resolvedDestination == AdultDestination.Cull);

        PortalHelper.SyncRegistryFromZdo(zdo);
        BreedableSpeciesRegistry.RefreshFromAllBreeders();
        DestinationRegistry.RefreshCapWarnings();

        if (ZNet.instance.IsServer())
        {
            PortalRegistrySync.BroadcastFromServer();
        }
        else
        {
            PortalRegistrySync.RequestServerRebuildAndBroadcast();
        }

        Player player = Player.m_localPlayer;
        if (player != null)
        {
            string portalName = string.IsNullOrEmpty(name) ? PortalDisplayHelper.UnnamedDisplay : name;
            string config = PortalRoleCatalog.GetConfiguredMessage(role, resolvedSpeciesKey, resolvedDestination);
            player.Message(
                MessageHud.MessageType.TopLeft,
                $"Portal \"{portalName}\" configured as {config}.");
        }

        return true;
    }

    private static bool TryClaimPortalOwnership(ZNetView nview, out string error)
    {
        error = string.Empty;
        if (nview == null || !nview.IsValid())
        {
            error = "Portal is not ready yet. Try again in a moment.";
            return false;
        }

        if (!nview.IsOwner())
        {
            nview.ClaimOwnership();
        }

        if (!nview.IsOwner())
        {
            error = "Could not take ownership of this portal. Try again in a moment.";
            return false;
        }

        return true;
    }

    private static void QueuePendingConfig(
        ZDOID portalId,
        PortalRole role,
        string speciesKey,
        string name,
        AdultDestination adultDestination)
    {
        for (int i = PendingConfigs.Count - 1; i >= 0; i--)
        {
            if (PendingConfigs[i].PortalId == portalId)
            {
                PendingConfigs.RemoveAt(i);
            }
        }

        PendingConfigs.Add(new PendingPortalConfig
        {
            PortalId = portalId,
            Role = role,
            SpeciesKey = speciesKey ?? string.Empty,
            Name = name ?? string.Empty,
            AdultDestination = adultDestination,
            ExpireTime = Time.time + ConfigRetrySeconds
        });
    }

    private static OPTeleportWorld FindPortal(ZDOID portalId)
    {
        if (ZDOMan.instance == null || ZNetScene.instance == null)
        {
            return null;
        }

        ZDO zdo = ZDOMan.instance.GetZDO(portalId);
        if (zdo == null)
        {
            return null;
        }

        ZNetView nview = ZNetScene.instance.FindInstance(zdo);
        return nview != null ? nview.GetComponent<OPTeleportWorld>() : null;
    }
}
