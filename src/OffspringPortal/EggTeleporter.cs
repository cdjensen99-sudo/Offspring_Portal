using UnityEngine;

namespace OffspringPortal;

public static class EggTeleporter
{
    public static bool TryTeleport(ItemDrop egg, PortalRecord destination, OPTeleportWorld sourcePortal)
    {
        return TryExecuteApproved(egg, destination, sourcePortal);
    }

    public static bool TryExecuteApproved(ItemDrop egg, PortalRecord destination, OPTeleportWorld sourcePortal)
    {
        if (egg == null || destination == null || sourcePortal == null)
        {
            return false;
        }

        DestinationRegistry.RefreshPortalPosition(destination);
        Vector3 targetPos = PortalPlacement.GetExitPosition(destination.Id, sourcePortal);
        Quaternion targetRot = PortalPlacement.GetExitRotation(destination.Id, sourcePortal);

        ZNetView nview = egg.GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
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

        Vector3 placed = targetPos + targetRot * Vector3.right * Random.Range(-0.15f, 0.15f);
        egg.transform.position = placed;
        egg.transform.rotation = targetRot;

        ZDO zdo = nview.GetZDO();
        if (zdo != null)
        {
            zdo.SetPosition(placed);
            zdo.SetRotation(targetRot);
            zdo.Set(ZdoFields.Transported, true);
        }

        Rigidbody body = egg.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        return true;
    }
}
