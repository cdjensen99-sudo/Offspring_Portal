using System.Linq;
using UnityEngine;

namespace OffspringPortal;

public static class JuvenileInteractHelper
{
    private static readonly int InteractMask = LayerMask.GetMask(
        "item", "piece", "piece_nonsolid", "Default", "static_solid", "Default_small",
        "character", "character_net", "terrain", "vehicle");

    public static Character FindJuvenileUnderCrosshair(Player player, float maxDistance = 6f)
    {
        return FindTamedCharacterUnderCrosshair(player, maxDistance, juvenilesOnly: true);
    }

    public static Character FindTamedCharacterUnderCrosshair(
        Player player,
        float maxDistance,
        bool juvenilesOnly)
    {
        if (player == null || GameCamera.instance == null)
        {
            return null;
        }

        Vector3 origin = GameCamera.instance.transform.position;
        Vector3 direction = GameCamera.instance.transform.forward;
        Vector3 eye = player.m_eye.position;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, 50f, InteractMask);
        Character best = null;
        float bestDistance = maxDistance;

        foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.attachedRigidbody != null
                && hit.collider.attachedRigidbody.gameObject == player.gameObject)
            {
                continue;
            }

            Character character = hit.collider.attachedRigidbody != null
                ? hit.collider.attachedRigidbody.GetComponent<Character>()
                : hit.collider.GetComponentInParent<Character>();

            if (character == null
                || character.IsPlayer()
                || !character.IsTamed())
            {
                continue;
            }

            if (juvenilesOnly && !SpeciesHelper.IsEligibleJuvenile(character))
            {
                continue;
            }

            if (!juvenilesOnly && character.GetComponent<Tameable>() == null)
            {
                continue;
            }

            float distance = Vector3.Distance(eye, hit.point);
            if (distance <= bestDistance)
            {
                best = character;
                bestDistance = distance;
            }
        }

        return best;
    }
}
