using OffspringPortal.UI;

using UnityEngine;



namespace OffspringPortal;



/// <summary>

/// Automation-only portal piece. Not a TeleportWorld — invisible to travel-mod discovery.

/// Valheim 1.0 Hoverable requires GetHoverOffset(); UseHoverMarker() is not part of the interface.

/// </summary>

public sealed class OPTeleportWorld : MonoBehaviour, Hoverable, Interactable

{

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");



    public Transform m_proximityRoot;

    public float m_exitDistance = PortalPlacement.ScaledExitDistance;

    public float m_hoverOffset;

    public EffectList m_connected;

    public MeshRenderer m_model;

    public Color m_colorUnconnected = Color.white;

    public Color m_colorTargetfound = Color.white;



    private ZNetView nview;

    private Material emissionMaterial;

    private bool supportsEmission;

    private bool hadRoutingReady;

    private float colorAlpha;



    private void Awake()

    {

        nview = GetComponent<ZNetView>();

        CacheEmissionMaterial();

    }



    private void Start()

    {

        OffspringPortalPrefabs.EnsureIdentity(this);

        PortalHelper.EnsurePortalInitialized(this);

        if (ZNet.instance != null && ZNet.instance.IsServer())

        {

            PortalHelper.SyncRegistryFromPortal(this);

        }



        InvokeRepeating(nameof(UpdateConnectionVisual), 0.5f, 0.5f);

    }



    internal void Initialize(

        Transform proximityRoot,

        float exitDistance,

        EffectList connectedVfx,

        float hoverOffset,

        MeshRenderer model,

        Color colorUnconnected,

        Color colorTargetfound)

    {

        m_proximityRoot = proximityRoot;

        m_exitDistance = exitDistance;

        m_connected = connectedVfx;

        m_hoverOffset = hoverOffset;

        m_model = model;

        m_colorUnconnected = colorUnconnected;

        m_colorTargetfound = colorTargetfound;

        CacheEmissionMaterial();

    }



    private void CacheEmissionMaterial()

    {

        if (m_model == null)

        {

            emissionMaterial = null;

            supportsEmission = false;

            return;

        }



        emissionMaterial = m_model.material;

        supportsEmission = emissionMaterial != null && emissionMaterial.HasProperty(EmissionColorId);

    }



    public Vector3 GetExitPosition()

    {

        return transform.position + transform.rotation * Vector3.forward * m_exitDistance;

    }



    public void PlayActivationEffect()

    {

        if (m_connected == null)

        {

            return;

        }



        try

        {

            m_connected.Create(transform.position, transform.rotation, transform);

        }

        catch (System.Exception ex)

        {

            OffspringPortalPlugin.Log.LogWarning($"Portal activation effect failed: {ex.Message}");

        }

    }



    public string GetHoverText()

    {

        ZDO zdo = nview?.GetZDO();

        if (zdo == null)

        {

            return string.Empty;

        }



        PortalRole role = PortalRoleCatalog.FromZdo(zdo);

        string speciesKey = zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty);

        AdultDestination adultDestination = PortalRoleCatalog.GetAdultDestination(zdo);

        bool capWarning = zdo.GetBool(ZdoFields.CapWarning);

        return PortalDisplayHelper.GetHoverText(zdo, role, speciesKey, adultDestination, capWarning);

    }



    public string GetHoverName()

    {

        ZDO zdo = nview?.GetZDO();

        if (zdo == null)

        {

            return string.Empty;

        }



        string portalName = zdo.GetString(ZdoFields.PortalName, string.Empty);

        return string.IsNullOrEmpty(portalName) ? PortalDisplayHelper.UnnamedDisplay : portalName;

    }



    public float GetHoverOffset()

    {

        return m_hoverOffset;

    }



    public bool Interact(Humanoid human, bool hold, bool alt)

    {

        if (hold || alt)

        {

            return false;

        }



        if (!PrivateArea.CheckAccess(transform.position))

        {

            human.Message(MessageHud.MessageType.Center, "$piece_noaccess");

            return true;

        }



        ZDO zdo = nview?.GetZDO();

        if (zdo == null)

        {

            return false;

        }



        string speciesKey = zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty);

        PortalRole role = PortalRoleCatalog.FromZdo(zdo);

        string portalName = zdo.GetString(ZdoFields.PortalName, string.Empty);

        AdultDestination adultDestination = PortalRoleCatalog.GetAdultDestination(zdo);

        SpeciesConfigPanel.Instance.Open(zdo.m_uid, role, speciesKey, portalName, adultDestination);

        return true;

    }



    public bool UseItem(Humanoid user, ItemDrop.ItemData item)

    {

        return false;

    }



    private void Update()

    {

        if (!supportsEmission || emissionMaterial == null)

        {

            return;

        }



        colorAlpha = Mathf.MoveTowards(colorAlpha, hadRoutingReady ? 1f : 0f, Time.deltaTime);

        emissionMaterial.SetColor(EmissionColorId, Color.Lerp(m_colorUnconnected, m_colorTargetfound, colorAlpha));

    }



    private void UpdateConnectionVisual()

    {

        if (!PortalHelper.IsWorldReady())

        {

            return;

        }



        ZDO zdo = nview?.GetZDO();

        if (zdo == null)

        {

            return;

        }



        PortalHelper.SyncRegistryFromZdo(zdo);
        PortalRole role = PortalRoleCatalog.FromZdo(zdo);
        string speciesKey = zdo.GetString(ZdoFields.DeclaredSpecies, string.Empty);
        AdultDestination adultDestination = PortalRoleCatalog.GetAdultDestination(zdo);
        bool routingReady = PortalConnectionStatus.IsRoutingReady(role, speciesKey, adultDestination);

        if (routingReady && !hadRoutingReady)

        {

            PlayActivationEffect();

        }



        hadRoutingReady = routingReady;

    }

}

