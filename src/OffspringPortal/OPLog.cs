using System;
using BepInEx.Logging;

namespace OffspringPortal;

/// <summary>
/// Centralized BepInEx logging with server/client role prefixes.
/// </summary>
internal static class OPLog
{
    private static ManualLogSource Source => OffspringPortalPlugin.Log;

    private static string Prefix
    {
        get
        {
            try
            {
                if (ZNet.instance != null)
                {
                    return ZNet.instance.IsServer()
                        ? "[OffspringPortal][Server]"
                        : "[OffspringPortal][Client]";
                }
            }
            catch
            {
                // Logging must never become the reason the mod fails.
            }

            return "[OffspringPortal][Startup]";
        }
    }

    public static void Debug(string message)
    {
        if (Source == null)
        {
            return;
        }

        Source.LogDebug($"{Prefix} {message}");

        if (ModConfig.VerboseLogging?.Value == true)
        {
            Source.LogInfo($"{Prefix} [DEBUG] {message}");
        }
    }

    public static void Info(string message) => Source?.LogInfo($"{Prefix} {message}");
    public static void Warning(string message) => Source?.LogWarning($"{Prefix} {message}");
    public static void Error(string message) => Source?.LogError($"{Prefix} {message}");
    public static void Exception(string context, Exception exception) =>
        Source?.LogError($"{Prefix} {context}: {exception}");
}
