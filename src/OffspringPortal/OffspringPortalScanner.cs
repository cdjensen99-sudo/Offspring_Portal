using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public class OffspringPortalScanner : MonoBehaviour
{
    private TeleportWorld portal;
    private readonly Dictionary<int, float> juvenileCooldowns = new Dictionary<int, float>();
    private readonly Dictionary<int, float> adultCooldowns = new Dictionary<int, float>();
    private readonly Dictionary<int, float> eggCooldowns = new Dictionary<int, float>();
    private float lastNoJuvenileDestinationMessageTime;
    private float lastNoAdultDestinationMessageTime;
    private float lastNoEggDestinationMessageTime;

    private void Start()
    {
        portal = GetComponent<TeleportWorld>();
        float juvenileInterval = ModConfig.BreederScanIntervalSec.Value;
        InvokeRepeating(nameof(ScanForJuveniles), juvenileInterval, juvenileInterval);

        float adultInterval = ModConfig.MaturingAdultScanIntervalSec.Value;
        InvokeRepeating(nameof(ScanForAdults), adultInterval, adultInterval);

        if (ModConfig.EnableMateDraw.Value)
        {
            float mateInterval = ModConfig.MateDrawIntervalSec.Value;
            InvokeRepeating(nameof(ScanForMateDraw), mateInterval, mateInterval);
        }
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

        RefreshDiscovery();
        ScanNearbyCharacters((character, cooldowns) =>
            JuvenilePortalRouter.TryRoute(portal, character, cooldowns, ref lastNoJuvenileDestinationMessageTime),
            juvenileCooldowns);
        ScanNearbyEggs();
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

    private void RefreshDiscovery()
    {
        BreedableSpeciesRegistry.RefreshFromAllBreeders();
    }

    private void ScanNearbyEggs()
    {
        Vector3 center = GetScanCenter();
        float range = ModConfig.BreederScanRange.Value;
        float rangeSquared = range * range;

        ItemDrop[] eggs = Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
        foreach (ItemDrop egg in eggs)
        {
            if (egg == null || egg.GetComponent<EggGrow>() == null)
            {
                continue;
            }

            if ((egg.transform.position - center).sqrMagnitude > rangeSquared)
            {
                continue;
            }

            EggPortalRouter.TryRoute(portal, egg, eggCooldowns, ref lastNoEggDestinationMessageTime);
        }
    }

    private void ScanNearbyCharacters(
        System.Func<Character, Dictionary<int, float>, bool> tryRoute,
        Dictionary<int, float> cooldowns)
    {
        Vector3 center = GetScanCenter();
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

    private void ScanForMateDraw()
    {
        if (!ZNet.instance.IsServer() || portal == null || !OffspringPortalPrefabs.IsOffspringPortal(portal))
        {
            return;
        }

        MateDrawScanner.ScanPortal(portal, GetScanCenter());
    }

    private Vector3 GetScanCenter()
    {
        return portal.m_proximityRoot != null
            ? portal.m_proximityRoot.position
            : portal.transform.position;
    }
}
