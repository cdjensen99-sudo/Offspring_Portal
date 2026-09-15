using UnityEngine;

namespace OffspringPortal;

public static class JuvenileFollowRpc
{
    private const string RpcToggleFollow = "OffspringPortal_ToggleJuvenileFollow";
    private const string RpcExecuteToggleFollow = "OffspringPortal_ExecuteJuvenileFollow";
    private static bool registered;

    public static void Register()
    {
        if (registered || ZRoutedRpc.instance == null)
        {
            return;
        }

        ZRoutedRpc instance = ZRoutedRpc.instance;
        instance.Register<ZDOID, ZDOID, bool>(RpcToggleFollow, OnToggleFollowRpc);
        instance.Register<ZDOID, ZDOID, bool>(RpcExecuteToggleFollow, OnExecuteToggleFollowRpc);
        registered = true;
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

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcExecuteToggleFollow, creatureId, playerId, showMessage);
    }

    private static void OnExecuteToggleFollowRpc(long sender, ZDOID creatureId, ZDOID playerId, bool showMessage)
    {
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
