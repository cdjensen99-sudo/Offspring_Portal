using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public class OffspringPortalTrigger : MonoBehaviour
{
    private TeleportWorld portal;
    private readonly Dictionary<int, float> cooldowns = new Dictionary<int, float>();
    private float lastNoDestinationMessageTime;

    private void Awake()
    {
        portal = GetComponentInParent<TeleportWorld>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        if (GetCharacter(other) is Player player)
        {
            if (player == Player.m_localPlayer)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    "Offspring portals route juveniles only. Use a standard portal to travel.");
            }

            return;
        }

        TryRouteJuvenile(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (GetCharacter(other) is Player)
        {
            return;
        }

        TryRouteJuvenile(other);
    }

    private void TryRouteJuvenile(Collider other)
    {
        if (portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        Character character = GetCharacter(other);
        if (character == null)
        {
            return;
        }

        JuvenilePortalRouter.TryRoute(portal, character, cooldowns, ref lastNoDestinationMessageTime);
    }

    private static Character GetCharacter(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null)
        {
            return character;
        }

        if (other.attachedRigidbody != null)
        {
            character = other.attachedRigidbody.GetComponent<Character>();
            if (character != null)
            {
                return character;
            }
        }

        return other.GetComponentInParent<Character>();
    }
}
