using UnityEngine;

namespace OffspringPortal;

public static class JuvenilePlacement
{
    private static readonly int FloorMask = LayerMask.GetMask(
        "Default", "static_solid", "piece", "piece_nonsolid", "terrain", "water");

    public static bool TryGetFloorPosition(Vector3 position, out Vector3 grounded)
    {
        grounded = position;
        if (ZoneSystem.instance == null)
        {
            return false;
        }

        float skyY = Mathf.Max(position.y + 30f, 128f);
        Vector3 skyProbe = new Vector3(position.x, skyY, position.z);

        if (!ZoneSystem.instance.GetGroundHeight(skyProbe, out float terrainY))
        {
            terrainY = ZoneSystem.instance.GetSolidHeight(position);
        }

        if (position.y - terrainY > 2f)
        {
            grounded.y = terrainY + 0.2f;
            return true;
        }

        float bestFloor = terrainY + 0.2f;
        RaycastHit[] hits = Physics.RaycastAll(skyProbe, Vector3.down, skyY + 100f, FloorMask);
        foreach (RaycastHit hit in hits)
        {
            if (hit.point.y > position.y + 1f || hit.point.y < terrainY - 1f)
            {
                continue;
            }

            if (hit.point.y > bestFloor)
            {
                bestFloor = hit.point.y + 0.15f;
            }
        }

        grounded.y = bestFloor;
        return true;
    }

    public static void ApplyPosition(Character character, Vector3 position, Quaternion rotation)
    {
        if (character == null)
        {
            return;
        }

        character.transform.position = position;
        character.transform.rotation = rotation;

        Rigidbody body = character.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        character.SetLookDir(position, 0f);

        ZNetView nview = character.GetNview();
        ZDO zdo = nview?.GetZDO();
        if (zdo != null)
        {
            zdo.SetPosition(position);
            zdo.SetRotation(rotation);
        }
    }
}
