using UnityEngine;

namespace OffspringPortal;

public static class JuvenileFollowRpc
{
    private const string RpcToggleFollow = "OffspringPortal_ToggleJuvenileFollow";

    public static void Register()
    {
        ZRoutedRpc.instance.Register<ZDOID, ZDOID, bool>(RpcToggleFollow, OnToggleFollowRpc);
    }

    public static void RequestToggle(ZDOID creatureId, ZDOID playerId, bool showMessage)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(RpcToggleFollow, creatureId, playerId, showMessage);
    }

    private static void OnToggleFollowRpc(long sender, ZDOID creatureId, ZDOID playerId, bool showMessage)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        ApplyToggle(creatureId, playerId, showMessage, sender);
    }

    private static void ApplyToggle(ZDOID creatureId, ZDOID playerId, bool showMessage, long sender)
    {
        Character creature = ResolveCreature(creatureId);
        Player player = ResolveRequestingPlayer(sender, playerId);
        if (creature == null || player == null)
        {
            return;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(creature) || !creature.IsTamed())
        {
            return;
        }

        JuvenileGroundSnapper.EnsureAttached(creature);
        JuvenileFollowController.EnsureAttached(creature);
        JuvenileFollow.ApplyFollowToggle(creature, player, showMessage);
    }

    private static Character ResolveCreature(ZDOID creatureId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(creatureId);
        return instance != null ? instance.GetComponent<Character>() : null;
    }

    private static Player ResolveRequestingPlayer(long sender, ZDOID playerId)
    {
        Player senderPlayer = Player.GetPlayer(sender);
        if (senderPlayer != null)
        {
            return senderPlayer;
        }

        GameObject instance = ZNetScene.instance?.FindInstance(playerId);
        return instance != null ? instance.GetComponent<Player>() : null;
    }
}
