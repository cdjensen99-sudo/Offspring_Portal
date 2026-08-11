using System.Collections.Generic;

using UnityEngine;



namespace OffspringPortal;



public static class JuvenileTeleporter

{

    public static bool TryTeleport(Character juvenile, PortalRecord destination, OPTeleportWorld sourcePortal)

    {

        if (juvenile == null || destination == null || sourcePortal == null || !ZNet.instance.IsServer())

        {

            return false;

        }



        DestinationRegistry.RefreshPortalPosition(destination);

        Vector3 targetPos = PortalPlacement.GetExitPosition(destination.Id, sourcePortal);



        if (ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(targetPos))

        {

            return TryTeleportNow(juvenile, destination, sourcePortal);

        }



        if (TryTeleportNow(juvenile, destination, sourcePortal, allowStoredHeight: true))

        {

            OffspringPortalPlugin.Log.LogInfo(

                $"Cross-zone teleport of {SpeciesCatalog.GetDisplayName(SpeciesHelper.GetJuvenileSpecies(juvenile))} to {targetPos}.");

            return true;

        }



        OffspringPortalPlugin.Log.LogInfo(

            $"Deferring teleport of {SpeciesCatalog.GetDisplayName(SpeciesHelper.GetJuvenileSpecies(juvenile))} until pen zone loads.");

        return OffspringPortalRuntime.Instance.TryQueueDistantTeleport(juvenile, destination, sourcePortal);

    }



    public static bool TryTeleportNow(

        Character juvenile,

        PortalRecord destination,

        OPTeleportWorld sourcePortal,

        bool allowStoredHeight = false)

    {

        if (juvenile == null || destination == null || sourcePortal == null)

        {

            return false;

        }



        if (!ZNet.instance.IsServer())

        {

            return false;

        }



        ZNetView nview = juvenile.GetNview();

        if (nview == null || !nview.IsValid())

        {

            return false;

        }



        nview.ClaimOwnership();



        Tameable tameable = juvenile.GetComponent<Tameable>();

        if (tameable != null)

        {

            tameable.m_unsummonDistance = 0f;

        }



        DestinationRegistry.RefreshPortalPosition(destination);

        Vector3 targetPos = PortalPlacement.GetExitPosition(destination.Id, sourcePortal);

        Quaternion targetRot = PortalPlacement.GetExitRotation(destination.Id, sourcePortal);



        if (!TryPlaceCharacter(juvenile, targetPos, targetRot, allowStoredHeight))

        {

            Player local = Player.m_localPlayer;

            if (local != null)

            {

                local.Message(MessageHud.MessageType.Center,

                    $"Could not place {SpeciesCatalog.GetDisplayName(SpeciesHelper.GetJuvenileSpecies(juvenile))} at destination.");

            }



            return false;

        }



        JuvenileGroundSnapper.EnsureAttached(juvenile);
        JuvenileFollowController.EnsureAttached(juvenile);

        ZDO zdo = nview.GetZDO();

        if (zdo != null)

        {

            zdo.Set(ZdoFields.Transported, true);

        }



        return true;

    }



    public static Vector3 ResolveDestinationPosition(PortalRecord destination, OPTeleportWorld sourcePortal)

    {

        return PortalPlacement.GetExitPosition(destination.Id, sourcePortal);

    }



    public static Quaternion ResolveDestinationRotation(PortalRecord destination, OPTeleportWorld sourcePortal)

    {

        return PortalPlacement.GetExitRotation(destination.Id, sourcePortal);

    }



    private static bool TryPlaceCharacter(

        Character character,

        Vector3 destination,

        Quaternion rotation,

        bool allowStoredHeight)

    {

        Vector3 right = rotation * Vector3.right;

        for (int attempt = 0; attempt < 5; attempt++)

        {

            Vector3 candidate = destination + right * Random.Range(-0.2f, 0.2f);

            if (JuvenilePlacement.TryGetFloorPosition(candidate, out Vector3 grounded))

            {

                JuvenilePlacement.ApplyPosition(character, grounded, rotation);

                JuvenileGroundSnapper.EnsureAttached(character);

                return true;

            }

        }



        if (!allowStoredHeight)

        {

            return false;

        }



        if (JuvenilePlacement.TryGetFloorPosition(destination, out Vector3 stored))

        {

            JuvenilePlacement.ApplyPosition(character, stored, rotation);

            return true;

        }



        return false;

    }

}


