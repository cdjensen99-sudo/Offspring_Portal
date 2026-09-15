using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public sealed class OffspringPortalRuntime : MonoBehaviour
{
    private static OffspringPortalRuntime instance;
    private readonly HashSet<ZDOID> pendingTeleports = new HashSet<ZDOID>();
    private float configRetryTimer;

    public static OffspringPortalRuntime Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("OffspringPortal_Runtime");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<OffspringPortalRuntime>();
            }

            return instance;
        }
    }

    private void Update()
    {
        if (ZNet.instance == null)
        {
            return;
        }

        configRetryTimer += Time.deltaTime;
        if (configRetryTimer >= 0.5f)
        {
            configRetryTimer = 0f;
            PortalRpc.ProcessPendingConfigs();
        }
    }

    public bool TryQueueDistantTeleport(Character juvenile, PortalRecord destination, OPTeleportWorld sourcePortal)
    {
        ZNetView nview = juvenile?.GetNview();
        if (nview == null || !nview.IsValid())
        {
            return false;
        }

        ZDOID id = nview.GetZDO().m_uid;
        if (pendingTeleports.Contains(id))
        {
            return false;
        }

        pendingTeleports.Add(id);
        StartCoroutine(DeferredTeleport(juvenile, destination, sourcePortal, id));
        return true;
    }

    private IEnumerator DeferredTeleport(Character juvenile, PortalRecord destination, OPTeleportWorld sourcePortal, ZDOID juvenileId)
    {
        float timeout = ModConfig.DistantTeleportTimeoutSec.Value;
        float elapsed = 0f;
        Vector3 targetPos = JuvenileTeleporter.ResolveDestinationPosition(destination, sourcePortal);

        while (elapsed < timeout)
        {
            if (juvenile == null)
            {
                pendingTeleports.Remove(juvenileId);
                yield break;
            }

            if (ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(targetPos))
            {
                if (JuvenileTeleporter.TryTeleportNow(juvenile, destination, sourcePortal))
                {
                    pendingTeleports.Remove(juvenileId);
                    yield break;
                }
            }

            elapsed += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        if (juvenile != null && JuvenileTeleporter.TryTeleportNow(juvenile, destination, sourcePortal, allowStoredHeight: true))
        {
            pendingTeleports.Remove(juvenileId);
            yield break;
        }

        pendingTeleports.Remove(juvenileId);
        Player local = Player.m_localPlayer;
        if (local != null)
        {
            local.Message(MessageHud.MessageType.Center,
                $"Could not reach distant pen for {SpeciesCatalog.GetDisplayName(SpeciesHelper.GetJuvenileSpecies(juvenile))}.");
        }
    }
}
