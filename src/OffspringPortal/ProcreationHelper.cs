using UnityEngine;

namespace OffspringPortal;

public static class ProcreationHelper
{
    public static bool IsMateDrawEligible(Character character)
    {
        if (character == null || !character.IsTamed() || character.IsPlayer())
        {
            return false;
        }

        if (character.GetComponent<Growup>() != null)
        {
            return false;
        }

        Procreation procreation = character.GetComponent<Procreation>();
        if (procreation == null || !procreation.ReadyForProcreation())
        {
            return false;
        }

        Tameable tameable = character.GetComponent<Tameable>();
        if (tameable != null && tameable.IsHungry())
        {
            return false;
        }

        BaseAI baseAi = character.GetComponent<BaseAI>();
        if (baseAi != null && baseAi.IsAlerted())
        {
            return false;
        }

        if (JuvenileFollow.IsFollowing(character))
        {
            return false;
        }

        MonsterAI monsterAi = character.GetComponent<MonsterAI>();
        if (monsterAi != null && monsterAi.GetFollowTarget() != null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(SpeciesHelper.GetAdultSpeciesKey(character));
    }

    public static bool HasMateInBreedingRange(Character character, Procreation procreation)
    {
        if (character == null || procreation == null)
        {
            return false;
        }

        string speciesKey = SpeciesHelper.GetAdultSpeciesKey(character);
        float partnerRange = procreation.m_partnerCheckRange;
        float partnerRangeSquared = partnerRange * partnerRange;
        Vector3 position = character.transform.position;

        foreach (Character other in Character.GetAllCharacters())
        {
            if (other == null || other == character || other.IsPlayer())
            {
                continue;
            }

            if (!SpeciesHelper.SpeciesKeyMatches(
                    SpeciesHelper.GetAdultSpeciesKey(other),
                    speciesKey))
            {
                continue;
            }

            Procreation otherProcreation = other.GetComponent<Procreation>();
            if (otherProcreation == null || !otherProcreation.ReadyForProcreation())
            {
                continue;
            }

            if ((other.transform.position - position).sqrMagnitude <= partnerRangeSquared)
            {
                return true;
            }
        }

        return false;
    }

    public static float GetStopDistance(Procreation procreation)
    {
        if (procreation == null)
        {
            return ModConfig.MateDrawStopDistance.Value;
        }

        float partnerRange = procreation.m_partnerCheckRange;
        float configured = ModConfig.MateDrawStopDistance.Value;
        return Mathf.Min(configured, partnerRange * 0.85f);
    }
}
