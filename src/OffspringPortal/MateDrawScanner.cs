using System.Collections.Generic;
using UnityEngine;

namespace OffspringPortal;

public static class MateDrawScanner
{
    public static void ScanPortal(TeleportWorld portal, Vector3 center)
    {
        if (!ModConfig.EnableMateDraw.Value
            || portal == null
            || !ZNet.instance.IsServer()
            || !JuvenilePortalRouter.IsBreederPortal(portal))
        {
            return;
        }

        float drawRange = ModConfig.MateDrawRange.Value;
        float drawRangeSquared = drawRange * drawRange;
        List<Character> eligible = new List<Character>();

        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsPlayer())
            {
                continue;
            }

            if ((character.transform.position - center).sqrMagnitude > drawRangeSquared)
            {
                MateDrawController.ClearMateDraw(character);
                continue;
            }

            if (!ProcreationHelper.IsMateDrawEligible(character))
            {
                MateDrawController.ClearMateDraw(character);
                continue;
            }

            eligible.Add(character);
        }

        AssignMateTargets(eligible, drawRange);
    }

    private static void AssignMateTargets(List<Character> eligible, float drawRange)
    {
        float drawRangeSquared = drawRange * drawRange;

        foreach (Character character in eligible)
        {
            Procreation procreation = character.GetComponent<Procreation>();
            if (procreation == null)
            {
                MateDrawController.ClearMateDraw(character);
                continue;
            }

            if (ProcreationHelper.HasMateInBreedingRange(character, procreation))
            {
                MateDrawController.ClearMateDraw(character);
                continue;
            }

            string speciesKey = SpeciesHelper.GetAdultSpeciesKey(character);
            float partnerRange = procreation.m_partnerCheckRange;
            float partnerRangeSquared = partnerRange * partnerRange;
            Character bestPartner = null;
            float bestDistanceSquared = float.MaxValue;
            Vector3 position = character.transform.position;

            foreach (Character candidate in eligible)
            {
                if (candidate == character)
                {
                    continue;
                }

                if (!SpeciesHelper.SpeciesKeyMatches(
                        SpeciesHelper.GetAdultSpeciesKey(candidate),
                        speciesKey))
                {
                    continue;
                }

                float distanceSquared = (candidate.transform.position - position).sqrMagnitude;
                if (distanceSquared <= partnerRangeSquared || distanceSquared > drawRangeSquared)
                {
                    continue;
                }

                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestPartner = candidate;
                }
            }

            if (bestPartner == null)
            {
                MateDrawController.ClearMateDraw(character);
                continue;
            }

            MateDrawController.SetMateDrawTarget(character, bestPartner);
        }
    }
}
