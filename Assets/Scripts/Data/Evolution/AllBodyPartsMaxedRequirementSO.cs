using UnityEngine;

[CreateAssetMenu(
    menuName = "Evolution/Requirements/All Parts Maxed",
    fileName = "Requirement_AllPartsMaxed")]
public class AllBodyPartsMaxedRequirementSO : EvolutionRequirementSO
{
    public override bool IsMet(
        OrganismProgression progression,
        PlayerBody body)
    {
        if (body == null || progression == null)
            return false;

        body.EnsureStates();

        foreach (var state in body.States)
        {
            if (state == null || state.organ == null)
                continue;

            if (state.level < progression.MutationCap)
                return false;
        }

        return true;
    }

    public override string GetDescription(
        OrganismProgression progression,
        PlayerBody body)
    {
        return "Все части тела прокачаны до максимального уровня";
    }
}