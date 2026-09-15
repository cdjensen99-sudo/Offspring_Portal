using System.Collections.Generic;

using UnityEngine;



namespace OffspringPortal;



public static class EggPortalRouter

{

    public static bool TryRoute(

        OPTeleportWorld portal,

        ItemDrop egg,

        Dictionary<int, float> cooldowns,

        ref float lastNoDestinationMessageTime)

    {

        if (portal == null || egg == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))

        {

            return false;

        }



        if (egg.GetComponent<EggGrow>() == null)

        {

            return false;

        }



        if (!JuvenilePortalRouter.IsBreederPortal(portal))

        {

            return false;

        }



        ZNetView nview = egg.GetComponent<ZNetView>();

        ZDO zdo = nview?.GetZDO();

        if (zdo != null && zdo.GetBool(ZdoFields.Transported) && !ModConfig.AllowRetransport.Value)

        {

            return false;

        }



        int bodyId = egg.GetInstanceID();

        float now = Time.time;



        if (!SpeciesDiscovery.TryGetEggSpeciesInfo(egg, out BreedableSpeciesInfo speciesInfo))

        {

            if (now - lastNoDestinationMessageTime > 5f)

            {

                lastNoDestinationMessageTime = now;

                string eggName = SpeciesKey.Normalize(egg.name);

                OffspringPortalPlugin.Log.LogWarning(

                    $"Breeder portal could not classify egg '{eggName}' for routing (missing EggGrow chain or species not discovered).");

            }



            return false;

        }



        if (cooldowns.TryGetValue(bodyId, out float nextAllowed) && now < nextAllowed)

        {

            return false;

        }



        ZDOID eggId = nview?.GetZDO()?.m_uid ?? ZDOID.None;

        ZDOID sourcePortalId = portal.GetComponent<ZNetView>()?.GetZDO()?.m_uid ?? ZDOID.None;

        if (eggId == ZDOID.None || sourcePortalId == ZDOID.None)

        {

            return false;

        }



        if (ZNet.instance.IsServer())

        {

            return TryRouteOnServer(

                portal,

                egg,

                speciesInfo,

                eggId,

                sourcePortalId,

                cooldowns,

                ref lastNoDestinationMessageTime,

                now,

                bodyId);

        }



        if (!RoutingRpc.CanSendRequests())

        {

            return false;

        }



        RoutingRpc.RequestEggRoute(

            eggId,

            sourcePortalId,

            speciesInfo.Key,

            speciesInfo.IsDualPurposeEgg,

            speciesInfo.EggCollectorKey);

        cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;

        return false;

    }



    public static bool ExecuteApprovedRoute(

        ZDOID eggId,

        ZDOID sourcePortalId,

        PortalRecord destination,

        ItemDrop egg = null,

        OPTeleportWorld sourcePortal = null)

    {

        egg ??= ResolveEgg(eggId);

        sourcePortal ??= ResolvePortal(sourcePortalId);

        if (egg == null || sourcePortal == null || destination == null)

        {

            return false;

        }



        if (!JuvenilePortalRouter.IsBreederPortal(sourcePortal))

        {

            return false;

        }



        if (!EggTeleporter.TryExecuteApproved(egg, destination, sourcePortal))

        {

            return false;

        }



        if (SpeciesDiscovery.TryGetEggSpeciesInfo(egg, out BreedableSpeciesInfo speciesInfo))

        {

            string destinationLabel = destination.Role == PortalRole.EggCollector

                ? SpeciesKey.GetEggCollectorDisplayName(speciesInfo.EggCollectorKey)

                : "maturing portal";

            OffspringPortalPlugin.Log.LogInfo(

                $"Teleported {speciesInfo.DisplayName} egg to {destinationLabel} at {destination.Position}.");

        }



        PlayPortalActivation(sourcePortal);

        return true;

    }



    private static bool TryRouteOnServer(

        OPTeleportWorld portal,

        ItemDrop egg,

        BreedableSpeciesInfo speciesInfo,

        ZDOID eggId,

        ZDOID sourcePortalId,

        Dictionary<int, float> cooldowns,

        ref float lastNoDestinationMessageTime,

        float now,

        int bodyId)

    {

        if (!EggRoutingResolver.TryResolveDestination(speciesInfo, out PortalRecord destination))

        {

            if (now - lastNoDestinationMessageTime > 5f)

            {

                lastNoDestinationMessageTime = now;

                string message = GetEggRoutingFailureMessage(speciesInfo);

                OffspringPortalPlugin.Log.LogWarning(message);

                Player local = Player.m_localPlayer;

                if (local != null)

                {

                    local.Message(MessageHud.MessageType.Center, message);

                }

            }



            return false;

        }



        if (ExecuteApprovedRoute(eggId, sourcePortalId, destination, egg, portal))

        {

            cooldowns[bodyId] = now + ModConfig.TeleportCooldownSec.Value;

            return true;

        }



        return false;

    }



    private static string GetEggRoutingFailureMessage(BreedableSpeciesInfo speciesInfo)

    {

        if (speciesInfo.IsDualPurposeEgg && EggRoutingResolver.IsHenEgg(speciesInfo))

        {

            if (DestinationRegistry.HasEggCollector())

            {

                return EggRoutingResolver.GetHenRoutingFailureMessage();

            }

            return "No Egg Collector or Maturing portal registered for Hen eggs.";

        }

        if (speciesInfo.IsDualPurposeEgg)

        {

            string eggLabel = SpeciesKey.GetEggCollectorDisplayName(speciesInfo.EggCollectorKey);

            return $"No egg collector for {eggLabel} and no maturing fallback registered.";

        }

        return $"No maturing portal registered for {speciesInfo.DisplayName} eggs.";

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



    private static void PlayPortalActivation(OPTeleportWorld portal)

    {

        portal?.PlayActivationEffect();

    }

}

