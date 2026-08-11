using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OffspringPortal.Patches;

[HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
public static class ZNetSceneRemoveObjectsPatch
{
    private static bool Prefix(
        ZNetScene __instance,
        List<ZDO> currentNearObjects,
        List<ZDO> currentDistantObjects)
    {
        byte earmark = (byte)(Time.frameCount & 0xFF);
        foreach (ZDO currentNearObject in currentNearObjects)
        {
            currentNearObject.TempRemoveEarmark = earmark;
        }

        foreach (ZDO currentDistantObject in currentDistantObjects)
        {
            currentDistantObject.TempRemoveEarmark = earmark;
        }

        List<ZNetView> tempRemoved = Traverse.Create(__instance).Field<List<ZNetView>>("m_tempRemoved").Value;
        Dictionary<ZDO, ZNetView> instances =
            Traverse.Create(__instance).Field<Dictionary<ZDO, ZNetView>>("m_instances").Value;

        tempRemoved.Clear();
        foreach (ZNetView view in instances.Values)
        {
            if (view == null)
            {
                continue;
            }

            ZDO zdo = view.GetZDO();
            if (zdo == null || zdo.TempRemoveEarmark != earmark)
            {
                tempRemoved.Add(view);
            }
        }

        for (int i = 0; i < tempRemoved.Count; i++)
        {
            ZNetView view = tempRemoved[i];
            ZDO zdo = view.GetZDO();
            view.ResetZDO();
            Object.Destroy(view.gameObject);

            if (zdo != null)
            {
                if (!zdo.Persistent && zdo.IsOwner())
                {
                    ZDOMan.instance.DestroyZDO(zdo);
                }

                instances.Remove(zdo);
                continue;
            }

            ZDO staleKey = null;
            foreach (KeyValuePair<ZDO, ZNetView> entry in instances)
            {
                if (entry.Value == view)
                {
                    staleKey = entry.Key;
                    break;
                }
            }

            if (staleKey != null)
            {
                instances.Remove(staleKey);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(Game), "Start")]
public static class GameStartPatch
{
    private static void Postfix()
    {
        PortalRpc.Register();
        JuvenileFollowRpc.Register();
        PortalHelper.RebuildRegistryFromWorld();
        OffspringPortalRuntime.Instance.StartCoroutine(DelayedRegistryRebuild());
    }

    private static IEnumerator DelayedRegistryRebuild()
    {
        yield return new WaitForSeconds(2f);
        PortalHelper.RebuildRegistryFromWorld();
    }
}

[HarmonyPatch(typeof(Player), "OnSpawned")]
public static class PlayerSpawnedPatch
{
    private static void Postfix()
    {
        OffspringPortalPrefabs.EnsurePieceRegistered();
        PortalHelper.RebuildRegistryFromWorld();
    }
}

[HarmonyPatch(typeof(WearNTear), "OnPlaced")]
public static class WearNTearOnPlacedPatch
{
    private static void Postfix(WearNTear __instance)
    {
        if (!OffspringPortalPrefabs.IsOffspringPortal(__instance.gameObject))
        {
            return;
        }

        if (!__instance.gameObject.activeSelf)
        {
            __instance.gameObject.SetActive(true);
        }

        OffspringPortalPrefabs.MigrateLegacyPortal(__instance.gameObject);
        OPTeleportWorld portal = __instance.GetComponent<OPTeleportWorld>();
        if (portal != null)
        {
            PortalHelper.EnsurePortalInitialized(portal);
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.TeleportTo))]
public static class CharacterTeleportToPlayerTravelPatch
{
    private static bool Prefix(Character __instance, Vector3 pos, Quaternion rot, bool distantTeleport)
    {
        if (__instance is not Player player)
        {
            return true;
        }

        if (!PortalTravelGuard.BlocksPlayerTeleportTo(pos, player, out string message))
        {
            return true;
        }

        if (player == Player.m_localPlayer)
        {
            player.Message(MessageHud.MessageType.Center, message);
        }

        return false;
    }
}

[HarmonyPatch(typeof(WearNTear), "Destroy")]
public static class WearNTearDestroyPatch
{
    private static void Prefix(WearNTear __instance)
    {
        OPTeleportWorld portal = __instance.GetComponent<OPTeleportWorld>();
        if (portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        ZNetView nview = portal.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();
        if (zdo != null)
        {
            DestinationRegistry.Remove(zdo.m_uid);
        }
    }
}

[HarmonyPatch(typeof(Player), "FindHoverObject")]
public static class PlayerFindHoverObjectPatch
{
    private static void Postfix(Player __instance, ref GameObject hover, ref Character hoverCreature)
    {
        if (!ModConfig.EnableFollowCommand.Value)
        {
            return;
        }

        Character juvenile = JuvenileInteractHelper.FindJuvenileUnderCrosshair(
            __instance, __instance.m_maxInteractDistance);
        if (juvenile == null)
        {
            return;
        }

        hover = juvenile.gameObject;
        hoverCreature = juvenile;
    }
}

[HarmonyPatch(typeof(Player), "Interact")]
[HarmonyPriority(Priority.First)]
public static class PlayerJuvenileInteractPatch
{
    private static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
    {
        if (hold || alt || !ModConfig.EnableFollowCommand.Value)
        {
            return true;
        }

        Character creature = JuvenileInteractHelper.FindJuvenileUnderCrosshair(__instance)
            ?? __instance.GetHoverCreature();
        if (creature == null || !creature.IsTamed() || !SpeciesHelper.IsEligibleJuvenile(creature))
        {
            return true;
        }

        JuvenileGroundSnapper.EnsureAttached(creature);
        JuvenileFollowController.EnsureAttached(creature);

        if (JuvenileFollow.TryCommand(creature, __instance, showMessage: true))
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
[HarmonyPriority(Priority.First)]
public static class TameableInteractPatch
{
    private static void Prefix(Tameable __instance, ref bool ___m_commandable)
    {
        if (!ModConfig.EnableFollowCommand.Value)
        {
            return;
        }

        Character character = __instance.GetComponent<Character>();
        if (character == null || !SpeciesHelper.IsEligibleJuvenile(character) || !character.IsTamed())
        {
            return;
        }

        ___m_commandable = true;
    }
}

[HarmonyPatch(typeof(Character), "GetHoverText")]
public static class CharacterJuvenileHoverPatch
{
    private static void Postfix(Character __instance, ref string __result)
    {
        JuvenileFollowDisplay.ApplyFollowHover(__instance, ref __result);
    }
}

[HarmonyPatch(typeof(Character), "Awake")]
public static class CharacterAwakePatch
{
    private static void Postfix(Character __instance)
    {
        JuvenileGroundSnapper.EnsureAttached(__instance);
        JuvenileFollowController.EnsureAttached(__instance);
    }
}

[HarmonyPatch(typeof(AnimalAI), nameof(AnimalAI.UpdateAI))]
public static class AnimalAIFollowPatch
{
    private static bool Prefix(AnimalAI __instance, float dt, ref bool __result)
    {
        if (!JuvenileFollowController.TryApplyAnimalFollow(__instance, dt))
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(AnimalAI), nameof(AnimalAI.UpdateAI))]
[HarmonyPriority(1)]
public static class AnimalAIMateDrawPatch
{
    private static bool Prefix(AnimalAI __instance, float dt, ref bool __result)
    {
        if (MateDrawController.TryApplyAnimalMateDraw(__instance, dt))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
[HarmonyPriority(1)]
public static class MonsterAIMateDrawPatch
{
    private static bool Prefix(MonsterAI __instance, float dt, ref bool __result)
    {
        if (MateDrawController.TryApplyMonsterMateDraw(__instance, dt))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Growup), "GrowUpdate")]
public static class GrowupPatch
{
    private static void Prefix(Growup __instance)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        BaseAI baseAi = __instance.GetComponent<BaseAI>();
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || baseAi == null)
        {
            return;
        }

        if (baseAi.GetTimeSinceSpawned().TotalSeconds <= __instance.m_growTime)
        {
            return;
        }

        MonsterAI monsterAi = __instance.GetComponent<MonsterAI>();
        if (monsterAi != null)
        {
            monsterAi.SetFollowTarget(null);
        }

        ZDO zdo = nview.GetZDO();
        if (zdo != null)
        {
            zdo.Set(ZdoFields.FollowTarget, ZDOID.None);
            zdo.Set(ZDOVars.s_follow, string.Empty);
        }
    }
}
