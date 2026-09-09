using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Evolution", fileName = "Evolution")]
public class EvolutionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("0 = стартовая форма, 1 = первая эволюция, 2 = вторая и т.д.")]
    public int evolutionStage;

    public string evolutionId;
    public string displayName;

    [TextArea(minLines: 3, maxLines: 20)]
    public string description;

    [Header("Stat bonuses")]
    public int strengthExtBonus;
    public int strengthIntBonus;
    public int enduranceExtBonus;
    public int enduranceIntBonus;

    [Header("New Mutations")]
    public List<BodyPartVariantSO> mutations = new();

    [Header("New organs")]
    public List<BodyPartDefinitionSO> organs = new();
    
    [Header("Available organs for OE purchase")]
    public List<BodyPartDefinitionSO> purchasableOrgans = new();

    [Header("Next evolutions")]
    public List<EvolutionSO> nextEvolutions = new();

    [Header("Requirements")]
    public List<EvolutionRequirementSO> requirements = new();

    [Header("Evolution")]
    [Min(1f)]
    public float evolutionDuration = 10f;

    public bool CanBeSelected(OrganismProgression progression, PlayerBody body)
    {
        if (progression == null || body == null)
            return false;

        if (evolutionStage != progression.EvolutionStage + 1)
            return false;

        foreach (var requirement in requirements)
        {
            if (requirement == null)
                continue;

            if (!requirement.IsMet(progression, body))
                return false;
        }

        return true;
    }
}