using System;
using System.Collections;
using Jotunn.Managers;
using UnityEngine;

namespace OffspringPortal;

public static class OffspringPortalPrefabs
{
    public static GameObject Prefab { get; private set; }
    public static bool Registered { get; private set; }
    private static bool pieceRegistered;
    private static bool retryScheduled;

    public static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefab;
        PrefabManager.OnPrefabsRegistered += RegisterPrefab;
        PieceManager.OnPiecesRegistered += RegisterPiece;
        PrefabManager.OnPrefabsRegistered += RegisterPiece;
    }

    public static void EnsureRegistered()
    {
        RegisterPrefab();
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

        try
        {
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(
                PrefabNames.OffspringPortal,
                PrefabNames.VanillaPortalWood);

            if (clone == null)
            {
                ScheduleRegisterRetry();
                return;
            }

            ConfigureClone(clone);
            OPTeleportWorld portal = ConvertFromVanillaPortal(clone);
            EnsureRuntimeTriggers(portal);

            PrefabManager.Instance.AddPrefab(clone);
            Prefab = clone;
            Registered = true;
            retryScheduled = false;

            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefab;
            PrefabManager.OnPrefabsRegistered -= RegisterPrefab;
            OffspringPortalPlugin.Log.LogInfo(
                $"Registered offspring_portal prefab (cloned from {PrefabNames.VanillaPortalWood}).");

            RegisterPiece();
        }
        catch (Exception ex)
        {
            OffspringPortalPlugin.Log.LogError($"Failed to register offspring_portal prefab: {ex}");
            ScheduleRegisterRetry();
        }
    }

    private static void ScheduleRegisterRetry()
    {
        if (retryScheduled || Registered)
        {
            return;
        }

        retryScheduled = true;
        OffspringPortalRuntime.Instance.StartCoroutine(RetryRegisterPrefab());
    }

    private static IEnumerator RetryRegisterPrefab()
    {
        const int maxFrames = 600;

        for (int i = 0; i < maxFrames && !Registered; i++)
        {
            RegisterPrefab();
            if (Registered)
            {
                yield break;
            }

            yield return null;
        }

        retryScheduled = false;

        if (!Registered)
        {
            OffspringPortalPlugin.Log.LogError(
                $"Could not register offspring_portal: {PrefabNames.VanillaPortalWood} was unavailable after waiting for ZNetScene.");
        }
    }

    internal static bool TryValidateVanillaPortalPrefab(out string message)
    {
        message = null;

        if (!TryResolveVanillaPortalWood(out GameObject vanilla))
        {
            message =
                $"{PrefabNames.VanillaPortalWood} was not found in ZNetScene at registration time. " +
                "Offspring Portal cannot clone the vanilla wood portal.";
            return false;
        }

        if (vanilla.GetComponent<TeleportWorld>() == null)
        {
            message =
                $"{PrefabNames.VanillaPortalWood} exists but has no TeleportWorld component.";
            return false;
        }

        return true;
    }

    internal static bool TryResolveVanillaPortalWood(out GameObject vanilla)
    {
        vanilla = null;

        if (ZNetScene.instance != null)
        {
            vanilla = ZNetScene.instance.GetPrefab(PrefabNames.VanillaPortalWood);
        }

        return vanilla != null;
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
            ApplyHammerIcon(Prefab);
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
        float hoverOffset = 0f;
        EffectList connectedVfx = null;
        MeshRenderer model = null;
        Color colorUnconnected = Color.white;
        Color colorTargetfound = Color.white;

        TeleportWorld legacyPortal = gameObject.GetComponent<TeleportWorld>();
        if (legacyPortal != null)
        {
            proximityRoot = legacyPortal.m_proximityRoot;
            exitDistance = legacyPortal.m_exitDistance;
            hoverOffset = legacyPortal.m_hoverOffset;
            connectedVfx = legacyPortal.m_connected;
            model = legacyPortal.m_model;
            colorUnconnected = legacyPortal.m_colorUnconnected;
            colorTargetfound = legacyPortal.m_colorTargetfound;
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

        portal.Initialize(
            proximityRoot,
            exitDistance,
            connectedVfx,
            hoverOffset,
            model,
            colorUnconnected,
            colorTargetfound);
        return portal;
    }

    private static void ApplyHammerIcon(GameObject prefab)
    {
        Piece piece = prefab?.GetComponent<Piece>();
        if (piece == null)
        {
            return;
        }

        try
        {
            Sprite sprite = PortalHammerIcon.Create(prefab);
            if (sprite != null)
            {
                piece.m_icon = sprite;
            }
        }
        catch (System.Exception ex)
        {
            OffspringPortalPlugin.Log.LogWarning($"Could not render offspring portal hammer icon: {ex.Message}");
        }
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
