using UnityEngine;

namespace OffspringPortal;

public static class PortalConsoleCommands
{
    private static bool registered;

    public static void Register()
    {
        if (registered || !ModConfig.EnablePortalConsoleCommands.Value)
        {
            return;
        }

        _ = new Terminal.ConsoleCommand(
            "opscan",
            "Scan for orphan offspring_portal ZDOs. Usage: opscan [here|origin] [radius]",
            OnOpscan,
            onlyAdmin: ModConfig.ConsoleCommandsRequireAdmin.Value);

        _ = new Terminal.ConsoleCommand(
            "opclean",
            "Remove orphan offspring_portal ZDOs. Usage: opclean [dryrun] [here|origin|all] [radius]",
            OnOpclean,
            onlyAdmin: ModConfig.ConsoleCommandsRequireAdmin.Value);

        registered = true;
        OPLog.Info("[OP] Registered console commands: opscan, opclean.");
    }

    private static void OnOpscan(Terminal.ConsoleEventArgs args)
    {
        if (!CanRunScanCommands(args))
        {
            return;
        }

        if (ZNet.instance != null && !ZNet.instance.IsServer())
        {
            args.Context.AddString("[OP] Client scan only sees loaded zones; server opscan is authoritative.");
        }

        ParseScope(args, out bool entireWorld, out float radius, out Vector3 center);
        var matches = PortalZdoMaintenance.Scan(radius, center, entireWorld);
        if (matches.Count == 0)
        {
            args.Context.AddString("[OP] No orphan offspring_portal ZDOs matched cleanup rules.");
            return;
        }

        args.Context.AddString($"[OP] Found {matches.Count} orphan offspring_portal ZDO(s):");
        int shown = 0;
        foreach (PortalMaintenanceEntry entry in matches)
        {
            if (shown >= 40)
            {
                args.Context.AddString("[OP] ...see BepInEx LogOutput.log for additional entries.");
                break;
            }

            args.Context.AddString(
                $"[OP] {ZdoIdUtility.Format(entry.Id)} at ({entry.Position.x:F1}, {entry.Position.y:F1}, {entry.Position.z:F1}) — {entry.Reason}");
            shown++;
        }
    }

    private static void OnOpclean(Terminal.ConsoleEventArgs args)
    {
        if (!CanRunMaintenanceCommands(args))
        {
            return;
        }

        bool dryRun = args.HasArgumentAnywhere("dryrun");
        ParseScope(args, out bool entireWorld, out float radius, out Vector3 center);
        var matches = PortalZdoMaintenance.Scan(radius, center, entireWorld);
        PortalZdoMaintenance.Clean(matches, dryRun, out string summary);
        args.Context.AddString(summary);
    }

    private static bool CanRunScanCommands(Terminal.ConsoleEventArgs args)
    {
        if (ZNet.instance == null || ZDOMan.instance == null)
        {
            args.Context.AddString("[OP] World is not ready yet.");
            return false;
        }

        if (ModConfig.ConsoleCommandsRequireAdmin.Value
            && ZNet.instance != null
            && !ZNet.instance.LocalPlayerIsAdminOrHost())
        {
            args.Context.AddString(
                "[OP] Admin or host required (add your Steam ID to adminlist.txt on dedicated servers; devcommands alone is not enough).");
            return false;
        }

        if (ModConfig.ConsoleCommandsServerOnly.Value
            && !ModConfig.ConsoleCommandsAllowClientScan.Value
            && ZNet.instance != null
            && !ZNet.instance.IsServer())
        {
            args.Context.AddString("[OP] Run opscan on the server instance (or set ConsoleCommandsAllowClientScan=true).");
            return false;
        }

        return true;
    }

    private static bool CanRunMaintenanceCommands(Terminal.ConsoleEventArgs args)
    {
        if (!CanRunScanCommands(args))
        {
            return false;
        }

        if (ModConfig.ConsoleCommandsServerOnly.Value
            && ZNet.instance != null
            && !ZNet.instance.IsServer())
        {
            args.Context.AddString("[OP] Run opclean on the server instance (or enable ConsoleCommandsServerOnly=false).");
            return false;
        }

        return true;
    }

    private static void ParseScope(
        Terminal.ConsoleEventArgs args,
        out bool entireWorld,
        out float radius,
        out Vector3 center)
    {
        entireWorld = args.HasArgumentAnywhere("all");
        radius = ModConfig.SpawnCleanupRadiusMeters.Value;
        center = Vector3.zero;

        if (args.HasArgumentAnywhere("here") && Player.m_localPlayer != null)
        {
            center = Player.m_localPlayer.transform.position;
        }
        else if (args.HasArgumentAnywhere("origin"))
        {
            center = Vector3.zero;
        }

        for (int i = 1; i < args.Length; i++)
        {
            if (float.TryParse(args[i], out float parsedRadius) && parsedRadius > 0f)
            {
                radius = parsedRadius;
                break;
            }
        }
    }
}
