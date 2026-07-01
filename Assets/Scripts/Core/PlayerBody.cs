using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerBody : MonoBehaviour
{
    [Serializable]
    public class AppliedVariant
    {
        public int milestoneLevel;
        public BodyPartVariantSO variant;
    }

    [Serializable]
    public class BodyPartRuntimeState
    {
        public BodyPartDefinitionSO definition;
        public int level;
        public List<AppliedVariant> appliedVariants = new();
    }

    [Header("Definitions")]
    [SerializeField] private List<BodyPartDefinitionSO> partDefinitions = new();

    [Header("Runtime")]
    [SerializeField] private List<BodyPartRuntimeState> states = new();

    public IReadOnlyList<BodyPartRuntimeState> States => states;

    public event Action OnBodyChanged;

    private void Awake()
    {
        EnsureStates();
    }

    private void Reset()
    {
        EnsureStates();
    }

    public void EnsureStates()
    {
        if (partDefinitions == null)
            return;

        if (states.Count == 0)
        {
            foreach (var def in partDefinitions)
            {
                states.Add(new BodyPartRuntimeState
                {
                    definition = def,
                    level = 0
                });
            }
            return;
        }

        while (states.Count < partDefinitions.Count)
            states.Add(new BodyPartRuntimeState());

        for (int i = 0; i < partDefinitions.Count; i++)
            states[i].definition = partDefinitions[i];
    }

    public BodyPartRuntimeState GetState(string partId)
    {
        return states.FirstOrDefault(s => s.definition != null && s.definition.partId == partId);
    }

    public int GetLevel(string partId)
    {
        var state = GetState(partId);
        return state != null ? state.level : 0;
    }

    public void ApplyMutation(Dictionary<string, int> queuedLevelAdds, List<BodyVariantSelection> selections)
    {
        if (queuedLevelAdds != null)
        {
            foreach (var kv in queuedLevelAdds)
            {
                var state = GetState(kv.Key);
                if (state != null)
                    state.level += kv.Value;
            }
        }

        if (selections != null)
        {
            foreach (var selection in selections)
            {
                if (selection == null || selection.variant == null)
                    continue;

                var state = GetState(selection.partId);
                if (state == null)
                    continue;

                var existing = state.appliedVariants.Find(v => v.milestoneLevel == selection.milestoneLevel);
                if (existing == null)
                {
                    state.appliedVariants.Add(new AppliedVariant
                    {
                        milestoneLevel = selection.milestoneLevel,
                        variant = selection.variant
                    });
                }
                else
                {
                    existing.variant = selection.variant;
                }
            }
        }

        OnBodyChanged?.Invoke();
    }
}