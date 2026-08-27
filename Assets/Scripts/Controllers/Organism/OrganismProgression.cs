using System;
using UnityEngine;

public class OrganismProgression : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int experienceToNextLevel = 10;

    [Header("Mutation")]
    [SerializeField] private int evolutionStage = 1;
    [SerializeField] private int mutationCap = 5;

    [Header("Biomass")]
    [SerializeField] private float biomass = 0f;

    private int LevelCap => evolutionStage * 5;

    public event Action OnEvolve;

    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => experienceToNextLevel;
    public int EvolutionStage => evolutionStage;
    public int MutationCap => mutationCap;
    public float Biomass => biomass;

    public bool CanEvolve => level >= LevelCap;

    public void InitializeRuntime(int initialLevel, int initialEvolutionStage)
    {
        evolutionStage = Mathf.Max(1, initialEvolutionStage);
        level = Mathf.Clamp(initialLevel, 1, LevelCap);
        experience = 0;
        experienceToNextLevel = 10 * level;
        mutationCap = evolutionStage * 5;
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

            experienceToNextLevel = Mathf.CeilToInt(experienceToNextLevel + 10f);

            if (level >= LevelCap)
            {
                level = LevelCap;
                experience = 0;
                break;
            }
        }
    }

    public void Evolve()
    {
        if (!CanEvolve)
            return;

        evolutionStage++;

        level = 1;
        experience = 0;

        mutationCap = evolutionStage * 5;

        OnEvolve?.Invoke();
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
}