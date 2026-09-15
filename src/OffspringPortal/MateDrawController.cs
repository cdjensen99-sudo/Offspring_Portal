using HarmonyLib;
using UnityEngine;

namespace OffspringPortal;

public static class MateDrawController
{
    public static bool TryApplyMonsterMateDraw(MonsterAI monsterAi, float dt)
    {
        if (!ModConfig.EnableMateDraw.Value || monsterAi == null)
        {
            return false;
        }

        Character character = monsterAi.GetComponent<Character>();
        if (!CanApplyMateDraw(character, out Character partner, out float stopDistance))
        {
            return false;
        }

        Vector3 offset = partner.transform.position - character.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        Procreation procreation = character.GetComponent<Procreation>();
        if (ShouldYieldBreedingToVanilla(character, procreation, distance, stopDistance))
        {
            return false;
        }

        bool run = distance > 10f;
        Traverse.Create(monsterAi)
            .Method("MoveTo", dt, partner.transform.position, 0f, run)
            .GetValue();
        return true;
    }

    public static bool TryApplyAnimalMateDraw(AnimalAI animalAi, float dt)
    {
        if (!ModConfig.EnableMateDraw.Value || animalAi == null)
        {
            return false;
        }

        Character character = animalAi.GetComponent<Character>();
        if (!CanApplyMateDraw(character, out Character partner, out float stopDistance))
        {
            return false;
        }

        Vector3 offset = partner.transform.position - character.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        Procreation procreation = character.GetComponent<Procreation>();
        if (ShouldYieldBreedingToVanilla(character, procreation, distance, stopDistance))
        {
            return false;
        }

        animalAi.MoveTowards(offset / distance, distance > 10f);
        return true;
    }

    public static void SetMateDrawTarget(Character character, Character partner)
    {
        if (character == null || partner == null)
        {
            return;
        }

        ZNetView nview = character.GetNview();
        ZDO zdo = character.GetZdo();
        if (nview == null || zdo == null)
        {
            return;
        }

        if (!nview.IsOwner())
        {
            nview.ClaimOwnership();
        }

        zdo.Set(ZdoFields.MateDrawTarget, partner.GetNview()?.GetZDO()?.m_uid ?? ZDOID.None);
    }

    public static void ClearMateDraw(Character character)
    {
        ZDO zdo = character?.GetZdo();
        if (zdo == null || zdo.GetZDOID(ZdoFields.MateDrawTarget) == ZDOID.None)
        {
            return;
        }

        zdo.Set(ZdoFields.MateDrawTarget, ZDOID.None);
    }

    private static bool CanApplyMateDraw(
        Character character,
        out Character partner,
        out float stopDistance)
    {
        partner = null;
        stopDistance = ModConfig.MateDrawStopDistance.Value;

        if (character == null)
        {
            return false;
        }

        ZNetView nview = character.GetNview();
        if (nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return false;
        }

        if (!ProcreationHelper.IsMateDrawEligible(character))
        {
            ClearMateDraw(character);
            return false;
        }

        Procreation procreation = character.GetComponent<Procreation>();
        if (procreation == null)
        {
            ClearMateDraw(character);
            return false;
        }

        if (ProcreationHelper.HasMateInBreedingRange(character, procreation))
        {
            ClearMateDraw(character);
            return false;
        }

        ZDOID partnerId = character.GetZdo()?.GetZDOID(ZdoFields.MateDrawTarget) ?? ZDOID.None;
        if (partnerId == ZDOID.None)
        {
            return false;
        }

        partner = ResolveCharacter(partnerId);
        if (partner == null || !ProcreationHelper.IsMateDrawEligible(partner))
        {
            ClearMateDraw(character);
            return false;
        }

        if (!SpeciesHelper.SpeciesKeyMatches(
                SpeciesHelper.GetAdultSpeciesKey(character),
                SpeciesHelper.GetAdultSpeciesKey(partner)))
        {
            ClearMateDraw(character);
            return false;
        }

        stopDistance = ProcreationHelper.GetStopDistance(procreation);
        return true;
    }

    private static bool ShouldYieldBreedingToVanilla(
        Character character,
        Procreation procreation,
        float distance,
        float stopDistance)
    {
        if (procreation != null && distance <= procreation.m_partnerCheckRange)
        {
            ClearMateDraw(character);
            return true;
        }

        if (distance <= stopDistance)
        {
            ClearMateDraw(character);
            return true;
        }

        return false;
    }

    private static Character ResolveCharacter(ZDOID characterId)
    {
        GameObject instance = ZNetScene.instance?.FindInstance(characterId);
        if (instance != null)
        {
            Character character = instance.GetComponent<Character>();
            if (character != null)
            {
                return character;
            }
        }

        foreach (Character candidate in Character.GetAllCharacters())
        {
            ZNetView nview = candidate?.GetNview();
            if (nview != null && nview.GetZDO()?.m_uid == characterId)
            {
                return candidate;
            }
        }

        return null;
    }
}
