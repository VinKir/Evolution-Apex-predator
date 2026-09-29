using System;

public static class CombatSettings
{
    public const float BaseChitinDamageMultiplier = 0.5f; // percent 0.5 - 50%
    public const float BaseInternalDamageMultiplier = 0.3f; // percent 0.3 - 30%
    public const float BleedingTickInterval = 1f; // in seconds
    public const float BaseRegrowCooldown = 180f; // in seconds
    public const float MinRegrowCooldown = 20f; // in seconds
    public const float BaseRegenPeriod = 3f; // in seconds
    public const float MinRegenPeriod = 0.5f; // in seconds
    public const float AccidentalDeathCheckInterval = 60f; // in seconds
    public const float BaseAttackWindup = 1f; // in seconds
    public const float BaseAttackActiveTime = 1f; // in seconds
    public const float BaseAttackCooldown = 5f; // in seconds
    public const float BaseExperiencePerHit = 1f;
    public const float BaseExperienceMultiplier = 1f;
}

public enum BodyPartType
{
    Chitin,
    Jaws,
    Legs,
    Body,
    Bonus
}

public enum BodyHitboxSlot
{
    Chitin,
    Body,
    Jaws,
    Legs
}

public enum OrganismBehaviourType
{
    Scavenger,
    Predator,
    Guardian
}

public enum BodyStatType
{
    AttackDamageMult,
    ChitinDamageMultiplierDealt,
    ChitinDamageMultiplierTaken,
    InternalDamageMultiplierDealt,
    InternalDamageMultiplierTaken,
    BodyDamageMultiplierDealt,
    BodyDamageMultiplierTaken,
    LimbDamageMultiplierDealt,
    LimbDamageMultiplierTaken,
    LegsDamageMultiplierDealt,
    LegsDamageMultiplierTaken,
    JawsDamageMultiplierDealt,
    JawsDamageMultiplierTaken,
    BleedPercent,
    BleedDurationSeconds,
    LifestealPercent,
    ChitinReflectPercent,
    MoveSpeedMult,
    TurnSpeedMult,
    StaminaMoveCostReduction,
    StaminaAttackCostReduction,
    MaxChitinHpMult,
    MaxBodyHpMult,
    MaxJawHpMult,
    MaxLegHpMult,
    DetectRadiusReduction,
    SizeMult,
    AttackVsHealthyMult,
    AttackVsLowMult,
    BodyRegenPeriodReduction,
    BodyRegenPercent,
    ChitinRegenPeriodReduction,
    ChitinRegenPercent,
    JawsRegenPeriodReduction,
    JawsRegenPercent,
    LegsRegenPeriodReduction,
    LegsRegenPercent,
    JawsRegrowPercent,
    LegsRegrowPercent,
    ChitinRegrowPercent,
    JawsRegrowCooldownReduction,
    LegsRegrowCooldownReduction,
    ChitinRegrowCooldownReduction,
    MaxStaminaMult,
    StaminaRecoveryMult,
    AccidentalDeathChance,
    AttackWindupReduction,
    AttackActiveTimeReduction,
    AttackCooldownReduction,
    ExperienceOnHitMult,
    ExperienceOnHitVsLowerEvolutionMult,
    ExperienceOnHitVsHigherEvolutionMult,
    ExperienceOnKillMult,
    ExperienceOnKillVsLowerEvolutionMult,
    ExperienceOnKillVsHigherEvolutionMult
}

[Serializable]
public struct BodyStatModifier
{
    public BodyStatType stat;
    public float value;
    public float perLevel;
    public float perEvolutionStage;
}

[Serializable]
public struct CombatBonusAccumulator
{
    public float attackDamageMult;
    public float chitinDamageMultiplierDealt;
    public float chitinDamageMultiplierTaken;
    public float internalDamageMultiplierDealt;
    public float internalDamageMultiplierTaken;
    public float bodyDamageMultiplierDealt;
    public float bodyDamageMultiplierTaken;
    public float limbDamageMultiplierDealt;
    public float limbDamageMultiplierTaken;
    public float legsDamageMultiplierDealt;
    public float legsDamageMultiplierTaken;
    public float jawsDamageMultiplierDealt;
    public float jawsDamageMultiplierTaken;
    public float bleedPercent;
    public float bleedDurationSeconds;
    public float lifestealPercent;
    public float chitinReflectPercent;
    public float moveSpeedMult;
    public float turnSpeedMult;
    public float staminaMoveCostReduction;
    public float staminaAttackCostReduction;
    public float maxChitinHpMult;
    public float maxBodyHpMult;
    public float maxJawHpMult;
    public float maxLegHpMult;
    public float detectRadiusReduction;
    public float sizeMult;
    public float attackVsHealthyMult;
    public float attackVsLowMult;
    public float bodyRegenPeriodReduction;
    public float bodyRegenPercent;
    public float chitinRegenPeriodReduction;
    public float chitinRegenPercent;
    public float jawsRegenPeriodReduction;
    public float jawsRegenPercent;
    public float legsRegenPeriodReduction;
    public float legsRegenPercent;
    // percent (0..1) of max HP to which the part will be restored when regrow ability is used.
    // 0 means ability not available.
    public float jawsRegrowPercent;
    public float legsRegrowPercent;
    public float chitinRegrowPercent;
    // Cooldown reduction in seconds (applied to base 180s cooldown)
    public float jawsRegrowCooldownReduction;
    public float legsRegrowCooldownReduction;
    public float chitinRegrowCooldownReduction;
    public float maxStaminaMult;
    public float staminaRecoveryMult;
    public float accidentalDeathChance;
    public float attackWindupReduction;
    public float attackActiveTimeReduction;
    public float attackCooldownReduction;
    public float experienceOnHitMult;
    public float experienceOnHitVsLowerEvolutionMult;
    public float experienceOnHitVsHigherEvolutionMult;
    public float experienceOnKillMult;
    public float experienceOnKillVsLowerEvolutionMult;
    public float experienceOnKillVsHigherEvolutionMult;
}