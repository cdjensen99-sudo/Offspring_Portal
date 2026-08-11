using System;
using Jotunn.Managers;
using UnityEngine;

namespace OffspringPortal;

public static class OffspringPortalPrefabs
{
    public static GameObject Prefab { get; private set; }
    public static bool Registered { get; private set; }
    private static bool pieceRegistered;

    public static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefab;
        PieceManager.OnPiecesRegistered += RegisterPiece;
        PrefabManager.OnPrefabsRegistered += RegisterPiece;
    }

    public static void EnsurePieceRegistered()
    {
        RegisterPiece();
    }

    private static void RegisterPrefab()
    {
        if (Registered)
        {
            return;
        }

        GameObject clone = PrefabManager.Instance.CreateClonedPrefab(
            PrefabNames.OffspringPortal,
            PrefabNames.VanillaPortalWood);

        if (clone == null)
        {
            OffspringPortalPlugin.Log.LogError("Failed to clone portal_wood via Jotunn.");
            return;
        }

        ConfigureClone(clone);
        OPTeleportWorld portal = ConvertFromVanillaPortal(clone);
        EnsureRuntimeTriggers(portal);

        PrefabManager.Instance.AddPrefab(clone);
        Prefab = clone;
        Registered = true;

        PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefab;
        OffspringPortalPlugin.Log.LogInfo("Registered offspring_portal prefab.");

        RegisterPiece();
    }

    private static void RegisterPiece()
    {
        if (pieceRegistered || !Registered || Prefab == null)
        {
            return;
        }

        try
        {
            ApplyRecipe(Prefab);
            PieceManager.Instance.RegisterPieceInPieceTable(Prefab, "Hammer");
            pieceRegistered = true;
            PieceManager.OnPiecesRegistered -= RegisterPiece;
            OffspringPortalPlugin.Log.LogInfo("Registered offspring_portal in Hammer piece table.");
        }
        catch (Exception ex)
        {
            OffspringPortalPlugin.Log.LogWarning($"Deferring offspring_portal piece registration: {ex.Message}");
        }
    }

    private static void ApplyRecipe(GameObject clone)
    {
        Piece piece = clone.GetComponent<Piece>();
        if (piece == null || ObjectDB.instance == null)
        {
            return;
        }

        piece.m_resources = new[]
        {
            CreateRequirement("FineWood", 20),
            CreateRequirement("GreydwarfEye", 10),
            CreateRequirement("SurtlingCore", 2)
        };
    }

    private static void ConfigureClone(GameObject clone)
    {
        clone.transform.localScale = Vector3.one * PortalPlacement.PortalScale;

        Piece piece = clone.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_name = "$piece_offspring_portal";
            piece.m_description = "$piece_offspring_portal_description";
        }
    }

    internal static OPTeleportWorld ConvertFromVanillaPortal(GameObject gameObject)
    {
        Transform proximityRoot = null;
        float exitDistance = PortalPlacement.ScaledExitDistance;
        EffectList connectedVfx = null;

        TeleportWorld legacyPortal = gameObject.GetComponent<TeleportWorld>();
        if (legacyPortal != null)
        {
            proximityRoot = legacyPortal.m_proximityRoot;
            exitDistance = legacyPortal.m_exitDistance;
            connectedVfx = legacyPortal.m_connected;
            DestroyPortalComponent(legacyPortal);
        }

        foreach (TeleportWorldTrigger legacyTrigger in gameObject.GetComponentsInChildren<TeleportWorldTrigger>(true))
        {
            DestroyPortalComponent(legacyTrigger);
        }

        OPTeleportWorld portal = gameObject.GetComponent<OPTeleportWorld>();
        if (portal == null)
        {
            portal = gameObject.AddComponent<OPTeleportWorld>();
        }

        portal.Initialize(proximityRoot, exitDistance, connectedVfx);
        return portal;
    }

    public static void MigrateLegacyPortal(GameObject gameObject)
    {
        if (gameObject == null || gameObject.GetComponent<OPTeleportWorld>() != null)
        {
            return;
        }

        if (!IsOffspringPortal(gameObject))
        {
            return;
        }

        OPTeleportWorld portal = ConvertFromVanillaPortal(gameObject);
        EnsureRuntimeTriggers(portal);
        PortalHelper.EnsurePortalInitialized(portal);
    }

    public static void EnsureRuntimeTriggers(OPTeleportWorld portal)
    {
        if (portal == null)
        {
            return;
        }

        if (portal.m_proximityRoot != null)
        {
            foreach (Collider collider in portal.m_proximityRoot.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.isTrigger || collider.GetComponent<OffspringPortalTrigger>() != null)
                {
                    continue;
                }

                collider.gameObject.AddComponent<OffspringPortalTrigger>();
            }
        }

        if (portal.GetComponent<OffspringPortalScanner>() == null)
        {
            portal.gameObject.AddComponent<OffspringPortalScanner>();
        }
    }

    private static Piece.Requirement CreateRequirement(string itemName, int amount)
    {
        GameObject itemPrefab = ObjectDB.instance?.GetItemPrefab(itemName);
        return new Piece.Requirement
        {
            m_resItem = itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null,
            m_amount = amount,
            m_amountPerLevel = 0,
            m_recover = true
        };
    }

    public static bool IsOffspringPortal(OPTeleportWorld portal)
    {
        return portal != null && IsOffspringPortal(portal.gameObject);
    }

    public static bool IsOffspringPortal(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        if (gameObject.GetComponent<OPTeleportWorld>() != null)
        {
            return true;
        }

        if (IsOffspringPiece(gameObject))
        {
            return true;
        }

        ZNetView nview = gameObject.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        if (zdo.GetPrefab() == PrefabNames.OffspringPortal.GetStableHashCode())
        {
            return true;
        }

        return !string.IsNullOrEmpty(zdo.GetString(ZdoFields.PortalRole, string.Empty));
    }

    public static void EnsureIdentity(OPTeleportWorld portal)
    {
        if (portal == null || !IsOffspringPiece(portal.gameObject))
        {
            return;
        }

        if (portal.GetComponent<OPTeleportWorld>() == null)
        {
            portal.gameObject.AddComponent<OPTeleportWorld>();
        }
    }

    private static bool IsOffspringPiece(GameObject gameObject)
    {
        Piece piece = gameObject.GetComponent<Piece>();
        return piece != null && piece.m_name == "$piece_offspring_portal";
    }

    private static void DestroyPortalComponent(Component component)
    {
        if (component == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(component);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }
}
