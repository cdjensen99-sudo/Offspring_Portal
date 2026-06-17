using System;
using System.Collections;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace OffspringPortal;

public class OffspringPortalMarker : MonoBehaviour
{
    private TeleportWorld teleportWorld;

    private void Awake()
    {
        teleportWorld = GetComponent<TeleportWorld>();
    }

    private void Start()
    {
        if (teleportWorld != null && !teleportWorld.enabled)
        {
            StartCoroutine(InitializeTeleportWorldWhenReady());
        }
    }

    private IEnumerator InitializeTeleportWorldWhenReady()
    {
        ZNetView nview = GetComponent<ZNetView>();
        for (int i = 0; i < 30; i++)
        {
            if (nview != null && nview.GetZDO() != null)
            {
                break;
            }

            yield return null;
        }

        if (teleportWorld == null || nview == null || nview.GetZDO() == null || teleportWorld.enabled)
        {
            yield break;
        }

        OffspringPortalInitializer.CompleteTeleportWorldAwake(teleportWorld, nview);
    }
}

internal static class OffspringPortalInitializer
{
    internal static void CompleteTeleportWorldAwake(TeleportWorld portal, ZNetView nview)
    {
        if (portal.enabled)
        {
            return;
        }

        Traverse.Create(portal).Field<ZNetView>("m_nview").Value = nview;
        Traverse.Create(portal).Field<bool>("m_hadTarget").Value =
            (bool)AccessTools.Method(typeof(TeleportWorld), "HaveTarget").Invoke(portal, null);

        nview.Register<string, string>("RPC_SetTag", (sender, tag, target) =>
            AccessTools.Method(typeof(TeleportWorld), "RPC_SetTag").Invoke(portal, new object[] { sender, tag, target }));

        nview.Register<ZDOID>("RPC_SetConnected", (sender, targetId) =>
            AccessTools.Method(typeof(TeleportWorld), "RPC_SetConnected").Invoke(portal, new object[] { sender, targetId }));

        portal.enabled = true;
        portal.InvokeRepeating("UpdatePortal", 0.5f, 0.5f);
    }
}

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
        EnsureRuntimeTriggers(clone.GetComponent<TeleportWorld>());

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

        TeleportWorld teleportWorld = clone.GetComponent<TeleportWorld>();
        if (teleportWorld != null)
        {
            teleportWorld.m_exitDistance = PortalPlacement.ScaledExitDistance;
        }

        EnsureRuntimeTriggers(teleportWorld);

        if (clone.GetComponent<OffspringPortalMarker>() == null)
        {
            clone.AddComponent<OffspringPortalMarker>();
        }

        Piece piece = clone.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_name = "$piece_offspring_portal";
            piece.m_description = "$piece_offspring_portal_description";
        }
    }

    public static void EnsureRuntimeTriggers(TeleportWorld teleportWorld)
    {
        if (teleportWorld == null)
        {
            return;
        }

        foreach (TeleportWorldTrigger vanillaTrigger in teleportWorld.GetComponentsInChildren<TeleportWorldTrigger>(true))
        {
            GameObject triggerObject = vanillaTrigger.gameObject;
            vanillaTrigger.enabled = false;
            if (triggerObject.GetComponent<OffspringPortalTrigger>() == null)
            {
                triggerObject.AddComponent<OffspringPortalTrigger>();
            }
        }

        if (teleportWorld.m_proximityRoot == null)
        {
            return;
        }

        foreach (Collider collider in teleportWorld.m_proximityRoot.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.isTrigger || collider.GetComponent<OffspringPortalTrigger>() != null)
            {
                continue;
            }

            collider.gameObject.AddComponent<OffspringPortalTrigger>();
        }

        if (teleportWorld.GetComponent<OffspringPortalScanner>() == null)
        {
            teleportWorld.gameObject.AddComponent<OffspringPortalScanner>();
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

    public static bool IsOffspringPortal(TeleportWorld portal)
    {
        if (portal == null)
        {
            return false;
        }

        if (portal.GetComponent<OffspringPortalMarker>() != null)
        {
            return true;
        }

        if (IsOffspringPiece(portal.gameObject))
        {
            return true;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
        {
            return false;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo.GetPrefab() == PrefabNames.OffspringPortal.GetStableHashCode())
        {
            return true;
        }

        return !string.IsNullOrEmpty(zdo.GetString(ZdoFields.PortalRole, string.Empty));
    }

    public static bool IsOffspringPortal(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        if (gameObject.GetComponent<OffspringPortalMarker>() != null)
        {
            return true;
        }

        if (IsOffspringPiece(gameObject))
        {
            return true;
        }

        return IsOffspringPortal(gameObject.GetComponent<TeleportWorld>());
    }

    public static void EnsureIdentity(TeleportWorld portal)
    {
        if (portal == null || !IsOffspringPiece(portal.gameObject))
        {
            return;
        }

        if (portal.GetComponent<OffspringPortalMarker>() == null)
        {
            portal.gameObject.AddComponent<OffspringPortalMarker>();
        }
    }

    private static bool IsOffspringPiece(GameObject gameObject)
    {
        Piece piece = gameObject.GetComponent<Piece>();
        return piece != null && piece.m_name == "$piece_offspring_portal";
    }
}
