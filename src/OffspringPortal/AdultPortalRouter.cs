using System.Collections.Generic;

using UnityEngine;



namespace OffspringPortal;



public static class AdultPortalRouter

{

    public static bool IsAdultRoutingMaturingPortal(OPTeleportWorld portal)

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

            return record.Role == PortalRole.Maturing && record.AdultDestination != AdultDestination.None;

        }



        return PortalRoleCatalog.FromZdo(zdo) == PortalRole.Maturing

            && PortalRoleCatalog.GetAdultDestination(zdo) != AdultDestination.None;

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



        if (!SpeciesHelper.IsEligibleAdult(character))

        {

            return false;

        }



        ZNetView portalView = portal.GetComponent<ZNetView>();

        PortalRecord source = portalView != null ? DestinationRegistry.Get(portalView.GetZDO()?.m_uid ?? ZDOID.None) : null;

        if (source == null || source.Role != PortalRole.Maturing || source.AdultDestination == AdultDestination.None)

        {

            return false;

        }



        int bodyId = character.GetInstanceID();

        float now = Time.time;

        if (cooldowns.TryGetValue(bodyId, out float nextAllowed) && now < nextAllowed)

        {

            return false;

        }



        string speciesKey = SpeciesHelper.GetAdultSpeciesKey(character);

        if (!SpeciesHelper.SpeciesKeyMatches(speciesKey, source.DeclaredSpeciesKey))

        {

            return false;

        }



        ZDOID characterId = character.GetNview()?.GetZDO()?.m_uid ?? ZDOID.None;

        ZDOID sourcePortalId = portalView?.GetZDO()?.m_uid ?? ZDOID.None;

        if (characterId == ZDOID.None || sourcePortalId == ZDOID.None)

        {

            return false;

        }



        if (ZNet.instance.IsServer())

        {

            return TryRouteOnServer(

                portal,

                character,

                speciesKey,

                source,

                characterId,

                sourcePortalId,

                cooldowns,

                ref lastNoDestinationMessageTime,

                now,

                bodyId);

        }



        if (!RoutingRpc.CanSendRequests())

        {

            return false;

        }



        RoutingRpc.RequestAdultRoute(characterId, sourcePortalId, speciesKey);

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

        if (character == null || sourcePortal == null || destination == null)

        {

            return false;

        }



        PortalRecord source = DestinationRegistry.Get(sourcePortalId);

        if (source == null || source.Role != PortalRole.Maturing)

        {

            return false;

        }



        string speciesKey = SpeciesHelper.GetAdultSpeciesKey(character);

        if (!SpeciesHelper.SpeciesKeyMatches(speciesKey, source.DeclaredSpeciesKey))

        {

            return false;

        }



        if (!JuvenileTeleporter.TryExecuteApproved(character, destination, sourcePortal))

        {

            return false;

        }



        OffspringPortalPlugin.Log.LogInfo(

            $"Teleported {SpeciesKey.GetDisplayName(speciesKey)} adult to {source.AdultDestination} portal at {destination.Position}.");

        PlayPortalActivation(sourcePortal);

        return true;

    }



    private static bool TryRouteOnServer(

        OPTeleportWorld portal,

        Character character,

        string speciesKey,

        PortalRecord source,

        ZDOID characterId,

        ZDOID sourcePortalId,

        Dictionary<int, float> cooldowns,

        ref float lastNoDestinationMessageTime,

        float now,

        int bodyId)

    {

        PortalRecord destination;

        string missingMessage;

        switch (source.AdultDestination)

        {

            case AdultDestination.Farm:

                if (DestinationRegistry.TryResolveFarmDestination(speciesKey, out destination))

                {

                    break;

                }



                missingMessage = "No Farm portal exists.";

                return WarnMissingDestination(now, ref lastNoDestinationMessageTime, missingMessage);

            case AdultDestination.Cull:

                if (DestinationRegistry.TryResolveCullDestination(out destination))

                {

                    break;

                }



                missingMessage = "No Cull portal exists.";

                return WarnMissingDestination(now, ref lastNoDestinationMessageTime, missingMessage);

            default:

                return false;

        }



        if (ExecuteApprovedRoute(characterId, sourcePortalId, destination, character, portal))

        {

            cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;

            return true;

        }



        OffspringPortalPlugin.Log.LogWarning($"Failed to teleport {SpeciesKey.GetDisplayName(speciesKey)} adult.");

        return false;

    }



    private static bool WarnMissingDestination(float now, ref float lastMessageTime, string message)

    {

        if (now - lastMessageTime <= 5f)

        {

            return false;

        }



        lastMessageTime = now;

        OffspringPortalPlugin.Log.LogWarning(message);

        Player local = Player.m_localPlayer;

        if (local != null)

        {

            local.Message(MessageHud.MessageType.Center, message);

        }



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

