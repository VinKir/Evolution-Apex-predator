using System.Collections.Generic;
using UnityEngine;

// Result of EnemyGenerator.Generate, applied by OrganismCombatant.ConfigureEnemy
public class EnemyBuild
{
    public EvolutionSO evolution;

    // From the picked evolution down to the stage 0 form
    public readonly List<EvolutionSO> path = new();

    public int strengthExt;
    public int strengthInt;
    public int enduranceExt;
    public int enduranceInt;

    public readonly List<OrganismBody.BodyPartRuntimeState> parts = new();
}

// Builds a spawned enemy from a random path through the evolution tree:
// stats and organs of every stage on the path, extra purchasable organs, rolled part levels and free mutations.
public static class EnemyGenerator
{
    private const int MilestoneStep = 5;

    public static EnemyBuild Generate(EvolutionSO evolution, int dangerModificator)
    {
        var build = new EnemyBuild { evolution = evolution };

        BuildPath(evolution, build.path);
        SumStats(build);
        AddOrgans(build, dangerModificator);

        int maxLevel = evolution.LevelCap;
        foreach (var part in build.parts)
            part.level = RollPartLevel(maxLevel);

        AddGrantedMutations(build);
        AddMilestoneMutations(build);

        return build;
    }

    private static void BuildPath(EvolutionSO evolution, List<EvolutionSO> path)
    {
        var visited = new HashSet<EvolutionSO>();
        var current = evolution;

        while (current != null && visited.Add(current))
        {
            path.Add(current);

            if (current.evolutionStage <= 0)
                return;

            var candidates = new List<EvolutionSO>();
            foreach (var previous in current.previousEvolutions)
            {
                if (previous != null && previous.evolutionStage < current.evolutionStage && !visited.Contains(previous))
                    candidates.Add(previous);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"Evolution '{current.name}' (stage {current.evolutionStage}) has no previous evolution, enemy path stops there", current);
                return;
            }

            current = candidates[Random.Range(0, candidates.Count)];
        }
    }

    private static void SumStats(EnemyBuild build)
    {
        foreach (var stage in build.path)
        {
            build.strengthExt += stage.strengthExtBonus;
            build.strengthInt += stage.strengthIntBonus;
            build.enduranceExt += stage.enduranceExtBonus;
            build.enduranceInt += stage.enduranceIntBonus;
        }
    }

    private static void AddOrgans(EnemyBuild build, int dangerModificator)
    {
        foreach (var stage in build.path)
        {
            foreach (var organ in stage.organs)
                TryAddOrgan(build, organ);
        }

        if (dangerModificator <= 0)
            return;

        foreach (var stage in build.path)
        {
            int extraCount = Random.Range(0, dangerModificator + 1);
            if (extraCount == 0)
                continue;

            var options = new List<BodyPartDefinitionSO>(stage.purchasableOrgans);
            Shuffle(options);

            foreach (var organ in options)
            {
                if (extraCount == 0)
                    break;

                if (TryAddOrgan(build, organ))
                    extraCount--;
            }
        }
    }

    private static bool TryAddOrgan(EnemyBuild build, BodyPartDefinitionSO organ)
    {
        if (organ == null || FindPart(build, organ.partId) != null)
            return false;

        build.parts.Add(new OrganismBody.BodyPartRuntimeState { organ = organ, level = 0 });
        return true;
    }

    // 50%: in the top band [max - 5, max], otherwise below it
    private static int RollPartLevel(int maxLevel)
    {
        int bandStart = maxLevel - MilestoneStep;

        if (bandStart <= 0 || Random.value < 0.5f)
            return Random.Range(Mathf.Max(0, bandStart), maxLevel + 1);

        return Random.Range(0, bandStart);
    }

    // Mirrors EvolutionUIController.ApplyEvolutionMutations
    private static void AddGrantedMutations(EnemyBuild build)
    {
        foreach (var stage in build.path)
        {
            foreach (var mutation in stage.mutations)
            {
                if (mutation == null || mutation.bodyPart == null)
                    continue;

                var part = FindPart(build, mutation.bodyPart.partId);
                if (part == null || HasMilestone(part, mutation.unlockLevel) || HasVariant(part, mutation))
                    continue;

                part.appliedVariants.Add(new OrganismBody.AppliedVariant
                {
                    milestoneLevel = mutation.unlockLevel,
                    variant = mutation
                });
            }
        }
    }

    // Mirrors MutationUIController.BuildMilestoneRequests, with a random choice instead of the player's
    private static void AddMilestoneMutations(EnemyBuild build)
    {
        foreach (var part in build.parts)
        {
            for (int milestone = MilestoneStep; milestone <= part.level; milestone += MilestoneStep)
            {
                if (HasMilestone(part, milestone))
                    continue;

                var options = part.organ.GetVariantsForLevel(milestone);
                options.RemoveAll(variant => HasVariant(part, variant));

                if (options.Count == 0)
                    continue;

                part.appliedVariants.Add(new OrganismBody.AppliedVariant
                {
                    milestoneLevel = milestone,
                    variant = options[Random.Range(0, options.Count)]
                });
            }
        }
    }

    private static OrganismBody.BodyPartRuntimeState FindPart(EnemyBuild build, string partId)
    {
        return build.parts.Find(part => part.organ.partId == partId);
    }

    private static bool HasMilestone(OrganismBody.BodyPartRuntimeState part, int milestone)
    {
        return part.appliedVariants.Exists(applied => applied.milestoneLevel == milestone);
    }

    private static bool HasVariant(OrganismBody.BodyPartRuntimeState part, BodyPartVariantSO variant)
    {
        return part.appliedVariants.Exists(applied => applied.variant == variant);
    }

    private static void Shuffle<T>(List<T> items)
    {
        for (int index = items.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }
}
