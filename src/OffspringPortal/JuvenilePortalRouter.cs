using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class JuvenilePortalRouter
{
    public static bool IsBreederPortal(OPTeleportWorld portal)
    {
        if (portal == null)
        {
            return false;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        PortalRecord record = DestinationRegistry.Get(zdo.m_uid);
        if (record != null)
        {
            return record.Role == PortalRole.Breeder;
        }

        return PortalRoleCatalog.FromZdo(zdo) == PortalRole.Breeder;
    }

    public static bool TryRoute(
        OPTeleportWorld portal,
        Character character,
        Dictionary<int, float> cooldowns,
        ref float lastNoDestinationMessageTime)
    {
        if (portal == null || character == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return false;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(character))
        {
            return false;
        }

        int bodyId = character.GetInstanceID();
        float now = Time.time;
        if (cooldowns.TryGetValue(bodyId, out float nextAllowed) && now < nextAllowed)
        {
            return false;
        }

        ZDO zdo = character.GetZdo();
        if (zdo != null && zdo.GetBool(ZdoFields.Transported) && !ModConfig.AllowRetransport.Value)
        {
            return false;
        }

        if (!IsBreederPortal(portal))
        {
            return false;
        }

        string speciesKey = SpeciesHelper.GetJuvenileSpeciesKey(character);
        if (string.IsNullOrEmpty(speciesKey))
        {
            return false;
        }

        ZDOID characterId = character.GetNview()?.GetZDO()?.m_uid ?? ZDOID.None;
        ZDOID sourcePortalId = portal.GetComponent<ZNetView>()?.GetZDO()?.m_uid ?? ZDOID.None;
        if (characterId == ZDOID.None || sourcePortalId == ZDOID.None)
        {
            return false;
        }

        if (ZNet.instance.IsServer())
        {
            DiagnosticLog.Verbose(
                $"Juvenile scan on server: {SpeciesKey.GetDisplayName(speciesKey)} at breeder {sourcePortalId}.");
            return TryRouteOnServer(
                portal,
                character,
                speciesKey,
                characterId,
                sourcePortalId,
                cooldowns,
                ref lastNoDestinationMessageTime,
                now,
                bodyId);
        }

        if (!RoutingRpc.CanSendRequests())
        {
            DiagnosticLog.Verbose("Juvenile route skipped on client: routing RPC unavailable.");
            return false;
        }

        DiagnosticLog.Verbose(
            $"Juvenile route client request: {SpeciesKey.GetDisplayName(speciesKey)} creature={characterId}, breeder={sourcePortalId}.");
        RoutingRpc.RequestJuvenileRoute(characterId, sourcePortalId, speciesKey);
        cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;
        return false;
    }

    public static bool ExecuteApprovedRoute(
        ZDOID characterId,
        ZDOID sourcePortalId,
        PortalRecord destination,
        Character character = null,
        OPTeleportWorld sourcePortal = null)
    {
        character ??= ResolveCharacter(characterId);
        sourcePortal ??= ResolvePortal(sourcePortalId);
        if (destination == null)
        {
            return false;
        }

        if (character != null && sourcePortal != null)
        {
            if (!SpeciesHelper.IsEligibleJuvenile(character) || !IsBreederPortal(sourcePortal))
            {
                return false;
            }

            string speciesKey = SpeciesHelper.GetJuvenileSpeciesKey(character);
            Vector3 targetPos = JuvenileTeleporter.ResolveDestinationPosition(destination, sourcePortal);
            bool allowStoredHeight = ZNetScene.instance == null || !ZNetScene.instance.IsAreaReady(targetPos);
            if (JuvenileTeleporter.TryExecuteApproved(character, destination, sourcePortal, allowStoredHeight))
            {
                DiagnosticLog.Info(
                    $"Teleported {SpeciesKey.GetDisplayName(speciesKey)} juvenile to maturing portal at {destination.Position}.");
                PlayPortalActivation(sourcePortal);
                return true;
            }

            DiagnosticLog.Verbose(
                $"Juvenile teleport TryExecuteApproved failed for {characterId}; areaReady={ZNetScene.instance?.IsAreaReady(targetPos) ?? false}.");
        }

        if (ZNet.instance != null
            && ZNet.instance.IsServer()
            && JuvenileTeleporter.TryMoveZdo(characterId, destination, sourcePortalId))
        {
            DiagnosticLog.Info(
                $"Teleported juvenile via ZDO to maturing portal at {destination.Position}.");
            PlayPortalActivation(sourcePortal);
            return true;
        }

        DiagnosticLog.Verbose(
            $"ExecuteApprovedRoute failed for creature {characterId}: character={(character != null)}, source={(sourcePortal != null)}, destination={destination.Id}.");
        return false;
    }

    private static bool TryRouteOnServer(
        OPTeleportWorld portal,
        Character character,
        string speciesKey,
        ZDOID characterId,
        ZDOID sourcePortalId,
        Dictionary<int, float> cooldowns,
        ref float lastNoDestinationMessageTime,
        float now,
        int bodyId)
    {
        if (!DestinationRegistry.TryResolveMaturingDestination(speciesKey, out PortalRecord destination))
        {
            if (DiagnosticLog.ShouldLogRateLimited(ref lastNoDestinationMessageTime, 5f))
            {
                string displayName = SpeciesKey.GetDisplayName(speciesKey);
                DestinationRegistry.LogMaturingResolutionFailure(speciesKey, "breeder scan");
                Player local = Player.m_localPlayer;
                if (local != null)
                {
                    local.Message(MessageHud.MessageType.Center,
                        $"No maturing portal registered for {displayName}.");
                }
            }

            return false;
        }

        if (ExecuteApprovedRoute(characterId, sourcePortalId, destination, character, portal))
        {
            cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;
            return true;
        }

        DiagnosticLog.Warning(
            $"Failed to teleport {SpeciesKey.GetDisplayName(speciesKey)} juvenile from breeder {sourcePortalId} to maturing {destination.Id}.");
        return false;
    }

    private static Character ResolveCharacter(ZDOID characterId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(characterId);
        return instance != null ? instance.GetComponent<Character>() : null;
    }

    private static OPTeleportWorld ResolvePortal(ZDOID portalId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(portalId);
        return instance != null ? instance.GetComponent<OPTeleportWorld>() : null;
    }

    private static void PlayPortalActivation(OPTeleportWorld portal)
    {
        portal?.PlayActivationEffect();
    }
}
