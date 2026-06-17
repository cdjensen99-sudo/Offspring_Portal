using UnityEngine;

namespace OffspringPortal;

public sealed class JuvenileGroundSnapper : MonoBehaviour
{
    private Character character;
    private ZNetView nview;
    private float nextCheckTime;

    private void Awake()
    {
        character = GetComponent<Character>();
        nview = GetComponent<ZNetView>();
    }

    private void Start()
    {
        TrySnapToGround(force: true);
    }

    private void LateUpdate()
    {
        if (Time.time < nextCheckTime || character == null || nview == null || !nview.IsValid())
        {
            return;
        }

        if (!ZNet.instance.IsServer() || !nview.IsOwner())
        {
            return;
        }

        if (!SpeciesHelper.IsEligibleJuvenile(character))
        {
            return;
        }

        nextCheckTime = Time.time + 1f;
        TrySnapToGround(force: false);
    }

    public static void EnsureAttached(Character character)
    {
        if (character == null || !SpeciesHelper.IsEligibleJuvenile(character))
        {
            return;
        }

        JuvenileGroundSnapper snapper = character.GetComponent<JuvenileGroundSnapper>();
        if (snapper == null)
        {
            snapper = character.gameObject.AddComponent<JuvenileGroundSnapper>();
        }

        snapper.TrySnapToGround(force: true);
    }

    private void TrySnapToGround(bool force)
    {
        if (!JuvenilePlacement.TryGetFloorPosition(transform.position, out Vector3 grounded))
        {
            return;
        }

        float delta = transform.position.y - grounded.y;
        if (!force && delta < 0.35f)
        {
            return;
        }

        JuvenilePlacement.ApplyPosition(character, grounded, transform.rotation);
    }
}
