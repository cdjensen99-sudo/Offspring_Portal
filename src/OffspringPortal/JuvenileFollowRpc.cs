using UnityEngine;

namespace OffspringPortal;

public sealed class JuvenileFollowRpcHandler : MonoBehaviour
{
    private const string ToggleFollowRpc = "op_toggle_follow";
    private ZNetView nview;
    private bool registered;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
    }

    private void Start()
    {
        TryRegister();
    }

    private void TryRegister()
    {
        if (registered || nview == null || !nview.IsValid())
        {
            return;
        }

        nview.Register<ZDOID, bool>(ToggleFollowRpc, RPC_ToggleFollow);
        registered = true;
    }

    public void RequestToggle(Player player, bool showMessage)
    {
        TryRegister();
        if (nview == null || !nview.IsValid() || player == null)
        {
            return;
        }

        nview.InvokeRPC(ToggleFollowRpc, player.GetZDOID(), showMessage);
    }

    private void RPC_ToggleFollow(long sender, ZDOID playerId, bool showMessage)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }

        Character character = GetComponent<Character>();
        Player player = Player.GetPlayer(sender)
            ?? ZNetScene.instance.FindInstance(playerId)?.GetComponent<Player>();
        if (character == null || player == null)
        {
            return;
        }

        JuvenileFollow.ApplyAnimalFollowToggle(character, player, showMessage);
    }

    public static void EnsureAttached(Character character)
    {
        if (character == null || character.GetComponent<AnimalAI>() == null)
        {
            return;
        }

        if (character.GetComponent<JuvenileFollowRpcHandler>() == null)
        {
            character.gameObject.AddComponent<JuvenileFollowRpcHandler>();
        }
    }
}
