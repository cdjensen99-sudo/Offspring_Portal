using OffspringPortal.UI;
using UnityEngine;

namespace OffspringPortal;

/// <summary>
/// Automation-only portal piece. Not a TeleportWorld — invisible to travel-mod discovery.
/// Valheim 1.0 Hoverable requires GetHoverOffset(); UseHoverMarker() is not part of the interface.
/// </summary>
public sealed class OPTeleportWorld : MonoBehaviour, Hoverable, Interactable
{
    public Transform m_proximityRoot;
    public float m_exitDistance = PortalPlacement.ScaledExitDistance;
    /// <summary>Copied from vanilla portal_wood when cloned; 0f is fine for custom pieces.</summary>
    public float m_hoverOffset;
    public EffectList m_connected;

    private ZNetView nview;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
    }

    private void Start()
    {
        OffspringPortalPrefabs.EnsureIdentity(this);
        PortalHelper.EnsurePortalInitialized(this);
    }

    internal void Initialize(Transform proximityRoot, float exitDistance, EffectList connectedVfx, float hoverOffset)
    {
        m_proximityRoot = proximityRoot;
        m_exitDistance = exitDistance;
        m_connected = connectedVfx;
        m_hoverOffset = hoverOffset;
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

        m_connected.Create(transform.position, transform.rotation, transform);
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
}
