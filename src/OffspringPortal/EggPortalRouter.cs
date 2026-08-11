using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class EggPortalRouter
{
    public static bool TryRoute(
        OPTeleportWorld portal,
        ItemDrop egg,
        Dictionary<int, float> cooldowns,
        ref float lastNoDestinationMessageTime)
    {
        if (portal == null || egg == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return false;
        }

        if (egg.GetComponent<EggGrow>() == null)
        {
            return false;
        }

        if (!ZNet.instance.IsServer())
        {
            return false;
        }

        if (!JuvenilePortalRouter.IsBreederPortal(portal))
        {
            return false;
        }

        ZNetView nview = egg.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo != null && zdo.GetBool(ZdoFields.Transported) && !ModConfig.AllowRetransport.Value)
        {
            return false;
        }

        if (!SpeciesDiscovery.TryGetEggSpeciesInfo(egg, out BreedableSpeciesInfo speciesInfo))
        {
            return false;
        }

        int bodyId = egg.GetInstanceID();
        float now = Time.time;
        if (cooldowns.TryGetValue(bodyId, out float nextAllowed) && now < nextAllowed)
        {
            return false;
        }

        PortalRecord destination;
        if (speciesInfo.IsDualPurposeEgg)
        {
            string eggKey = speciesInfo.EggCollectorKey;
            if (DestinationRegistry.TryResolveEggCollectorDestination(eggKey, out destination)
                || DestinationRegistry.TryResolveMaturingDestination(speciesInfo.Key, out destination))
            {
                if (!EggTeleporter.TryTeleport(egg, destination, portal))
                {
                    return false;
                }

                string destinationLabel = destination.Role == PortalRole.EggCollector
                    ? SpeciesKey.GetEggCollectorDisplayName(eggKey)
                    : "maturing portal";
                OffspringPortalPlugin.Log.LogInfo(
                    $"Teleported dual-purpose {speciesInfo.DisplayName} egg to {destinationLabel} at {destination.Position}.");
                cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;
                PlayPortalActivation(portal);
                return true;
            }

            if (now - lastNoDestinationMessageTime > 5f)
            {
                lastNoDestinationMessageTime = now;
                string eggLabel = SpeciesKey.GetEggCollectorDisplayName(eggKey);
                string message = $"No egg collector for {eggLabel} and no maturing fallback registered.";
                OffspringPortalPlugin.Log.LogWarning(message);
                Player local = Player.m_localPlayer;
                if (local != null)
                {
                    local.Message(MessageHud.MessageType.Center, message);
                }
            }

            return false;
        }

        if (!DestinationRegistry.TryResolveMaturingDestination(speciesInfo.Key, out destination))
        {
            if (now - lastNoDestinationMessageTime > 5f)
            {
                lastNoDestinationMessageTime = now;
                string message = $"No maturing portal registered for {speciesInfo.DisplayName} eggs.";
                OffspringPortalPlugin.Log.LogWarning(message);
                Player local = Player.m_localPlayer;
                if (local != null)
                {
                    local.Message(MessageHud.MessageType.Center, message);
                }
            }

            return false;
        }

        if (!EggTeleporter.TryTeleport(egg, destination, portal))
        {
            return false;
        }

        OffspringPortalPlugin.Log.LogInfo(
            $"Teleported {speciesInfo.DisplayName} egg to maturing portal at {destination.Position}.");
        cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;
        PlayPortalActivation(portal);
        return true;
    }

    private static void PlayPortalActivation(OPTeleportWorld portal)
    {
        portal?.PlayActivationEffect();
    }
}
