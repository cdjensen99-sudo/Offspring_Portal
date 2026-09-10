using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using UnityEngine;

namespace OffspringPortal;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
public sealed class OffspringPortalPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "offspringportal.mod";
    public const string PluginName = "Offspring Portal";
    public const string PluginVersion = "1.0.0";

    internal static ManualLogSource Log;
    private Harmony harmony;

    private void Awake()
    {
        Log = Logger;
        ModConfig.Bind(Config);
        OffspringPortalPrefabs.Initialize();
        _ = OffspringPortalRuntime.Instance;
        harmony = new Harmony(PluginGuid);
        harmony.PatchAll(typeof(OffspringPortalPlugin).Assembly);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }
}
