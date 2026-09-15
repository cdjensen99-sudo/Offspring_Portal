using System.Collections.Generic;

using UnityEngine;



namespace OffspringPortal;



public static class PortalRpc

{

    private const string RpcSetConfig = "OffspringPortal_SetConfigV4";

    private const float ConfigRetrySeconds = 10f;



    private static readonly List<PendingPortalConfig> PendingConfigs = new List<PendingPortalConfig>();

    private static bool registered;



    private struct PendingPortalConfig

    {

        public ZDOID PortalId;

        public PortalRole Role;

        public string SpeciesKey;

        public string Name;

        public AdultDestination AdultDestination;

        public float ExpireTime;

    }



    public static void Register()

    {

        if (registered || ZRoutedRpc.instance == null)

        {

            return;

        }



        ZRoutedRpc.instance.Register<ZDOID, string, string, string, string>(RpcSetConfig, OnSetConfigRpc);

        registered = true;

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

        if (ZNet.instance == null)

        {

            return;

        }



        if (ZNet.instance.IsServer())

        {

            ApplyPortalConfig(portalId, role, resolvedSpeciesKey, sanitizedName, resolvedDestination);

            return;

        }



        if (ZRoutedRpc.instance == null)

        {

            return;

        }



        ZRoutedRpc.instance.InvokeRoutedRPC(

            RpcSetConfig,

            portalId,

            PortalRoleCatalog.ToStorageValue(role),

            resolvedSpeciesKey ?? string.Empty,

            sanitizedName,

            PortalRoleCatalog.ToStorageValue(resolvedDestination));

        ApplyLocalRegistryUpdate(portalId, role, resolvedSpeciesKey, resolvedDestination);

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



    public static void ProcessPendingConfigs()

    {

        if (ZNet.instance == null || ZDOMan.instance == null || !ZNet.instance.IsServer() || PendingConfigs.Count == 0)

        {

            return;

        }



        float now = Time.time;

        for (int i = PendingConfigs.Count - 1; i >= 0; i--)

        {

            PendingPortalConfig pending = PendingConfigs[i];

            if (now > pending.ExpireTime)

            {

                PendingConfigs.RemoveAt(i);

                continue;

            }



            ZDO zdo = ZDOMan.instance?.GetZDO(pending.PortalId);

            if (zdo == null)

            {

                continue;

            }



            ApplyPortalConfig(

                pending.PortalId,

                pending.Role,

                pending.SpeciesKey,

                pending.Name,

                pending.AdultDestination);

            PendingConfigs.RemoveAt(i);

        }

    }



    private static void OnSetConfigRpc(

        long sender,

        ZDOID portalId,

        string roleValue,

        string speciesValue,

        string name,

        string adultDestinationValue)

    {

        if (ZNet.instance == null || !ZNet.instance.IsServer())

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

        if (ZDOMan.instance == null)

        {

            QueuePendingConfig(portalId, role, speciesKey, name, adultDestination);

            return;

        }



        ZDO zdo = ZDOMan.instance.GetZDO(portalId);

        if (zdo == null)

        {

            QueuePendingConfig(portalId, role, speciesKey, name, adultDestination);

            return;

        }



        string resolvedSpeciesKey = PortalRoleCatalog.ResolveSpeciesKey(role, speciesKey);

        AdultDestination resolvedDestination = PortalRoleCatalog.ResolveAdultDestination(role, adultDestination);



        zdo.Set(ZdoFields.PortalRole, PortalRoleCatalog.ToStorageValue(role));

        zdo.Set(ZdoFields.DeclaredSpecies, resolvedSpeciesKey ?? string.Empty);

        zdo.Set(ZdoFields.PortalName, name);

        zdo.Set(ZdoFields.AdultDestination, PortalRoleCatalog.ToStorageValue(resolvedDestination));

        zdo.Set(ZdoFields.ForwardAdults, resolvedDestination == AdultDestination.Cull);

        DestinationRegistry.RegisterOrUpdate(portalId, zdo.GetPosition(), role, resolvedSpeciesKey, resolvedDestination);

        BreedableSpeciesRegistry.RefreshFromAllBreeders();

        DestinationRegistry.RefreshCapWarnings();



        OPTeleportWorld portal = FindPortal(portalId);

        if (portal != null && Player.m_localPlayer != null)

        {

            string portalName = string.IsNullOrEmpty(name) ? PortalDisplayHelper.UnnamedDisplay : name;

            string config = PortalRoleCatalog.GetConfiguredMessage(role, resolvedSpeciesKey, resolvedDestination);

            Player.m_localPlayer.Message(

                MessageHud.MessageType.TopLeft,

                $"Portal \"{portalName}\" configured as {config}.");

        }

    }



    private static void QueuePendingConfig(

        ZDOID portalId,

        PortalRole role,

        string speciesKey,

        string name,

        AdultDestination adultDestination)

    {

        for (int i = PendingConfigs.Count - 1; i >= 0; i--)

        {

            if (PendingConfigs[i].PortalId == portalId)

            {

                PendingConfigs.RemoveAt(i);

            }

        }



        PendingConfigs.Add(new PendingPortalConfig

        {

            PortalId = portalId,

            Role = role,

            SpeciesKey = speciesKey ?? string.Empty,

            Name = name ?? string.Empty,

            AdultDestination = adultDestination,

            ExpireTime = Time.time + ConfigRetrySeconds

        });

    }



    private static void ApplyLocalRegistryUpdate(

        ZDOID portalId,

        PortalRole role,

        string speciesKey,

        AdultDestination adultDestination)

    {

        Vector3 position = Vector3.zero;

        if (ZDOMan.instance != null)

        {

            ZDO zdo = ZDOMan.instance.GetZDO(portalId);

            if (zdo != null)

            {

                position = zdo.GetPosition();

            }

        }



        DestinationRegistry.RegisterOrUpdate(portalId, position, role, speciesKey, adultDestination);

        BreedableSpeciesRegistry.RefreshFromAllBreeders();

    }



    private static OPTeleportWorld FindPortal(ZDOID portalId)

    {

        if (ZDOMan.instance == null || ZNetScene.instance == null)

        {

            return null;

        }



        ZDO zdo = ZDOMan.instance.GetZDO(portalId);

        if (zdo == null)

        {

            return null;

        }



        ZNetView nview = ZNetScene.instance.FindInstance(zdo);

        return nview != null ? nview.GetComponent<OPTeleportWorld>() : null;

    }

}

