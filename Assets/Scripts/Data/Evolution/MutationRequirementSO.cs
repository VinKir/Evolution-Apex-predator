using UnityEngine;

[CreateAssetMenu(
    menuName = "Evolution/Requirements/Mutation",
    fileName = "Requirement_Mutation")]
public class MutationRequirementSO : EvolutionRequirementSO
{
    public string partId;
    public BodyPartVariantSO requiredMutation;

    public override bool IsMet(
        OrganismProgression progression,
        PlayerBody body)
    {
        if (body == null || requiredMutation == null)
            return false;

        return body.HasMutation(
            partId,
            requiredMutation);
    }

    public override string GetDescription(
        OrganismProgression progression,
        PlayerBody body)
    {
        return $"Мутация: {requiredMutation.displayName}";
    }
}