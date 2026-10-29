using UnityEngine;

public abstract class EvolutionRequirementSO : ScriptableObject
{
    public abstract bool IsMet(
        OrganismProgression progression,
        OrganismBody body);

    public abstract string GetDescription(
        OrganismProgression progression,
        OrganismBody body);
}