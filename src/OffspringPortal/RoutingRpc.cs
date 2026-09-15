using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class RoutingRpc
{
    private const string RpcRequestJuvenileRoute = "OffspringPortal_RequestJuvenileRoute";
    private const string RpcExecuteJuvenileRoute = "OffspringPortal_ExecuteJuvenileRoute";
    private const string RpcRequestEggRoute = "OffspringPortal_RequestEggRoute";
    private const string RpcExecuteEggRoute = "OffspringPortal_ExecuteEggRoute";
    private const string RpcRequestAdultRoute = "OffspringPortal_RequestAdultRoute";
    private const string RpcExecuteAdultRoute = "OffspringPortal_ExecuteAdultRoute";

    private static readonly Dictionary<ZDOID, float> ServerCooldowns = new Dictionary<ZDOID, float>();
    private static bool registered;

    public static bool CanSendRequests()
    {
        return ZRoutedRpc.instance != null && ZNet.instance != null;
    }

    public static void Register()
    {
        if (registered || ZRoutedRpc.instance == null)
        {
            return;
        }

        ZRoutedRpc instance = ZRoutedRpc.instance;
        instance.Register<ZDOID, ZDOID, string>(RpcRequestJuvenileRoute, OnRequestJuvenileRoute);
        instance.Register<ZDOID, ZDOID, ZDOID>(RpcExecuteJuvenileRoute, OnExecuteJuvenileRoute);
        instance.Register<ZDOID, ZDOID, string, bool, string>(RpcRequestEggRoute, OnRequestEggRoute);
        instance.Register<ZDOID, ZDOID, ZDOID>(RpcExecuteEggRoute, OnExecuteEggRoute);
        instance.Register<ZDOID, ZDOID, string>(RpcRequestAdultRoute, OnRequestAdultRoute);
        instance.Register<ZDOID, ZDOID, ZDOID>(RpcExecuteAdultRoute, OnExecuteAdultRoute);
        registered = true;
    }

    public static void RequestJuvenileRoute(ZDOID characterId, ZDOID sourcePortalId, string speciesKey)
    {
        if (!CanSendRequests())
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(RpcRequestJuvenileRoute, characterId, sourcePortalId, speciesKey ?? string.Empty);
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
        if (!ZNet.instance.IsServer() || !TryBeginServerCooldown(characterId))
        {
            return;
        }

        EnsureServerRegistry();

        if (!DestinationRegistry.TryResolveMaturingDestination(speciesKey, out PortalRecord destination))
        {
            return;
        }

        if (TryExecuteJuvenileRouteLocally(characterId, sourcePortalId, destination))
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcExecuteJuvenileRoute, characterId, sourcePortalId, destination.Id);
    }

    private static void OnExecuteJuvenileRoute(long sender, ZDOID characterId, ZDOID sourcePortalId, ZDOID destinationPortalId)
    {
        PortalRecord destination = ResolveDestinationRecord(destinationPortalId);
        if (destination == null)
        {
            return;
        }

        JuvenilePortalRouter.ExecuteApprovedRoute(characterId, sourcePortalId, destination);
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
            return;
        }

        if (TryExecuteEggRouteLocally(eggId, sourcePortalId, destination))
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcExecuteEggRoute, eggId, sourcePortalId, destination.Id);
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

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcExecuteAdultRoute, characterId, sourcePortalId, destination.Id);
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
