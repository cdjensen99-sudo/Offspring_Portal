using UnityEngine;

namespace OffspringPortal;

public sealed class JuvenileFollowController : MonoBehaviour
{
    public static bool TryApplyAnimalFollow(AnimalAI animalAi, float dt)
    {
        if (!ModConfig.EnableFollowCommand.Value || animalAi == null)
        {
            return false;
        }

        Character character = animalAi.GetComponent<Character>();
        ZNetView nview = animalAi.GetComponent<ZNetView>();
        if (character == null || nview == null || !nview.IsValid())
        {
            return false;
        }

        if (!ZNet.instance.IsServer() || !nview.IsOwner())
        {
            return false;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(character) || !character.IsTamed())
        {
            return false;
        }

        ZDO zdo = character.GetZdo();
        if (zdo == null)
        {
            return false;
        }

        ZDOID followId = zdo.GetZDOID(ZdoFields.FollowTarget);
        if (followId == ZDOID.None)
        {
            return false;
        }

        Player target = ResolvePlayer(followId);
        if (target == null)
        {
            return false;
        }

        Vector3 offset = target.transform.position - character.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance < 2f)
        {
            animalAi.StopMoving();
            return true;
        }

        animalAi.MoveTowards(offset / distance, distance > 10f);
        return true;
    }

    public static void EnsureAttached(Character character)
    {
        if (character == null || character.GetComponent<AnimalAI>() == null)
        {
            return;
        }

        if (character.GetComponent<JuvenileFollowController>() == null)
        {
            character.gameObject.AddComponent<JuvenileFollowController>();
        }
    }

    private static Player ResolvePlayer(ZDOID followId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(followId);
        Player player = instance != null ? instance.GetComponent<Player>() : null;
        if (player != null)
        {
            return player;
        }

        foreach (Player onlinePlayer in Player.GetAllPlayers())
        {
            ZNetView playerView = onlinePlayer?.GetNview();
            if (playerView != null && playerView.GetZDO()?.m_uid == followId)
            {
                return onlinePlayer;
            }
        }

        return null;
    }
}
