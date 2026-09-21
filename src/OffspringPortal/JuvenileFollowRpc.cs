using System;
using UnityEngine;

namespace OffspringPortal;

public static class JuvenileFollowRpc
{
    private const string RpcToggleFollow = "OffspringPortal_ToggleJuvenileFollow";
    private const string RpcExecuteToggleFollow = "OffspringPortal_ExecuteJuvenileFollow";
    private static ZRoutedRpc registeredInstance;

    public static void Register()
    {
        ZRoutedRpc instance = ZRoutedRpc.instance;
        if (instance == null)
        {
            return;
        }

        if (ReferenceEquals(registeredInstance, instance))
        {
            return;
        }

        instance.Register<ZDOID, ZDOID, bool>(RpcToggleFollow, OnToggleFollowRpc);
        instance.Register<ZDOID, ZDOID, bool>(RpcExecuteToggleFollow, OnExecuteToggleFollowRpc);
        registeredInstance = instance;
        DiagnosticLog.Info("Juvenile follow RPC handlers registered on current ZRoutedRpc instance.");
    }

    public static void RequestToggle(ZDOID creatureId, ZDOID playerId, bool showMessage)
    {
        if (ZRoutedRpc.instance == null)
        {
            DiagnosticLog.Warning("Cannot request juvenile follow toggle: ZRoutedRpc.instance is null.");
            return;
        }

        try
        {
            Register();
            if (ZNet.instance == null)
            {
                DiagnosticLog.Warning("Cannot request juvenile follow toggle: ZNet.instance is null.");
                return;
            }

            if (ZNet.instance.IsServer())
            {
                DiagnosticLog.Verbose(
                    $"Local server handling juvenile follow request: creature={creatureId}, player={playerId}.");
                OnToggleFollowRpc(ZNet.GetUID(), creatureId, playerId, showMessage);
                return;
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer == null || serverPeer.m_uid == 0L)
            {
                DiagnosticLog.Warning("Cannot send juvenile follow RPC: server peer unavailable.");
                return;
            }

            DiagnosticLog.Verbose(
                $"Sending juvenile follow RPC to server peer {serverPeer.m_uid}: creature={creatureId}, player={playerId}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(serverPeer.m_uid, RpcToggleFollow, creatureId, playerId, showMessage);
        }
        catch (Exception ex)
        {
            OPLog.Exception("Failed to send juvenile follow RPC", ex);
        }
    }

    private static void OnToggleFollowRpc(long sender, ZDOID creatureId, ZDOID playerId, bool showMessage)
    {
        try
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                DiagnosticLog.Warning($"Follow RPC reached a non-server peer unexpectedly. sender={sender}, creature={creatureId}");
                return;
            }

            ZDO creatureZdo = ZDOMan.instance?.GetZDO(creatureId);
            long owner = creatureZdo?.GetOwner() ?? 0L;
            DiagnosticLog.Verbose(
                $"Server received follow request: sender={sender}, creature={creatureId}, owner={owner}, player={playerId}.");

            if (owner != 0L)
            {
                DiagnosticLog.Verbose($"Routing follow execution to ZDO owner {owner}.");
                ZRoutedRpc.instance.InvokeRoutedRPC(owner, RpcExecuteToggleFollow, creatureId, playerId, showMessage);
                return;
            }

            DiagnosticLog.Verbose("Follow target has no remote owner; applying on server.");
            ApplyToggle(creatureId, playerId, showMessage, sender);
        }
        catch (Exception ex)
        {
            OPLog.Exception($"Server failed processing juvenile follow request from {sender}", ex);
        }
    }

    private static void OnExecuteToggleFollowRpc(long sender, ZDOID creatureId, ZDOID playerId, bool showMessage)
    {
        try
        {
            DiagnosticLog.Verbose($"Executing juvenile follow RPC: sender={sender}, creature={creatureId}, player={playerId}.");
            ApplyToggle(creatureId, playerId, showMessage, sender);
        }
        catch (Exception ex)
        {
            OPLog.Exception($"Failed executing juvenile follow RPC for creature {creatureId}", ex);
        }
    }

    private static void ApplyToggle(ZDOID creatureId, ZDOID playerId, bool showMessage, long sender)
    {
        Character creature = ResolveCreature(creatureId);
        Player player = ResolveRequestingPlayer(sender, playerId);

        if (creature == null || player == null)
        {
            DiagnosticLog.Warning(
                $"Could not resolve follow RPC targets. creature={creatureId} resolved={creature != null}, player={playerId} resolved={player != null}, sender={sender}.");
            return;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(creature) || !creature.IsTamed())
        {
            DiagnosticLog.Verbose(
                $"Ignoring follow RPC for '{creature.GetHoverName()}': not an eligible tamed juvenile.");
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
