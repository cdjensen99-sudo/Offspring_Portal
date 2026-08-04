using UnityEngine;

namespace OffspringPortal;

public static class JuvenileFollow
{
    public static bool TryCommand(Character character, Player player, bool showMessage)
    {
        if (character == null || player == null)
        {
            return false;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(character) || !character.IsTamed())
        {
            return false;
        }

        ZNetView nview = character.GetNview();
        if (nview == null || !nview.IsValid())
        {
            return false;
        }

        JuvenileGroundSnapper.EnsureAttached(character);
        JuvenileFollowController.EnsureAttached(character);

        if (ZNet.instance.IsServer())
        {
            return ApplyFollowToggle(character, player, showMessage);
        }

        JuvenileFollowRpc.RequestToggle(nview.GetZDO().m_uid, player.GetZDOID(), showMessage);
        return true;
    }

    public static bool ApplyFollowToggle(Character character, Player player, bool showMessage)
    {
        if (character == null || player == null)
        {
            return false;
        }

        Tameable tameable = character.GetComponent<Tameable>();
        MonsterAI monsterAi = character.GetComponent<MonsterAI>();
        if (tameable != null && monsterAi != null)
        {
            tameable.m_commandable = true;
            ApplyMonsterFollowToggle(tameable, monsterAi, player, showMessage);
            return true;
        }

        if (character.GetComponent<AnimalAI>() != null)
        {
            return ApplyAnimalFollowToggle(character, player, showMessage);
        }

        return false;
    }

    public static bool IsFollowing(Character character)
    {
        if (character == null)
        {
            return false;
        }

        MonsterAI monsterAi = character.GetComponent<MonsterAI>();
        if (monsterAi != null && monsterAi.GetFollowTarget() != null)
        {
            return true;
        }

        ZDO zdo = character.GetZdo();
        return zdo != null && zdo.GetZDOID(ZdoFields.FollowTarget) != ZDOID.None;
    }

    private static void ApplyMonsterFollowToggle(
        Tameable tameable,
        MonsterAI monsterAi,
        Player player,
        bool showMessage)
    {
        ZNetView nview = tameable.GetComponent<ZNetView>();
        if (nview != null && !nview.IsOwner())
        {
            nview.ClaimOwnership();
        }

        if (monsterAi.GetFollowTarget() != null)
        {
            monsterAi.SetFollowTarget(null);
            monsterAi.SetPatrolPoint();
            if (nview != null && nview.IsOwner())
            {
                nview.GetZDO()?.Set(ZDOVars.s_follow, string.Empty);
            }

            if (showMessage)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    tameable.GetHoverName() + " " + Localization.instance.Localize("$hud_tamestay"));
            }

            return;
        }

        monsterAi.ResetPatrolPoint();
        monsterAi.SetFollowTarget(player.gameObject);
        if (nview != null && nview.IsOwner())
        {
            nview.GetZDO()?.Set(ZDOVars.s_follow, player.GetPlayerName());
        }

        if (showMessage)
        {
            player.Message(
                MessageHud.MessageType.Center,
                tameable.GetHoverName() + " " + Localization.instance.Localize("$hud_tamefollow"));
        }
    }

    public static bool ApplyAnimalFollowToggle(Character character, Player player, bool showMessage)
    {
        ZNetView nview = character.GetNview();
        if (nview == null || !nview.IsValid())
        {
            return false;
        }

        if (!nview.IsOwner())
        {
            nview.ClaimOwnership();
        }

        ZDO zdo = character.GetZdo();
        if (zdo == null)
        {
            return false;
        }

        ZDOID playerId = player.GetZDOID();
        ZDOID currentFollow = zdo.GetZDOID(ZdoFields.FollowTarget);
        string displayName = character.GetHoverName();

        if (currentFollow == playerId)
        {
            zdo.Set(ZdoFields.FollowTarget, ZDOID.None);
            if (showMessage)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    displayName + " " + Localization.instance.Localize("$hud_tamestay"));
            }
        }
        else
        {
            zdo.Set(ZdoFields.FollowTarget, playerId);
            if (showMessage)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    displayName + " " + Localization.instance.Localize("$hud_tamefollow"));
            }
        }

        return true;
    }
}
