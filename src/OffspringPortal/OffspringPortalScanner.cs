using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public class OffspringPortalScanner : MonoBehaviour
{
    private TeleportWorld portal;
    private readonly Dictionary<int, float> juvenileCooldowns = new Dictionary<int, float>();
    private readonly Dictionary<int, float> adultCooldowns = new Dictionary<int, float>();
    private float lastNoJuvenileDestinationMessageTime;
    private float lastNoAdultDestinationMessageTime;

    private void Start()
    {
        portal = GetComponent<TeleportWorld>();
        float juvenileInterval = ModConfig.BreederScanIntervalSec.Value;
        InvokeRepeating(nameof(ScanForJuveniles), juvenileInterval, juvenileInterval);

        float adultInterval = ModConfig.MaturingAdultScanIntervalSec.Value;
        InvokeRepeating(nameof(ScanForAdults), adultInterval, adultInterval);
    }

    private void ScanForJuveniles()
    {
        if (!ZNet.instance.IsServer() || portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        if (!JuvenilePortalRouter.IsBreederPortal(portal))
        {
            return;
        }

        ScanNearbyCharacters((character, cooldowns) =>
            JuvenilePortalRouter.TryRoute(portal, character, cooldowns, ref lastNoJuvenileDestinationMessageTime),
            juvenileCooldowns);
    }

    private void ScanForAdults()
    {
        if (!ZNet.instance.IsServer() || portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        if (!AdultPortalRouter.IsAdultRoutingMaturingPortal(portal))
        {
            return;
        }

        ScanNearbyCharacters((character, cooldowns) =>
            AdultPortalRouter.TryRoute(portal, character, cooldowns, ref lastNoAdultDestinationMessageTime),
            adultCooldowns);
    }

    private void ScanNearbyCharacters(
        System.Func<Character, Dictionary<int, float>, bool> tryRoute,
        Dictionary<int, float> cooldowns)
    {
        Vector3 center = portal.m_proximityRoot != null
            ? portal.m_proximityRoot.position
            : portal.transform.position;
        float range = ModConfig.BreederScanRange.Value;
        float rangeSquared = range * range;

        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsPlayer())
            {
                continue;
            }

            if ((character.transform.position - center).sqrMagnitude > rangeSquared)
            {
                continue;
            }

            tryRoute(character, cooldowns);
        }
    }
}
