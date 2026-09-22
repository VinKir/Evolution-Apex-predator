using System;
using System.Collections.Generic;
using UnityEngine;

public class OrganismProgression : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OrganismCombatant combatant;

    [Header("Starting Evolution")]
    [SerializeField] private EvolutionSO startingEvolution;

    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int experienceToNextLevel = 10;

    [Header("Evolution")]
    [SerializeField] private EvolutionSO currentEvolution;

    [Header("Evolution Points")]
    [SerializeField] private int evolutionPoints;

    [Header("Characteristics")]
    [SerializeField] private int strengthExt;
    [SerializeField] private int strengthInt;
    [SerializeField] private int enduranceExt;
    [SerializeField] private int enduranceInt;

    [Header("Achievements")]
    [SerializeField] private List<string> achievementFlags = new();

    [Header("Biomass")]
    [SerializeField] private float biomass = 0f;

    public event Action OnEvolve;
    public event Action OnEvolutionPointsChanged;

    public int StrengthExt => strengthExt;
    public int StrengthInt => strengthInt;
    public int EnduranceExt => enduranceExt;
    public int EnduranceInt => enduranceInt;

    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    public EvolutionSO CurrentEvolution => currentEvolution;

    public int EvolutionStage => currentEvolution != null ? currentEvolution.evolutionStage : 0;

    public int MutationCap => LevelCap;

    public int LevelCap => (EvolutionStage + 1) * 5;

    public bool CanEvolve => level >= LevelCap;

    public int EvolutionPoints => evolutionPoints;

    public float Biomass => biomass;

    public OrganismCombatant Combatant => combatant;

    private void Awake()
    {
        strengthExt = startingEvolution.strengthExtBonus;
        strengthInt = startingEvolution.strengthIntBonus;
        enduranceExt = startingEvolution.enduranceExtBonus;
        enduranceInt = startingEvolution.enduranceIntBonus;
    }

    //TODO: доделать генерацию врага. Характеристики сейчас берутся только у базовой эволюции, должны браться сумма за все эволюции
    public void InitializeRuntime(int initialLevel, EvolutionSO initialEvolution)
    {
        currentEvolution = initialEvolution;
        level = Mathf.Clamp(initialLevel, 1, (EvolutionStage + 1) * 5);

        experience = 0;
        experienceToNextLevel = 10 * level;

        strengthExt = initialEvolution.strengthExtBonus;
        strengthInt = initialEvolution.strengthIntBonus;
        enduranceExt = initialEvolution.enduranceExtBonus;
        enduranceInt = initialEvolution.enduranceIntBonus;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        if (level >= LevelCap)
            return;

        experience += amount;

        while (experience >= experienceToNextLevel && level < LevelCap)
        {
            experience -= experienceToNextLevel;
            level++;

            experienceToNextLevel += 10;

            if (level >= LevelCap)
            {
                level = LevelCap;
                experience = 0;
                break;
            }
        }
    }

    public void AddBiomass(float amount)
    {
        if (amount <= 0f) return;
        biomass += amount;
    }

    public bool CanSpendBiomass(float amount)
    {
        return biomass + 0.0001f >= amount;
    }

    public bool SpendBiomass(float amount)
    {
        if (!CanSpendBiomass(amount))
            return false;

        biomass -= amount;
        if (biomass < 0f) biomass = 0f;
        return true;
    }
    
    public void ApplyEvolution(
        EvolutionSO evolution,
        int purchasedStrengthExt,
        int purchasedStrengthInt,
        int purchasedEnduranceExt,
        int purchasedEnduranceInt,
        int evolutionPointsSpent)
    {
        if (!ModifyEvolutionPoints(-evolutionPointsSpent))
            throw new InvalidOperationException(
                "Недостаточно Очков Эволюции.");

        strengthExt += evolution.strengthExtBonus + purchasedStrengthExt;
        strengthInt += evolution.strengthIntBonus + purchasedStrengthInt;
        enduranceExt += evolution.enduranceExtBonus + purchasedEnduranceExt;
        enduranceInt += evolution.enduranceIntBonus + purchasedEnduranceInt;

        currentEvolution = evolution;

        level = 1;
        experience = 0;
        experienceToNextLevel = 10;

        OnEvolve?.Invoke();
    }

    public bool CanSpendEvolutionPoints(int amount)
    {
        return evolutionPoints >= amount;
    }

    public bool ModifyEvolutionPoints(int amount)
    {
        if (!CanSpendEvolutionPoints(amount))
            return false;

        evolutionPoints += amount;
        OnEvolutionPointsChanged?.Invoke();

        return true;
    }

    public bool HasAchievementFlag(string flagId)
    {
        return !string.IsNullOrEmpty(flagId) && achievementFlags.Contains(flagId);
    }

    public void SetAchievementFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
            return;

        if (achievementFlags.Contains(flagId))
            return;

        achievementFlags.Add(flagId);
    }
}