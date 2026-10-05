using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public static class OrganismStatsCalculator
{
    public static OrganismRuntimeStats Calculate(
        OrganismProgression progression,
        IReadOnlyList<PlayerBody.BodyPartRuntimeState> bodyParts,
        int evolutionStage)
    {
        CombatBonusAccumulator bonus = AggregateBonuses(bodyParts, evolutionStage);
        OrganismRuntimeStats stats = new OrganismRuntimeStats
        {
            maxChitinHp = 2.5f * progression.EnduranceExt * (1f + bonus.maxChitinHpMult),
            maxBodyHp = progression.EnduranceInt * (1f + bonus.maxBodyHpMult),
            maxJawHp = 0.8f * progression.EnduranceInt * (1f + bonus.maxJawHpMult),
            maxLegHp = 0.8f * progression.EnduranceInt * (1f + bonus.maxLegHpMult),

            maxStamina = (5f + progression.StrengthInt + progression.EnduranceInt) * (1 + bonus.maxStaminaMult),
            staminaRegen = (0.2f + progression.StrengthInt + progression.EnduranceInt) * (1 + bonus.staminaRecoveryMult),

            attackDamage = progression.StrengthExt * (1f + bonus.attackDamageMult),
            attackWindup = Mathf.Max(0f, CombatSettings.BaseAttackWindup - bonus.attackWindupReduction),
            attackActiveTime = Mathf.Max(0f, CombatSettings.BaseAttackActiveTime - bonus.attackActiveTimeReduction),
            attackCooldown = Mathf.Max(0f, CombatSettings.BaseAttackCooldown - bonus.attackCooldownReduction),
            experienceOnHitMult = bonus.experienceOnHitMult,
            experienceOnHitVsLowerEvolutionMult = bonus.experienceOnHitVsLowerEvolutionMult,
            experienceOnHitVsHigherEvolutionMult = bonus.experienceOnHitVsHigherEvolutionMult,
            experienceOnKillMult = bonus.experienceOnKillMult,
            experienceOnKillVsLowerEvolutionMult = bonus.experienceOnKillVsLowerEvolutionMult,
            experienceOnKillVsHigherEvolutionMult = bonus.experienceOnKillVsHigherEvolutionMult,

            staminaMoveCost = Mathf.Max(0.01f, 1f * (1f - bonus.staminaMoveCostReduction)),
            staminaAttackCost = Mathf.Max(0.01f, 2f * (1f - bonus.staminaAttackCostReduction)),

            moveSpeed = (2f - progression.StrengthExt * 0.05f + progression.StrengthInt * 0.015f)
                              * (1f + bonus.moveSpeedMult),

            turnSpeed = 6f * (1f + bonus.turnSpeedMult) * Mathf.Clamp(1f - progression.StrengthExt * 0.03f, 0.4f, 2f),

            sizeMultiplier = (progression.StrengthExt * 0.08f - progression.StrengthInt * 0.03f) * (1f + bonus.sizeMult)
        };
        stats.detectionRadius = CombatSettings.BaseDetectionRadius * stats.sizeMultiplier * Mathf.Clamp01(1f - bonus.detectRadiusReduction);
        Debug.Log(stats.detectionRadius);
        Debug.Log(bonus.detectRadiusReduction);
        stats.chitinDamageMultiplierDealt = bonus.chitinDamageMultiplierDealt;
        stats.chitinDamageMultiplierTaken = bonus.chitinDamageMultiplierTaken;
        stats.internalDamageMultiplierDealt = bonus.internalDamageMultiplierDealt;
        stats.internalDamageMultiplierTaken = bonus.internalDamageMultiplierTaken;
        stats.bodyDamageMultiplierDealt = bonus.bodyDamageMultiplierDealt;
        stats.bodyDamageMultiplierTaken = bonus.bodyDamageMultiplierTaken;
        stats.limbDamageMultiplierDealt = bonus.limbDamageMultiplierDealt;
        stats.limbDamageMultiplierTaken = bonus.limbDamageMultiplierTaken;
        stats.legsDamageMultiplierDealt = bonus.legsDamageMultiplierDealt;
        stats.legsDamageMultiplierTaken = bonus.legsDamageMultiplierTaken;
        stats.jawsDamageMultiplierDealt = bonus.jawsDamageMultiplierDealt;
        stats.jawsDamageMultiplierTaken = bonus.jawsDamageMultiplierTaken;
        stats.chitinReflectPercent = bonus.chitinReflectPercent;

        stats.bodyRegenPeriod = Mathf.Max(CombatSettings.MinRegenPeriod, CombatSettings.BaseRegenPeriod - bonus.bodyRegenPeriodReduction);
        stats.bodyRegenPercent = bonus.bodyRegenPercent;
        stats.chitinRegenPeriod = Mathf.Max(CombatSettings.MinRegenPeriod, CombatSettings.BaseRegenPeriod - bonus.chitinRegenPeriodReduction);
        stats.chitinRegenPercent = bonus.chitinRegenPercent;
        stats.jawsRegenPeriod = Mathf.Max(CombatSettings.MinRegenPeriod, CombatSettings.BaseRegenPeriod - bonus.jawsRegenPeriodReduction);
        stats.jawsRegenPercent = bonus.jawsRegenPercent;
        stats.legsRegenPeriod = Mathf.Max(CombatSettings.MinRegenPeriod, CombatSettings.BaseRegenPeriod - bonus.legsRegenPeriodReduction);
        stats.legsRegenPercent = bonus.legsRegenPercent;

        stats.attackVsHealthyMult = 1f + Mathf.Max(0f, bonus.attackVsHealthyMult);
        stats.attackVsLowMult = 1f + Mathf.Max(0f, bonus.attackVsLowMult);
        stats.bleedPercent = bonus.bleedPercent;
        stats.lifestealPercent = bonus.lifestealPercent;
        stats.bleedDurationSeconds = bonus.bleedDurationSeconds;
        stats.jawsRegrowPercent = bonus.jawsRegrowPercent;
        stats.legsRegrowPercent = bonus.legsRegrowPercent;
        stats.chitinRegrowPercent = bonus.chitinRegrowPercent;

        stats.jawsRegrowCooldown = Mathf.Max(CombatSettings.MinRegrowCooldown, CombatSettings.BaseRegrowCooldown - bonus.jawsRegrowCooldownReduction);
        stats.legsRegrowCooldown = Mathf.Max(CombatSettings.MinRegrowCooldown, CombatSettings.BaseRegrowCooldown - bonus.legsRegrowCooldownReduction);
        stats.chitinRegrowCooldown = Mathf.Max(CombatSettings.MinRegrowCooldown, CombatSettings.BaseRegrowCooldown - bonus.chitinRegrowCooldownReduction);

        stats.accidentalDeathChance = bonus.accidentalDeathChance;

        return stats;
    }

    private static CombatBonusAccumulator AggregateBonuses(IReadOnlyList<PlayerBody.BodyPartRuntimeState> bodyParts, int evolutionStage)
    {
        CombatBonusAccumulator bonuses = default;

        foreach (var state in bodyParts)
        {
            foreach (var modifier in state.organ.modifiers)
                AddModifier(ref bonuses, modifier, state.level, evolutionStage);

            foreach (var applied in state.appliedVariants)
                foreach (var modifier in applied.variant.modifiers)
                    AddModifier(ref bonuses, modifier, state.level, evolutionStage);
        }

        return bonuses;
    }

    private static void AddModifier(ref CombatBonusAccumulator bonuses, BodyStatModifier modifier, int level, int evolutionStage)
    {
        float value = modifier.value + modifier.perLevel * level + modifier.perEvolutionStage * evolutionStage;

        switch (modifier.stat)
        {
            case BodyStatType.AttackDamageMult: bonuses.attackDamageMult += value; break;
            case BodyStatType.ChitinDamageMultiplierDealt: bonuses.chitinDamageMultiplierDealt += value; break;
            case BodyStatType.ChitinDamageMultiplierTaken: bonuses.chitinDamageMultiplierTaken += value; break;
            case BodyStatType.InternalDamageMultiplierDealt: bonuses.internalDamageMultiplierDealt += value; break;
            case BodyStatType.InternalDamageMultiplierTaken: bonuses.internalDamageMultiplierTaken += value; break;
            case BodyStatType.BodyDamageMultiplierDealt: bonuses.bodyDamageMultiplierDealt += value; break;
            case BodyStatType.BodyDamageMultiplierTaken: bonuses.bodyDamageMultiplierTaken += value; break;
            case BodyStatType.LimbDamageMultiplierDealt: bonuses.limbDamageMultiplierDealt += value; break;
            case BodyStatType.LimbDamageMultiplierTaken: bonuses.limbDamageMultiplierTaken += value; break;
            case BodyStatType.LegsDamageMultiplierDealt: bonuses.legsDamageMultiplierDealt += value; break;
            case BodyStatType.LegsDamageMultiplierTaken: bonuses.legsDamageMultiplierTaken += value; break;
            case BodyStatType.JawsDamageMultiplierDealt: bonuses.jawsDamageMultiplierDealt += value; break;
            case BodyStatType.JawsDamageMultiplierTaken: bonuses.jawsDamageMultiplierTaken += value; break;
            case BodyStatType.BleedPercent: bonuses.bleedPercent += value; break;
            case BodyStatType.BleedDurationSeconds: bonuses.bleedDurationSeconds += value; break;
            case BodyStatType.LifestealPercent: bonuses.lifestealPercent += value; break;
            case BodyStatType.ChitinReflectPercent: bonuses.chitinReflectPercent += value; break;
            case BodyStatType.MoveSpeedMult: bonuses.moveSpeedMult += value; break;
            case BodyStatType.TurnSpeedMult: bonuses.turnSpeedMult += value; break;
            case BodyStatType.StaminaMoveCostReduction: bonuses.staminaMoveCostReduction += value; break;
            case BodyStatType.StaminaAttackCostReduction: bonuses.staminaAttackCostReduction += value; break;
            case BodyStatType.MaxChitinHpMult: bonuses.maxChitinHpMult += value; break;
            case BodyStatType.MaxBodyHpMult: bonuses.maxBodyHpMult += value; break;
            case BodyStatType.MaxJawHpMult: bonuses.maxJawHpMult += value; break;
            case BodyStatType.MaxLegHpMult: bonuses.maxLegHpMult += value; break;
            case BodyStatType.DetectRadiusReduction: bonuses.detectRadiusReduction += value; break;
            case BodyStatType.SizeMult: bonuses.sizeMult += value; break;
            case BodyStatType.BodyRegenPeriodReduction: bonuses.bodyRegenPeriodReduction += value; break;
            case BodyStatType.BodyRegenPercent: bonuses.bodyRegenPercent += value; break;
            case BodyStatType.ChitinRegenPeriodReduction: bonuses.chitinRegenPeriodReduction += value; break;
            case BodyStatType.ChitinRegenPercent: bonuses.chitinRegenPercent += value; break;
            case BodyStatType.JawsRegenPeriodReduction: bonuses.jawsRegenPeriodReduction += value; break;
            case BodyStatType.JawsRegenPercent: bonuses.jawsRegenPercent += value; break;
            case BodyStatType.LegsRegenPeriodReduction: bonuses.legsRegenPeriodReduction += value; break;
            case BodyStatType.LegsRegenPercent: bonuses.legsRegenPercent += value; break;
            case BodyStatType.AttackVsHealthyMult: bonuses.attackVsHealthyMult += value; break;
            case BodyStatType.AttackVsLowMult: bonuses.attackVsLowMult += value; break;
            case BodyStatType.JawsRegrowPercent: bonuses.jawsRegrowPercent = Mathf.Max(bonuses.jawsRegrowPercent, value); break;
            case BodyStatType.LegsRegrowPercent: bonuses.legsRegrowPercent = Mathf.Max(bonuses.legsRegrowPercent, value); break;
            case BodyStatType.ChitinRegrowPercent: bonuses.chitinRegrowPercent = Mathf.Max(bonuses.chitinRegrowPercent, value); break;
            case BodyStatType.JawsRegrowCooldownReduction: bonuses.jawsRegrowCooldownReduction += value; break;
            case BodyStatType.LegsRegrowCooldownReduction: bonuses.legsRegrowCooldownReduction += value; break;
            case BodyStatType.ChitinRegrowCooldownReduction: bonuses.chitinRegrowCooldownReduction += value; break;
            case BodyStatType.MaxStaminaMult: bonuses.maxStaminaMult += value; break;
            case BodyStatType.StaminaRecoveryMult: bonuses.staminaRecoveryMult += value; break;
            case BodyStatType.AccidentalDeathChance: bonuses.accidentalDeathChance += value; break;
            case BodyStatType.AttackWindupReduction: bonuses.attackWindupReduction += value; break;
            case BodyStatType.AttackActiveTimeReduction: bonuses.attackActiveTimeReduction += value; break;
            case BodyStatType.AttackCooldownReduction: bonuses.attackCooldownReduction += value; break;
            case BodyStatType.ExperienceOnHitMult: bonuses.experienceOnHitMult += value; break;
            case BodyStatType.ExperienceOnHitVsLowerEvolutionMult: bonuses.experienceOnHitVsLowerEvolutionMult += value; break;
            case BodyStatType.ExperienceOnHitVsHigherEvolutionMult: bonuses.experienceOnHitVsHigherEvolutionMult += value; break;
            case BodyStatType.ExperienceOnKillMult: bonuses.experienceOnKillMult += value; break;
            case BodyStatType.ExperienceOnKillVsLowerEvolutionMult: bonuses.experienceOnKillVsLowerEvolutionMult += value; break;
            case BodyStatType.ExperienceOnKillVsHigherEvolutionMult: bonuses.experienceOnKillVsHigherEvolutionMult += value; break;
        }
    }
}