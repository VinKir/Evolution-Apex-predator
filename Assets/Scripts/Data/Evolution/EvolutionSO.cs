using System;
using System.Collections.Generic;
using UnityEditor;
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

    [Header("Previous evolutions")]
    [Tooltip("Синхронизируется с nextEvolutions других эволюций автоматически")]
    public List<EvolutionSO> previousEvolutions = new();

    [Header("Requirements")]
    public List<EvolutionRequirementSO> requirements = new();

    [Header("Evolution")]
    [Min(1f)]
    public float evolutionDuration = 10f;

    public int LevelCap => (evolutionStage + 1) * 5;

    public bool CanBeSelected(OrganismProgression progression, OrganismBody body)
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

    #if UNITY_EDITOR
    // Lists from the last validation, used to detect links added or removed in the inspector
    [NonSerialized] private List<EvolutionSO> nextSnapshot;
    [NonSerialized] private List<EvolutionSO> previousSnapshot;

    private void OnValidate()
    {
        // On load or first selection there is nothing to compare with, so only missing mirror links are added
        bool firstValidation = nextSnapshot == null || previousSnapshot == null;

        foreach (var next in nextEvolutions)
        {
            if (IsLinkable(next) && (firstValidation || !nextSnapshot.Contains(next)))
                AddLink(next, next.previousEvolutions);
        }

        foreach (var previous in previousEvolutions)
        {
            if (IsLinkable(previous) && (firstValidation || !previousSnapshot.Contains(previous)))
                AddLink(previous, previous.nextEvolutions);
        }

        if (!firstValidation)
        {
            foreach (var next in nextSnapshot)
            {
                if (IsLinkable(next) && !nextEvolutions.Contains(next))
                    RemoveLink(next, next.previousEvolutions);
            }

            foreach (var previous in previousSnapshot)
            {
                if (IsLinkable(previous) && !previousEvolutions.Contains(previous))
                    RemoveLink(previous, previous.nextEvolutions);
            }
        }

        nextSnapshot = new List<EvolutionSO>(nextEvolutions);
        previousSnapshot = new List<EvolutionSO>(previousEvolutions);
    }

    private bool IsLinkable(EvolutionSO other)
    {
        return other != null && other != this;
    }

    private void AddLink(EvolutionSO other, List<EvolutionSO> otherList)
    {
        if (otherList.Contains(this))
            return;

        otherList.Add(this);
        EditorUtility.SetDirty(other);
    }

    private void RemoveLink(EvolutionSO other, List<EvolutionSO> otherList)
    {
        if (otherList.RemoveAll(evolution => evolution == this) > 0)
            EditorUtility.SetDirty(other);
    }
    #endif
}