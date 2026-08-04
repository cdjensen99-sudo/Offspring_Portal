using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class JuvenilePortalRouter
{
    public static bool IsBreederPortal(TeleportWorld portal)
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
        TeleportWorld portal,
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

        if (!ZNet.instance.IsServer())
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

        if (!DestinationRegistry.TryResolveMaturingDestination(speciesKey, out PortalRecord destination))
        {
            if (now - lastNoDestinationMessageTime > 5f)
            {
                lastNoDestinationMessageTime = now;
                string displayName = SpeciesKey.GetDisplayName(speciesKey);
                OffspringPortalPlugin.Log.LogWarning(
                    $"No maturing portal registered for {displayName}.");
                Player local = Player.m_localPlayer;
                if (local != null)
                {
                    local.Message(MessageHud.MessageType.Center,
                        $"No maturing portal registered for {displayName}.");
                }
            }

            return false;
        }

        if (!JuvenileTeleporter.TryTeleport(character, destination, portal))
        {
            OffspringPortalPlugin.Log.LogWarning(
                $"Failed to teleport {SpeciesKey.GetDisplayName(speciesKey)} juvenile.");
            return false;
        }

        OffspringPortalPlugin.Log.LogInfo(
            $"Teleported {SpeciesKey.GetDisplayName(speciesKey)} juvenile to maturing portal at {destination.Position}.");
        cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;
        PlayPortalActivation(portal);
        return true;
    }

    private static void PlayPortalActivation(TeleportWorld portal)
    {
        if (portal?.m_connected == null)
        {
            return;
        }

        portal.m_connected.Create(portal.transform.position, portal.transform.rotation, portal.transform);
    }
}
