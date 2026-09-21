using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class RoutingRpc
{
    private const string RpcRequestJuvenileRoute = "OffspringPortal_RequestJuvenileRoute";
    private const string RpcExecuteJuvenileRoute = "OffspringPortal_ExecuteJuvenileRoute";
    private const string RpcApplyJuvenileLiveTransfer = "OffspringPortal_ApplyJuvenileLiveTransfer";
    private const string RpcRequestEggRoute = "OffspringPortal_RequestEggRoute";
    private const string RpcExecuteEggRoute = "OffspringPortal_ExecuteEggRoute";
    private const string RpcRequestAdultRoute = "OffspringPortal_RequestAdultRoute";
    private const string RpcExecuteAdultRoute = "OffspringPortal_ExecuteAdultRoute";

    private static readonly Dictionary<ZDOID, float> ServerCooldowns = new Dictionary<ZDOID, float>();
    private static ZRoutedRpc registeredInstance;

    public static bool CanSendRequests()
    {
        return ZRoutedRpc.instance != null && ZNet.instance != null;
    }

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

        instance.Register<ZDOID, ZDOID, string>(RpcRequestJuvenileRoute, OnRequestJuvenileRoute);
        instance.Register<ZDOID, ZDOID, ZDOID, Vector3, float>(RpcExecuteJuvenileRoute, OnExecuteJuvenileRoute);
        instance.Register<ZDOID, Vector3, float>(RpcApplyJuvenileLiveTransfer, OnApplyJuvenileLiveTransfer);
        instance.Register<ZDOID, ZDOID, string, bool, string>(RpcRequestEggRoute, OnRequestEggRoute);
        instance.Register<ZDOID, ZDOID, ZDOID>(RpcExecuteEggRoute, OnExecuteEggRoute);
        instance.Register<ZDOID, ZDOID, string>(RpcRequestAdultRoute, OnRequestAdultRoute);
        instance.Register<ZDOID, ZDOID, ZDOID>(RpcExecuteAdultRoute, OnExecuteAdultRoute);
        registeredInstance = instance;
        DiagnosticLog.Info("Routing RPC handlers registered on current ZRoutedRpc instance.");
    }

    public static void RequestJuvenileRoute(ZDOID characterId, ZDOID sourcePortalId, string speciesKey)
    {
        if (!CanSendRequests())
        {
            return;
        }

        Register();

        if (ZNet.instance.IsServer())
        {
            OnRequestJuvenileRoute(0L, characterId, sourcePortalId, speciesKey ?? string.Empty);
            return;
        }

        ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
        if (serverPeer == null || serverPeer.m_uid == 0L)
        {
            DiagnosticLog.Warning($"Cannot send juvenile route request: server peer unavailable for {characterId}.");
            return;
        }

        DiagnosticLog.Verbose(
            $"Sending juvenile route request to server {serverPeer.m_uid}: creature={characterId}, source={sourcePortalId}, species={speciesKey}.");
        ZRoutedRpc.instance.InvokeRoutedRPC(
            serverPeer.m_uid,
            RpcRequestJuvenileRoute,
            characterId,
            sourcePortalId,
            speciesKey ?? string.Empty);
    }

    public static void RequestEggRoute(
        ZDOID eggId,
        ZDOID sourcePortalId,
        string speciesKey,
        bool dualPurpose,
        string eggCollectorKey)
    {
        if (!CanSendRequests())
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(
            RpcRequestEggRoute,
            eggId,
            sourcePortalId,
            speciesKey ?? string.Empty,
            dualPurpose,
            eggCollectorKey ?? string.Empty);
    }

    public static void RequestAdultRoute(ZDOID characterId, ZDOID sourcePortalId, string speciesKey)
    {
        if (!CanSendRequests())
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(RpcRequestAdultRoute, characterId, sourcePortalId, speciesKey ?? string.Empty);
    }

    private static void OnRequestJuvenileRoute(long sender, ZDOID characterId, ZDOID sourcePortalId, string speciesKey)
    {
        DiagnosticLog.Verbose(
            $"Juvenile route request from peer {sender}: creature={characterId}, source={sourcePortalId}, species='{speciesKey}'.");
        if (!ZNet.instance.IsServer() || !TryBeginServerCooldown(characterId))
        {
            return;
        }

        ZDO existingCreature = ZDOMan.instance?.GetZDO(characterId);
        if (existingCreature != null
            && existingCreature.GetBool(ZdoFields.Transported)
            && !ModConfig.AllowRetransport.Value)
        {
            DiagnosticLog.Verbose($"Ignoring duplicate juvenile route request for already transferred creature {characterId}.");
            return;
        }

        EnsureServerRegistry();

        if (!DestinationRegistry.TryResolveMaturingDestination(speciesKey, out PortalRecord destination))
        {
            DestinationRegistry.LogMaturingResolutionFailure(speciesKey ?? string.Empty, "juvenile route request");
            return;
        }

        long previousOwner = existingCreature?.GetOwner() ?? 0L;
        DiagnosticLog.Verbose(
            $"Server received juvenile route request: sender={sender}, creature={characterId}, source={sourcePortalId}, destination={destination.Id}, previousOwner={previousOwner}.");

        if (JuvenileTeleporter.TryMoveZdo(characterId, destination, sourcePortalId))
        {
            Vector3 authoritativePosition = destination.Position;
            ZDO movedCreature = ZDOMan.instance?.GetZDO(characterId);
            if (movedCreature != null)
            {
                authoritativePosition = movedCreature.GetPosition();
            }

            float authoritativeRotationY = movedCreature?.GetRotation().eulerAngles.y ?? destination.Rotation.eulerAngles.y;
            DiagnosticLog.Info(
                $"Server-authoritative juvenile teleport completed: {characterId} -> {destination.Id} at {authoritativePosition}.");

            SendLiveJuvenileTransfer(characterId, sender, previousOwner, authoritativePosition, authoritativeRotationY);

            OPTeleportWorld sourcePortal = ResolvePortal(sourcePortalId);
            sourcePortal?.PlayActivationEffect();
            return;
        }

        DiagnosticLog.Warning(
            $"Server could not authoritatively move juvenile {characterId} to destination {destination.Id}.");
    }

    private static void OnApplyJuvenileLiveTransfer(long sender, ZDOID characterId, Vector3 targetPosition, float targetRotationY)
    {
        if (ZNet.instance == null || ZNet.instance.IsServer())
        {
            return;
        }

        bool applied = JuvenileTeleporter.TryApplyLiveTransfer(
            characterId,
            targetPosition,
            Quaternion.Euler(0f, targetRotationY, 0f));

        DiagnosticLog.Verbose(
            $"Received live juvenile transfer: creature={characterId}, target={targetPosition}, appliedToLocalCharacter={applied}, sender={sender}.");
    }

    private static void OnExecuteJuvenileRoute(
        long sender,
        ZDOID characterId,
        ZDOID sourcePortalId,
        ZDOID destinationPortalId,
        Vector3 destinationPosition,
        float destinationRotationY)
    {
        PortalRecord destination = ResolveDestinationRecord(destinationPortalId);
        if (destination == null)
        {
            DestinationRegistry.RegisterSnapshot(
                destinationPortalId,
                destinationPosition,
                Quaternion.Euler(0f, destinationRotationY, 0f));
            destination = DestinationRegistry.Get(destinationPortalId);
        }

        if (destination == null)
        {
            DiagnosticLog.Warning(
                $"Execute juvenile route failed: destination portal {destinationPortalId} is not registered at {destinationPosition}.");
            DiagnosticLog.LogPortalRegistry("execute juvenile route missing destination");
            return;
        }

        ZDO creatureZdo = ZDOMan.instance?.GetZDO(characterId);
        long owner = creatureZdo?.GetOwner() ?? 0L;
        if (ZNet.instance != null && !ZNet.instance.IsServer() && owner != 0L && owner != ZNet.GetUID())
        {
            DiagnosticLog.Verbose($"Ignoring legacy juvenile execute RPC on non-owner client: creature={characterId}, owner={owner}.");
            return;
        }

        if (JuvenilePortalRouter.ExecuteApprovedRoute(characterId, sourcePortalId, destination))
        {
            return;
        }

        DiagnosticLog.Warning(
            $"Execute juvenile route failed for creature {characterId} -> {destinationPortalId} at {destinationPosition}.");
    }

    private static void OnRequestEggRoute(
        long sender,
        ZDOID eggId,
        ZDOID sourcePortalId,
        string speciesKey,
        bool dualPurpose,
        string eggCollectorKey)
    {
        if (!ZNet.instance.IsServer() || !TryBeginServerCooldown(eggId))
        {
            return;
        }

        EnsureServerRegistry();

        BreedableSpeciesInfo speciesInfo = BuildEggSpeciesInfo(speciesKey, dualPurpose, eggCollectorKey, eggId);

        if (!EggRoutingResolver.TryResolveDestination(speciesInfo, out PortalRecord destination))
        {
            DiagnosticLog.Warning(
                $"No egg destination registered for '{speciesKey}' (dualPurpose={dualPurpose}, eggCollectorKey='{eggCollectorKey}'). " +
                $"Registry: {DestinationRegistry.BuildSummaryCounts()}.");
            return;
        }

        if (TryExecuteEggRouteLocally(eggId, sourcePortalId, destination))
        {
            return;
        }

        InvokeExecuteEgg(sender, eggId, sourcePortalId, destination.Id);
    }

    private static void OnExecuteEggRoute(long sender, ZDOID eggId, ZDOID sourcePortalId, ZDOID destinationPortalId)
    {
        PortalRecord destination = ResolveDestinationRecord(destinationPortalId);
        if (destination == null)
        {
            return;
        }

        EggPortalRouter.ExecuteApprovedRoute(eggId, sourcePortalId, destination);
    }

    private static void OnRequestAdultRoute(long sender, ZDOID characterId, ZDOID sourcePortalId, string speciesKey)
    {
        if (!ZNet.instance.IsServer() || !TryBeginServerCooldown(characterId))
        {
            return;
        }

        EnsureServerRegistry();

        PortalRecord source = ResolveDestinationRecord(sourcePortalId);
        if (source == null || source.Role != PortalRole.Maturing)
        {
            return;
        }

        PortalRecord destination;
        switch (source.AdultDestination)
        {
            case AdultDestination.Farm:
                if (!DestinationRegistry.TryResolveFarmDestination(speciesKey, out destination))
                {
                    return;
                }

                break;
            case AdultDestination.Cull:
                if (!DestinationRegistry.TryResolveCullDestination(out destination))
                {
                    return;
                }

                break;
            default:
                return;
        }

        if (TryExecuteAdultRouteLocally(characterId, sourcePortalId, destination))
        {
            return;
        }

        InvokeExecuteAdult(sender, characterId, sourcePortalId, destination.Id);
    }

    private static void OnExecuteAdultRoute(long sender, ZDOID characterId, ZDOID sourcePortalId, ZDOID destinationPortalId)
    {
        PortalRecord destination = ResolveDestinationRecord(destinationPortalId);
        if (destination == null)
        {
            return;
        }

        AdultPortalRouter.ExecuteApprovedRoute(characterId, sourcePortalId, destination);
    }

    private static bool TryBeginServerCooldown(ZDOID subjectId)
    {
        if (subjectId == ZDOID.None)
        {
            return false;
        }

        float now = Time.time;
        if (ServerCooldowns.TryGetValue(subjectId, out float nextAllowed) && now < nextAllowed)
        {
            return false;
        }

        ServerCooldowns[subjectId] = now + ModConfig.TeleportCooldownSec.Value;
        return true;
    }

    private static void SendLiveJuvenileTransfer(
        ZDOID characterId,
        long sender,
        long previousOwner,
        Vector3 targetPosition,
        float targetRotationY)
    {
        if (ZRoutedRpc.instance == null || ZNet.instance == null)
        {
            return;
        }

        HashSet<long> recipients = new HashSet<long>();
        if (sender != 0L)
        {
            recipients.Add(sender);
        }

        if (previousOwner != 0L && previousOwner != ZNet.GetUID())
        {
            recipients.Add(previousOwner);
        }

        foreach (long peerId in recipients)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(
                peerId,
                RpcApplyJuvenileLiveTransfer,
                characterId,
                targetPosition,
                targetRotationY);
        }

        if (recipients.Count > 0)
        {
            DiagnosticLog.Verbose(
                $"Sent live juvenile transfer notification: creature={characterId}, recipients={string.Join(",", recipients)}, target={targetPosition}.");
        }
    }

    private static void InvokeExecuteJuvenile(
        long sender,
        ZDOID characterId,
        ZDOID sourcePortalId,
        PortalRecord destination)
    {
        if (destination == null)
        {
            return;
        }

        DestinationRegistry.RefreshPortalPosition(destination);
        Vector3 destinationPosition = destination.Position;
        float destinationRotationY = destination.Rotation.eulerAngles.y;
        ZDO destinationZdo = ZDOMan.instance?.GetZDO(destination.Id);
        if (destinationZdo != null)
        {
            destinationPosition = destinationZdo.GetPosition();
            destinationRotationY = destinationZdo.GetRotation().eulerAngles.y;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(
            sender,
            RpcExecuteJuvenileRoute,
            characterId,
            sourcePortalId,
            destination.Id,
            destinationPosition,
            destinationRotationY);
        long owner = ZDOMan.instance?.GetZDO(characterId)?.GetOwner() ?? 0L;
        if (owner != 0L && owner != sender)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(
                owner,
                RpcExecuteJuvenileRoute,
                characterId,
                sourcePortalId,
                destination.Id,
                destinationPosition,
                destinationRotationY);
        }
    }

    private static void InvokeExecuteEgg(long sender, ZDOID eggId, ZDOID sourcePortalId, ZDOID destinationId)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcExecuteEggRoute, eggId, sourcePortalId, destinationId);
        long owner = ZDOMan.instance?.GetZDO(eggId)?.GetOwner() ?? 0L;
        if (owner != 0L && owner != sender)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(owner, RpcExecuteEggRoute, eggId, sourcePortalId, destinationId);
        }
    }

    private static void InvokeExecuteAdult(long sender, ZDOID characterId, ZDOID sourcePortalId, ZDOID destinationId)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcExecuteAdultRoute, characterId, sourcePortalId, destinationId);
        long owner = ZDOMan.instance?.GetZDO(characterId)?.GetOwner() ?? 0L;
        if (owner != 0L && owner != sender)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(owner, RpcExecuteAdultRoute, characterId, sourcePortalId, destinationId);
        }
    }

    private static bool TryExecuteJuvenileRouteLocally(
        ZDOID characterId,
        ZDOID sourcePortalId,
        PortalRecord destination)
    {
        Character character = ResolveCharacter(characterId);
        OPTeleportWorld sourcePortal = ResolvePortal(sourcePortalId);
        if (character == null || sourcePortal == null || destination == null)
        {
            return false;
        }

        return JuvenilePortalRouter.ExecuteApprovedRoute(characterId, sourcePortalId, destination, character, sourcePortal);
    }

    private static bool TryExecuteEggRouteLocally(ZDOID eggId, ZDOID sourcePortalId, PortalRecord destination)
    {
        ItemDrop egg = ResolveEgg(eggId);
        OPTeleportWorld sourcePortal = ResolvePortal(sourcePortalId);
        if (egg == null || sourcePortal == null || destination == null)
        {
            return false;
        }

        return EggPortalRouter.ExecuteApprovedRoute(eggId, sourcePortalId, destination, egg, sourcePortal);
    }

    private static bool TryExecuteAdultRouteLocally(
        ZDOID characterId,
        ZDOID sourcePortalId,
        PortalRecord destination)
    {
        Character character = ResolveCharacter(characterId);
        OPTeleportWorld sourcePortal = ResolvePortal(sourcePortalId);
        if (character == null || sourcePortal == null || destination == null)
        {
            return false;
        }

        return AdultPortalRouter.ExecuteApprovedRoute(characterId, sourcePortalId, destination, character, sourcePortal);
    }

    private static BreedableSpeciesInfo BuildEggSpeciesInfo(
        string speciesKey,
        bool dualPurpose,
        string eggCollectorKey,
        ZDOID eggId)
    {
        ItemDrop egg = ResolveEgg(eggId);
        if (egg != null && SpeciesDiscovery.TryGetEggSpeciesInfo(egg, out BreedableSpeciesInfo discovered))
        {
            return discovered;
        }

        return new BreedableSpeciesInfo
        {
            Key = speciesKey ?? string.Empty,
            DisplayName = SpeciesKey.GetDisplayName(speciesKey),
            IsDualPurposeEgg = dualPurpose,
            EggCollectorKey = eggCollectorKey ?? string.Empty
        };
    }

    private static Character ResolveCharacter(ZDOID characterId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(characterId);
        return instance != null ? instance.GetComponent<Character>() : null;
    }

    private static ItemDrop ResolveEgg(ZDOID eggId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(eggId);
        return instance != null ? instance.GetComponent<ItemDrop>() : null;
    }

    private static OPTeleportWorld ResolvePortal(ZDOID portalId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(portalId);
        return instance != null ? instance.GetComponent<OPTeleportWorld>() : null;
    }

    private static void EnsureServerRegistry()
    {
        if (!ZNet.instance.IsServer() || ZDOMan.instance == null)
        {
            return;
        }

        PortalHelper.SyncRegistryFromAllPortalZdos();
    }

    private static PortalRecord ResolveDestinationRecord(ZDOID destinationPortalId)
    {
        PortalRecord destination = DestinationRegistry.Get(destinationPortalId);
        if (destination != null)
        {
            return destination;
        }

        if (ZDOMan.instance == null)
        {
            return null;
        }

        ZDO zdo = ZDOMan.instance.GetZDO(destinationPortalId);
        if (zdo == null)
        {
            return null;
        }

        PortalHelper.SyncRegistryFromZdo(zdo);
        return DestinationRegistry.Get(destinationPortalId);
    }
}
