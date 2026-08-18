using System;
using System.Collections.Generic;
using UnityEngine;

public class OrganismCombatantRegistry : MonoBehaviour
{
    public static OrganismCombatantRegistry Instance { get; private set; }

    private readonly HashSet<OrganismCombatant> combatants = new();

    public IReadOnlyCollection<OrganismCombatant> Combatants => combatants;

    public event Action<OrganismCombatant> CombatantRegistered;
    public event Action<OrganismCombatant> CombatantUnregistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Register(OrganismCombatant combatant)
    {
        if (combatant == null)
            return;

        if (!combatants.Add(combatant))
            return;

        CombatantRegistered?.Invoke(combatant);
    }

    public void Unregister(OrganismCombatant combatant)
    {
        if (combatant == null)
            return;

        if (!combatants.Remove(combatant))
            return;

        CombatantUnregistered?.Invoke(combatant);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}