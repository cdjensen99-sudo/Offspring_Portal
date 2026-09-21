using UnityEngine;

namespace OffspringPortal;

public static class JuvenileTeleporter
{
    public static bool TryTeleport(Character juvenile, PortalRecord destination, OPTeleportWorld sourcePortal)
    {
        if (juvenile == null || destination == null || sourcePortal == null)
        {
            return false;
        }

        if (!ZNet.instance.IsServer() && !CanExecuteLocally(juvenile))
        {
            return false;
        }

        DestinationRegistry.RefreshPortalPosition(destination);
        Vector3 targetPos = PortalPlacement.GetExitPosition(destination.Id, sourcePortal);

        if (ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(targetPos))
        {
            return TryExecuteApproved(juvenile, destination, sourcePortal);
        }

        if (TryExecuteApproved(juvenile, destination, sourcePortal, allowStoredHeight: true))
        {
            DiagnosticLog.Info(
                $"Cross-zone teleport of {SpeciesCatalog.GetDisplayName(SpeciesHelper.GetJuvenileSpecies(juvenile))} to {targetPos}.");
            return true;
        }

        DiagnosticLog.Info(
            $"Deferring teleport of {SpeciesCatalog.GetDisplayName(SpeciesHelper.GetJuvenileSpecies(juvenile))} until pen zone loads.");

        return ZNet.instance.IsServer()
            && OffspringPortalRuntime.Instance.TryQueueDistantTeleport(juvenile, destination, sourcePortal);
    }

    public static bool TryTeleportNow(
        Character juvenile,
        PortalRecord destination,
        OPTeleportWorld sourcePortal,
        bool allowStoredHeight = false)
    {
        return TryExecuteApproved(juvenile, destination, sourcePortal, allowStoredHeight);
    }

    public static bool TryExecuteApproved(
        Character juvenile,
        PortalRecord destination,
        OPTeleportWorld sourcePortal,
        bool allowStoredHeight = false)
    {
        if (juvenile == null || destination == null || sourcePortal == null)
        {
            return false;
        }

        ZNetView nview = juvenile.GetNview();
        if (nview == null || !nview.IsValid())
        {
            return false;
        }

        if (!nview.IsOwner())
        {
            nview.ClaimOwnership();
            if (!nview.IsOwner() && !ZNet.instance.IsServer())
            {
                return false;
            }
        }

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

    public static bool TryMoveZdo(ZDOID characterId, PortalRecord destination, ZDOID sourcePortalId)
    {
        if (!ZNet.instance.IsServer() || characterId == ZDOID.None || destination == null || ZDOMan.instance == null)
        {
            return false;
        }

        ZDO zdo = ZDOMan.instance.GetZDO(characterId);
        if (zdo == null || !zdo.IsValid())
        {
            DiagnosticLog.Verbose($"Authoritative juvenile move failed: creature ZDO {characterId} is unavailable or invalid.");
            return false;
        }

        if (zdo.GetBool(ZdoFields.Transported) && !ModConfig.AllowRetransport.Value)
        {
            DiagnosticLog.Verbose($"Authoritative juvenile move skipped: {characterId} is already marked transported.");
            return false;
        }

        OPTeleportWorld sourcePortal = null;
        GameObject sourceObject = ZNetScene.instance?.FindInstance(sourcePortalId);
        if (sourceObject != null)
        {
            sourcePortal = sourceObject.GetComponent<OPTeleportWorld>();
        }

        DestinationRegistry.RefreshPortalPosition(destination);
        Vector3 targetPos = PortalPlacement.GetExitPosition(destination.Id, sourcePortal);
        Quaternion targetRot = PortalPlacement.GetExitRotation(destination.Id, sourcePortal);

        bool areaReady = ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(targetPos);
        if (areaReady && JuvenilePlacement.TryGetFloorPosition(targetPos, out Vector3 grounded))
        {
            targetPos = grounded;
        }

        long previousOwner = zdo.GetOwner();
        zdo.SetOwner(ZNet.GetUID());

        zdo.SetPosition(targetPos);
        zdo.SetRotation(targetRot);
        zdo.Set(ZdoFields.Transported, true);
        zdo.Set(ZDOVars.s_velHash, Vector3.zero);
        zdo.Set(ZDOVars.s_bodyVelHash, Vector3.zero);
        zdo.Set(ZDOVars.s_bodyAVelHash, Vector3.zero);
        ZDOMan.instance.ForceSendZDO(characterId);

        GameObject instance = ZNetScene.instance?.FindInstance(characterId);
        Character character = instance != null ? instance.GetComponent<Character>() : null;
        if (character != null)
        {
            JuvenilePlacement.ApplyLivePosition(character, targetPos, targetRot);
            JuvenileGroundSnapper.EnsureAttached(character);
            JuvenileFollowController.EnsureAttached(character);
        }

        DiagnosticLog.Verbose(
            $"Authoritative juvenile ZDO move: creature={characterId}, destination={destination.Id}, target={targetPos}, areaReady={areaReady}, instantiatedOnServer={character != null}, previousOwner={previousOwner}.");
        return true;
    }

    public static bool TryApplyLiveTransfer(ZDOID characterId, Vector3 targetPos, Quaternion targetRot)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(characterId);
        Character character = instance != null ? instance.GetComponent<Character>() : null;
        if (character == null)
        {
            return false;
        }

        JuvenilePlacement.ApplyLivePosition(character, targetPos, targetRot);
        JuvenileGroundSnapper.EnsureAttached(character);
        JuvenileFollowController.EnsureAttached(character);
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

    private static bool CanExecuteLocally(Character juvenile)
    {
        ZNetView nview = juvenile?.GetNview();
        return nview != null && nview.IsValid() && nview.IsOwner();
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
