using System;

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
    LeftLeg,
    RightLeg
}

public enum EnemyBehaviorType
{
    Scavenger,
    Predator,
    Guardian
}

public enum BodyStatType
{
    AttackDamageMult,
    ChitinDamageTakenMult,
    BodyDamageTakenMult,
    LimbDamageTakenMult,
    BodyBypassBonus,
    BleedPercent,
    LifestealPercent,
    ReflectPercent,
    MoveSpeedMult,
    TurnSpeedMult,
    StaminaMoveCostMult,
    StaminaAttackCostMult,
    MaxChitinHpMult,
    MaxBodyHpMult,
    MaxJawHpMult,
    MaxLegHpMult,
    DetectRadiusMult,
    SizeMult,
    ChitinRegenPerSec,
    JawsRegenPerSec,
    LegsRegenPerSec,
    BodyRegenPerSec,
    AttackVsHealthyMult,
    AttackVsLowMult,
    JawsRegrow,
    LegsRegrow,
    ChitinRegrow
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
    public float chitinDamageTakenMult;
    public float bodyDamageTakenMult;
    public float limbDamageTakenMult;
    public float bodyBypassBonus;
    public float bleedPercent;
    public float lifestealPercent;
    public float reflectPercent;
    public float moveSpeedMult;
    public float turnSpeedMult;
    public float staminaMoveCostMult;
    public float staminaAttackCostMult;
    public float maxChitinHpMult;
    public float maxBodyHpMult;
    public float maxJawHpMult;
    public float maxLegHpMult;
    public float detectRadiusMult;
    public float sizeMult;
    public float chitinRegenPerSec;
    public float jawsRegenPerSec;
    public float legsRegenPerSec;
    public float bodyRegenPerSec;
    public float attackVsHealthyMult;
    public float attackVsLowMult;
    public bool jawsRegrow;
    public bool legsRegrow;
    public bool chitinRegrow;
}