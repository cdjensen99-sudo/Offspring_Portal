using BepInEx.Bootstrap;
using UnityEngine;

namespace OffspringPortal;

public static class JuvenileFollow
{
    internal const string LetsGoPluginGuid = "hardwire99.letsgo";

    public static bool TryCommand(Character character, Player player, bool showMessage)
    {
        if (character == null || player == null)
        {
            return false;
        }

        if (!ModConfig.EnableFollowCommand.Value)
        {
            DiagnosticLog.Verbose("Follow command ignored: EnableFollowCommand is off.");
            return false;
        }

        if (IsLetsGoLoaded())
        {
            DiagnosticLog.Verbose("Follow command ignored: LetsGo mod is loaded.");
            return false;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(character) || !character.IsTamed())
        {
            DiagnosticLog.Verbose(
                $"Follow command ignored for {character.GetHoverName()}: eligible={SpeciesHelper.IsEligibleJuvenile(character)}, tamed={character.IsTamed()}.");
            return false;
        }

        ZNetView nview = character.GetNview();
        if (nview == null || !nview.IsValid())
        {
            DiagnosticLog.Verbose($"Follow command ignored for {character.GetHoverName()}: invalid ZNetView.");
            return false;
        }

        JuvenileGroundSnapper.EnsureAttached(character);
        JuvenileFollowController.EnsureAttached(character);

        if (ZNet.instance.IsServer())
        {
            DiagnosticLog.Verbose($"Follow command handled locally on server for {character.GetHoverName()}.");
            return ApplyFollowToggle(character, player, showMessage);
        }

        DiagnosticLog.Verbose(
            $"Follow command sending RPC for {character.GetHoverName()} (owner={nview.GetZDO()?.GetOwner() ?? 0L}).");
        JuvenileFollowRpc.RequestToggle(nview.GetZDO().m_uid, player.GetZDOID(), showMessage);
        return true;
    }

    public static bool ApplyFollowToggle(Character character, Player player, bool showMessage)
    {
        if (character == null || player == null)
        {
            return false;
        }

        if (character.GetComponent<AnimalAI>() != null)
        {
            return ApplyAnimalFollowToggle(character, player, showMessage);
        }

        MonsterAI monsterAi = character.GetComponent<MonsterAI>();
        if (monsterAi != null)
        {
            Tameable tameable = character.GetComponent<Tameable>();
            if (tameable != null)
            {
                tameable.m_commandable = true;
            }

            ApplyMonsterFollowToggle(character, monsterAi, player, showMessage);
            return true;
        }

        return false;
    }

    internal static bool IsLetsGoLoaded()
    {
        return Chainloader.PluginInfos.ContainsKey(LetsGoPluginGuid);
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
        Character character,
        MonsterAI monsterAi,
        Player player,
        bool showMessage)
    {
        ZNetView nview = character.GetComponent<ZNetView>();
        if (nview != null && !nview.IsOwner())
        {
            nview.ClaimOwnership();
        }

        string displayName = character.GetHoverName();

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
                    displayName + " " + Localization.instance.Localize("$hud_tamestay"));
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
                displayName + " " + Localization.instance.Localize("$hud_tamefollow"));
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
