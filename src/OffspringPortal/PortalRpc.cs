using UnityEngine;

namespace OffspringPortal;

public static class PortalRpc
{
    private const string RpcSetConfig = "OffspringPortal_SetConfigV4";

    public static void Register()
    {
        ZRoutedRpc.instance.Register<ZDOID, string, string, string, string>(RpcSetConfig, OnSetConfigRpc);
    }

    public static void SetPortalConfig(
        ZDOID portalId,
        PortalRole role,
        string speciesKey,
        string name,
        AdultDestination adultDestination)
    {
        string sanitizedName = PortalDisplayHelper.SanitizeName(name);
        string resolvedSpeciesKey = PortalRoleCatalog.ResolveSpeciesKey(role, speciesKey);
        AdultDestination resolvedDestination = PortalRoleCatalog.ResolveAdultDestination(role, adultDestination);
        if (ZNet.instance.IsServer())
        {
            ApplyPortalConfig(portalId, role, resolvedSpeciesKey, sanitizedName, resolvedDestination);
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(
            RpcSetConfig,
            portalId,
            PortalRoleCatalog.ToStorageValue(role),
            resolvedSpeciesKey ?? string.Empty,
            sanitizedName,
            PortalRoleCatalog.ToStorageValue(resolvedDestination));
    }

    public static void SetPortalConfig(
        ZDOID portalId,
        PortalRole role,
        SpeciesType species,
        string name,
        AdultDestination adultDestination)
    {
        string speciesKey = species == SpeciesType.None
            ? string.Empty
            : SpeciesCatalog.ToStorageValue(species);
        SetPortalConfig(portalId, role, speciesKey, name, adultDestination);
    }

    private static void OnSetConfigRpc(
        long sender,
        ZDOID portalId,
        string roleValue,
        string speciesValue,
        string name,
        string adultDestinationValue)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        PortalRole role = System.Enum.TryParse(roleValue, ignoreCase: true, out PortalRole parsedRole)
            ? parsedRole
            : PortalRole.Breeder;
        AdultDestination adultDestination = System.Enum.TryParse(
            adultDestinationValue,
            ignoreCase: true,
            out AdultDestination parsedDestination)
            ? parsedDestination
            : AdultDestination.None;

        ApplyPortalConfig(
            portalId,
            role,
            speciesValue,
            PortalDisplayHelper.SanitizeName(name),
            adultDestination);
    }

    private static void ApplyPortalConfig(
        ZDOID portalId,
        PortalRole role,
        string speciesKey,
        string name,
        AdultDestination adultDestination)
    {
        ZDO zdo = ZDOMan.instance.GetZDO(portalId);
        if (zdo == null)
        {
            return;
        }

        string resolvedSpeciesKey = PortalRoleCatalog.ResolveSpeciesKey(role, speciesKey);
        AdultDestination resolvedDestination = PortalRoleCatalog.ResolveAdultDestination(role, adultDestination);

        zdo.Set(ZdoFields.PortalRole, PortalRoleCatalog.ToStorageValue(role));
        zdo.Set(ZdoFields.DeclaredSpecies, resolvedSpeciesKey ?? string.Empty);
        zdo.Set(ZdoFields.PortalName, name);
        zdo.Set(ZdoFields.AdultDestination, PortalRoleCatalog.ToStorageValue(resolvedDestination));
        zdo.Set(ZdoFields.ForwardAdults, resolvedDestination == AdultDestination.Cull);
        PortalTravelGuard.ClearTravelBindings(zdo);
        DestinationRegistry.RegisterOrUpdate(portalId, zdo.GetPosition(), role, resolvedSpeciesKey, resolvedDestination);
        BreedableSpeciesRegistry.RefreshFromAllBreeders();
        DestinationRegistry.RefreshCapWarnings();

        TeleportWorld portal = FindPortal(portalId);
        if (portal != null && Player.m_localPlayer != null)
        {
            string portalName = string.IsNullOrEmpty(name) ? PortalDisplayHelper.UnnamedDisplay : name;
            string config = PortalRoleCatalog.GetConfiguredMessage(role, resolvedSpeciesKey, resolvedDestination);
            Player.m_localPlayer.Message(
                MessageHud.MessageType.TopLeft,
                $"Portal \"{portalName}\" configured as {config}.");
        }
    }

    private static TeleportWorld FindPortal(ZDOID portalId)
    {
        ZDO zdo = ZDOMan.instance.GetZDO(portalId);
        if (zdo == null)
        {
            return null;
        }

        ZNetView nview = ZNetScene.instance.FindInstance(zdo);
        return nview != null ? nview.GetComponent<TeleportWorld>() : null;
    }
}
