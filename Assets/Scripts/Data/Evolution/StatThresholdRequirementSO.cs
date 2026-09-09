using UnityEngine;

public enum EvolutionStatType
{
    StrengthExt,
    StrengthInt,
    EnduranceExt,
    EnduranceInt
}

[CreateAssetMenu(
    menuName = "Evolution/Requirements/Stat Threshold",
    fileName = "Requirement_Stat")]
public class StatThresholdRequirementSO : EvolutionRequirementSO
{
    public EvolutionStatType stat;
    public float minimumValue;

    public override bool IsMet(
        OrganismProgression progression,
        PlayerBody body)
    {
        if (progression == null || progression.Combatant == null)
            return false;

        float value = stat switch
        {
            EvolutionStatType.StrengthExt => progression.StrengthExt,
            EvolutionStatType.StrengthInt => progression.StrengthInt,
            EvolutionStatType.EnduranceExt => progression.EnduranceExt,
            EvolutionStatType.EnduranceInt => progression.EnduranceInt,
            _ => 0f
        };

        return value >= minimumValue;
    }

    public override string GetDescription(
        OrganismProgression progression,
        PlayerBody body)
    {
        return $"{stat} >= {minimumValue:0.##}";
    }
}