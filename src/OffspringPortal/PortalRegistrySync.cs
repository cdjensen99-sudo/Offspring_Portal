using System.Globalization;
using System.Text;
using UnityEngine;

namespace OffspringPortal;

public static class PortalRegistrySync
{
    private const string RpcSyncRegistry = "OffspringPortal_SyncRegistry";
    private const string RpcRequestRegistry = "OffspringPortal_RequestRegistry";
    private const string RpcServerRebuildRegistry = "OffspringPortal_ServerRebuildRegistry";
    private const char PortalSeparator = ';';
    private const char FieldSeparator = '|';

    private static bool registered;

    public static void Register()
    {
        if (registered || ZRoutedRpc.instance == null)
        {
            return;
        }

        ZRoutedRpc instance = ZRoutedRpc.instance;
        instance.Register<string>(RpcSyncRegistry, OnSyncRegistry);
        instance.Register(RpcRequestRegistry, OnRequestRegistry);
        instance.Register(RpcServerRebuildRegistry, OnServerRebuildRegistry);
        instance.m_onNewPeer += OnNewPeer;
        registered = true;
    }

    public static void RequestServerRebuildAndBroadcast()
    {
        if (ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(RpcServerRebuildRegistry);
    }

    public static void RequestIfClient()
    {
        if (ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
        {
            return;
        }

        DiagnosticLog.Verbose("Requesting portal registry snapshot from server.");
        ZRoutedRpc.instance.InvokeRoutedRPC(RpcRequestRegistry);
    }

    public static void BroadcastFromServer()
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
        {
            return;
        }

        string payload = BuildPayload();
        int portalCount = CountPayloadEntries(payload);
        DiagnosticLog.Info($"Broadcasting portal registry to all peers: {portalCount} portal(s).");
        ZRoutedRpc.instance.InvokeRoutedRPC(RpcSyncRegistry, payload);
    }

    private static void OnNewPeer(long peerId)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
        {
            return;
        }

        PortalZdoGuard.PurgeSpawnOrphansOnServer($"peer_connect:{peerId}", requestWorldSave: true);

        string payload = BuildPayload();
        DiagnosticLog.Verbose(
            $"Sending portal registry snapshot to new peer {peerId}: {CountPayloadEntries(payload)} portal(s).");
        ZRoutedRpc.instance.InvokeRoutedRPC(peerId, RpcSyncRegistry, payload);
    }

    private static void OnRequestRegistry(long sender)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
        {
            return;
        }

