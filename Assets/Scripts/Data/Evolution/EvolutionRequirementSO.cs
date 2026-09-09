using UnityEngine;

public abstract class EvolutionRequirementSO : ScriptableObject
{
    public abstract bool IsMet(
        OrganismProgression progression,
        PlayerBody body);

    public abstract string GetDescription(
        OrganismProgression progression,
        PlayerBody body);
}