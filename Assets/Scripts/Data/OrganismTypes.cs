using System;

public static class CombatSettings
{
    public const float BaseChitinDamageMultiplier = 0.5f; // percent 0.5 - 50%
    public const float BaseInternalDamageMultiplier = 0.3f; // percent 0.3 - 30%
    public const float BleedingTickInterval = 1f; // in seconds
    public const float BaseRegrowCooldown = 180f; // in seconds
}

public enum BodyPartType
{
    Chitin,
    Jaws,
    Legs,
    Body
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
    ChitinRegenPerSec,
    JawsRegenPerSec,
    LegsRegenPerSec,
    BodyRegenPerSec,
    AttackVsHealthyMult,
    AttackVsLowMult,
    JawsRegrowPercent,
    LegsRegrowPercent,
    ChitinRegrowPercent,
    JawsRegrowCooldownReduction,
    LegsRegrowCooldownReduction,
    ChitinRegrowCooldownReduction
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
    public float chitinRegenPerSec;
    public float jawsRegenPerSec;
    public float legsRegenPerSec;
    public float bodyRegenPerSec;
    public float attackVsHealthyMult;
    public float attackVsLowMult;
    // percent (0..1) of max HP to which the part will be restored when regrow ability is used.
    // 0 means ability not available.
    public float jawsRegrowPercent;
    public float legsRegrowPercent;
    public float chitinRegrowPercent;
    // Cooldown reduction in seconds (applied to base 180s cooldown)
    public float jawsRegrowCooldownReduction;
    public float legsRegrowCooldownReduction;
    public float chitinRegrowCooldownReduction;
}