        string payload = BuildPayload();
        DiagnosticLog.Verbose(
            $"Sending portal registry snapshot to peer {sender}: {CountPayloadEntries(payload)} portal(s).");
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RpcSyncRegistry, payload);
    }

    private static void OnServerRebuildRegistry(long sender)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
        {
            return;
        }

        PortalHelper.SyncRegistryFromAllPortalZdos();
        BreedableSpeciesRegistry.RefreshFromAllBreeders();
        DestinationRegistry.RefreshCapWarnings();
        BroadcastFromServer();
    }

    private static void OnSyncRegistry(long sender, string payload)
    {
        if (ZNet.instance == null || ZNet.instance.IsServer())
        {
            return;
        }

        ApplyPayload(payload);
    }

    private static string BuildPayload()
    {
        PortalHelper.SyncRegistryFromAllPortalZdos();

        StringBuilder builder = new StringBuilder();
        foreach (PortalRecord record in DestinationRegistry.GetAll())
        {
            if (!DestinationRegistry.IsLivePortalRecord(record))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(PortalSeparator);
            }

            builder.Append(ZdoIdUtility.Format(record.Id));
            builder.Append(FieldSeparator);
            builder.Append(PortalRoleCatalog.ToStorageValue(record.Role));
            builder.Append(FieldSeparator);
            builder.Append(record.DeclaredSpeciesKey ?? string.Empty);
            builder.Append(FieldSeparator);
            builder.Append(PortalRoleCatalog.ToStorageValue(record.AdultDestination));
            builder.Append(FieldSeparator);
            builder.Append(record.Position.x.ToString(CultureInfo.InvariantCulture));
            builder.Append(FieldSeparator);
            builder.Append(record.Position.y.ToString(CultureInfo.InvariantCulture));
            builder.Append(FieldSeparator);
            builder.Append(record.Position.z.ToString(CultureInfo.InvariantCulture));
            builder.Append(FieldSeparator);
            builder.Append(record.Rotation.eulerAngles.y.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static void ApplyPayload(string payload)
    {
        DestinationRegistry.Clear();
        if (string.IsNullOrEmpty(payload))
        {
            DiagnosticLog.Warning(
                "Portal registry sync received empty payload from server. No portals are registered on this client.");
            BreedableSpeciesRegistry.RefreshFromAllBreeders();
            return;
        }

        int portalCount = 0;
        string[] entries = payload.Split(PortalSeparator);
        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            if (!TryParseEntry(entry, out PortalRecord record))
            {
                continue;
            }

            DestinationRegistry.RegisterOrUpdate(
                record.Id,
                record.Position,
                record.Role,
                record.DeclaredSpeciesKey,
                record.AdultDestination);
            PortalRecord stored = DestinationRegistry.Get(record.Id);
            if (stored != null)
            {
                stored.Rotation = record.Rotation;
            }

            portalCount++;
        }

        PortalHelper.MergeRegistryFromLoadedPortals();
        BreedableSpeciesRegistry.RefreshFromAllBreeders();
        DiagnosticLog.Info($"Portal registry synced from server: {portalCount} portal(s).");
        DiagnosticLog.LogPortalRegistry("client sync from server");
    }

    private static int CountPayloadEntries(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return 0;
        }

        int count = 1;
        for (int i = 0; i < payload.Length; i++)
        {
            if (payload[i] == PortalSeparator)
            {
                count++;
            }
        }

        return count;
    }

    private static bool TryParseEntry(string entry, out PortalRecord record)
    {
        record = null;
        string[] fields = entry.Split(FieldSeparator);
        if (fields.Length < 8)
        {
            return false;
        }

        if (!TryParseZdoId(fields[0], out ZDOID portalId))
        {
            return false;
        }

        PortalRole role = System.Enum.TryParse(fields[1], ignoreCase: true, out PortalRole parsedRole)
            ? parsedRole
            : PortalRole.Breeder;
        string speciesKey = fields[2] ?? string.Empty;
        AdultDestination adultDestination = System.Enum.TryParse(fields[3], ignoreCase: true, out AdultDestination parsedDestination)
            ? parsedDestination
            : AdultDestination.None;
        if (!TryParseFloat(fields[4], out float x)
            || !TryParseFloat(fields[5], out float y)
            || !TryParseFloat(fields[6], out float z)
            || !TryParseFloat(fields[7], out float rotationY))
        {
            return false;
        }

        record = new PortalRecord
        {
            Id = portalId,
            Role = role,
            DeclaredSpeciesKey = speciesKey,
            DeclaredSpecies = SpeciesCatalog.FromStorageValue(speciesKey),
            AdultDestination = adultDestination,
            Position = new Vector3(x, y, z),
            Rotation = Quaternion.Euler(0f, rotationY, 0f),
        };
        return true;
    }

    private static bool TryParseZdoId(string text, out ZDOID id)
    {
        id = ZDOID.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        int separator = text.IndexOf(':');
        if (separator <= 0 || separator >= text.Length - 1)
        {
            return false;
        }

        if (!long.TryParse(text.Substring(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out long userId))
        {
            return false;
        }

        if (!uint.TryParse(text.Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint zdoId))
        {
            return false;
        }

        id = new ZDOID(userId, zdoId);
        return !id.IsNone();
    }

    private static bool TryParseFloat(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
