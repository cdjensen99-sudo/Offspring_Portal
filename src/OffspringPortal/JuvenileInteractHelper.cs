using System.Linq;
using UnityEngine;

namespace OffspringPortal;

public static class JuvenileInteractHelper
{
    private static ZDOID lastLoggedResolvedJuvenileId;
    private static bool hasLastLoggedResolvedJuvenileId;

    private static void LogResolvedJuvenile(Character juvenile, string source, float? distance = null)
    {
        if (juvenile == null)
        {
            return;
        }

        ZDO zdo = juvenile.GetNview()?.GetZDO();
        if (zdo == null)
        {
            return;
        }

        if (hasLastLoggedResolvedJuvenileId && lastLoggedResolvedJuvenileId == zdo.m_uid)
        {
            return;
        }

        hasLastLoggedResolvedJuvenileId = true;
        lastLoggedResolvedJuvenileId = zdo.m_uid;

        if (distance.HasValue)
        {
            DiagnosticLog.Verbose(
                $"Crosshair search resolved '{juvenile.GetHoverName()}' from {source} at {distance.Value:F2}m (id={zdo.m_uid}).");
        }
        else
        {
            DiagnosticLog.Verbose(
                $"Crosshair search resolved '{juvenile.GetHoverName()}' from {source} (id={zdo.m_uid}).");
        }
    }

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

        Character hovered = player.GetHoverCreature();
        if (IsValidCandidate(hovered, player, juvenilesOnly))
        {
            float distance = Vector3.Distance(player.m_eye.position, hovered.transform.position);
            if (distance <= maxDistance + 0.5f)
            {
                LogResolvedJuvenile(hovered, "Player.GetHoverCreature()");
                return hovered;
            }
        }

        Vector3 origin = GameCamera.instance.transform.position;
        Vector3 direction = GameCamera.instance.transform.forward;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            Mathf.Max(0.1f, maxDistance),
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        Character best = null;
        float bestDistance = maxDistance;

        foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
        {
            if (hit.collider == null)
            {
                continue;
            }

            Character character = GetCharacterFromCollider(hit.collider);
            if (!IsValidCandidate(character, player, juvenilesOnly))
            {
                continue;
            }

            float distance = Vector3.Distance(player.m_eye.position, hit.point);
            if (distance <= bestDistance)
            {
                best = character;
                bestDistance = distance;
            }
        }

        if (best != null)
        {
            LogResolvedJuvenile(best, "raycast", bestDistance);
        }

        return best;
    }

    private static Character GetCharacterFromCollider(Collider collider)
    {
        if (collider == null)
        {
            return null;
        }

        Character character = collider.GetComponentInParent<Character>();
        if (character != null)
        {
            return character;
        }

        if (collider.attachedRigidbody != null)
        {
            character = collider.attachedRigidbody.GetComponentInParent<Character>();
        }

        return character;
    }

    private static bool IsValidCandidate(Character character, Player player, bool juvenilesOnly)
    {
        if (character == null || character == player || character.IsPlayer() || !character.IsTamed())
        {
            return false;
        }

        if (juvenilesOnly)
        {
            return SpeciesHelper.IsEligibleJuvenile(character);
        }

        return character.GetComponent<Tameable>() != null;
    }
}
