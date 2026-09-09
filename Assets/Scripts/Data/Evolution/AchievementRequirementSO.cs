using UnityEngine;

[CreateAssetMenu(
    menuName = "Evolution/Requirements/Achievement",
    fileName = "Requirement_Achievement")]
public class AchievementRequirementSO : EvolutionRequirementSO
{
    public string flagId;

    public override bool IsMet(
        OrganismProgression progression,
        PlayerBody body)
    {
        return progression != null &&
               progression.HasAchievementFlag(flagId);
    }

    public override string GetDescription(
        OrganismProgression progression,
        PlayerBody body)
    {
        return $"Достижение: {flagId}";
    }
}