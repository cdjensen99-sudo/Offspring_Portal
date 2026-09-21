using System.Text;
using UnityEngine;

namespace OffspringPortal;

public static class DiagnosticLog
{
    public static void Info(string message) => OPLog.Info(message);

    public static void Warning(string message) => OPLog.Warning(message);

    public static void Verbose(string message)
    {
        if (ModConfig.VerboseLogging.Value)
        {
            OPLog.Debug(message);
        }
    }

    public static bool ShouldLogRateLimited(ref float lastLogTime, float intervalSeconds)
    {
        float now = Time.time;
        if (now - lastLogTime < intervalSeconds)
        {
            return false;
        }

        lastLogTime = now;
        return true;
    }

    public static void LogPortalRegistry(string context, bool includeAllPortals = true)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Portal registry snapshot (").Append(context).Append("): ");
        builder.Append(DestinationRegistry.BuildSummaryCounts());
        Info(builder.ToString());

        if (!includeAllPortals)
        {
            return;
        }

        foreach (PortalRecord record in DestinationRegistry.GetAll())
        {
            Info(DestinationRegistry.FormatPortalRecord(record));
        }
    }

    public static void LogNetworkContext(string context)
    {
        if (ZNet.instance == null)
        {
            Verbose($"{context}: ZNet not ready.");
            return;
        }

        string role = ZNet.instance.IsServer()
            ? (ZNet.instance.IsDedicated() ? "dedicated server" : "listen server")
            : "client";
        Verbose($"{context}: role={role}, peers={ZNet.instance.GetNrOfPlayers()}.");
    }
}
