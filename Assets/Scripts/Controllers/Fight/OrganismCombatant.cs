using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct AttackPacket
{
    public float rawDamage;
    public float staminaCost;
    public float bleedPercent;
    public float lifestealPercent;
    public float reflectPercent;
    public float bodyBypassBonus;
    public float attackVsHealthyMult;
    public float attackVsLowMult;
}

public struct OrganismRuntimeStats
{
    public float strengthExt;
    public float strengthInt;
    public float enduranceExt;
    public float enduranceInt;

    public float maxChitinHp;
    public float maxBodyHp;
    public float maxJawHp;
    public float maxLegHp;
    public float maxStamina;

    public float moveSpeed;
    public float turnSpeed;
    public float staminaRegen;
    public float detectionRadius;
    public float sizeMultiplier;

    public float attackDamage;
    public float staminaMoveCost;
    public float staminaAttackCost;

    public float chitinDamageTakenMult;
    public float bodyDamageTakenMult;
    public float limbDamageTakenMult;
    public float chitinReflectPercent;

    public float chitinRegenPerSec;
    public float jawsRegenPerSec;
    public float legsRegenPerSec;
    public float bodyRegenPerSec;

    public float attackVsHealthyMult;
    public float attackVsLowMult;
    public float bodyBypassBonus;
    public float bleedPercent;
    public float lifestealPercent;
    public float reflectPercent;

    public bool jawsRegrow;
    public bool legsRegrow;
    public bool chitinRegrow;
}

public class OrganismCombatant : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private bool isPlayer = true;
    [SerializeField] private int factionGroupId = 0;

    [Header("Player Sources")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerBody playerBody;

    [Header("Enemy Source")]
    [SerializeField] private EnemyTemplateSO enemyTemplate;
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private int enemyEvolutionStage = 1;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private AttackHitbox attackHitbox;
    [SerializeField] private FoodItem corpsePrefab;

    [Header("Attack")]
    [SerializeField] private float attackWindup = 0.12f;
    [SerializeField] private float attackActiveTime = 0.18f;
    [SerializeField] private float attackCooldown = 0.55f;

    [Header("Death Drop")]
    [SerializeField] private float corpseBiomassMultiplier = 1f;

    public OrganismRuntimeStats Stats { get; private set; }

    public float CurrentChitinHp { get; private set; }
    public float CurrentBodyHp { get; private set; }
    public float CurrentJawsHp { get; private set; }
    public float CurrentLeftLegHp { get; private set; }
    public float CurrentRightLegHp { get; private set; }
    public float CurrentStamina { get; private set; }

    public float CurrentBodyHpNormalized => Stats.maxBodyHp <= 0.001f ? 0f : CurrentBodyHp / Stats.maxBodyHp;
    public bool IsDead { get; private set; }

    public int FactionGroupId => factionGroupId;
    public float CombatPower => Stats.attackDamage + Stats.maxBodyHp * 0.15f + Stats.maxChitinHp * 0.1f + Stats.moveSpeed * 0.5f;

    public event Action<OrganismCombatant> OnDamagedBy;
    public event Action OnRecalculated;
    public event Action OnDied;

    private bool attackBusy;
    private float nextAttackTime;
    private readonly List<Coroutine> runningCoroutines = new();

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (attackHitbox != null)
            attackHitbox.Setup(this);

        HookSources();
        RecalculateStats();
    }

    private void OnEnable()
    {
        HookSources();
        RecalculateStats();
    }

    private void OnDisable()
    {
        UnhookSources();
    }

    private void Update()
    {
        if (IsDead)
            return;

        Regenerate(Time.deltaTime);

        if (rb != null)
        {
            float speed01 = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.01f, Stats.moveSpeed)); // TODO: ����������� �������� �� linearVelocity

            if (speed01 > 0.08f)
                SpendStamina(Stats.staminaMoveCost * speed01 * Time.deltaTime);
            else
                RestoreStamina(Stats.staminaRegen * Time.deltaTime);
        }
        else
        {
            RestoreStamina(Stats.staminaRegen * Time.deltaTime);
        }
    }

    private void HookSources()
    {
        if (isPlayer)
        {
            if (playerProgression != null)
                playerProgression.OnChanged += RecalculateStats;

            if (playerBody != null)
                playerBody.OnBodyChanged += RecalculateStats;
        }
    }

    private void UnhookSources()
    {
        if (isPlayer)
        {
            if (playerProgression != null)
                playerProgression.OnChanged -= RecalculateStats;

            if (playerBody != null)
                playerBody.OnBodyChanged -= RecalculateStats;
        }
    }

    public void ConfigureEnemy(EnemyTemplateSO template, int level, int evoStage, int groupId)
    {
        isPlayer = false;
        enemyTemplate = template;
        enemyLevel = level;
        enemyEvolutionStage = evoStage;
        factionGroupId = groupId;

        RecalculateStats();
    }

    public void RecalculateStats()
    {
        float oldChitinRatio = Stats.maxChitinHp > 0.001f ? CurrentChitinHp / Stats.maxChitinHp : 1f;
        float oldBodyRatio = Stats.maxBodyHp > 0.001f ? CurrentBodyHp / Stats.maxBodyHp : 1f;
        float oldJawRatio = Stats.maxJawHp > 0.001f ? CurrentJawsHp / Stats.maxJawHp : 1f;
        float oldLegLRatio = Stats.maxLegHp > 0.001f ? CurrentLeftLegHp / Stats.maxLegHp : 1f;
        float oldLegRRatio = Stats.maxLegHp > 0.001f ? CurrentRightLegHp / Stats.maxLegHp : 1f;
        float oldStaminaRatio = Stats.maxStamina > 0.001f ? CurrentStamina / Stats.maxStamina : 1f;

        Stats = isPlayer
            ? BuildPlayerStats()
            : BuildEnemyStats();

        CurrentChitinHp = Mathf.Clamp(Stats.maxChitinHp * oldChitinRatio, 0f, Stats.maxChitinHp);
        CurrentBodyHp = Mathf.Clamp(Stats.maxBodyHp * oldBodyRatio, 0f, Stats.maxBodyHp);
        CurrentJawsHp = Mathf.Clamp(Stats.maxJawHp * oldJawRatio, 0f, Stats.maxJawHp);
        CurrentLeftLegHp = Mathf.Clamp(Stats.maxLegHp * oldLegLRatio, 0f, Stats.maxLegHp);
        CurrentRightLegHp = Mathf.Clamp(Stats.maxLegHp * oldLegRRatio, 0f, Stats.maxLegHp);
        CurrentStamina = Mathf.Clamp(Stats.maxStamina * oldStaminaRatio, 0f, Stats.maxStamina);

        if (CurrentChitinHp <= 0f) CurrentChitinHp = Stats.maxChitinHp;
        if (CurrentBodyHp <= 0f) CurrentBodyHp = Stats.maxBodyHp;
        if (CurrentJawsHp <= 0f) CurrentJawsHp = Stats.maxJawHp;
        if (CurrentLeftLegHp <= 0f) CurrentLeftLegHp = Stats.maxLegHp;
        if (CurrentRightLegHp <= 0f) CurrentRightLegHp = Stats.maxLegHp;
        if (CurrentStamina <= 0f) CurrentStamina = Stats.maxStamina;

        OnRecalculated?.Invoke();
    }

    private OrganismRuntimeStats BuildPlayerStats()
    {
        var p = playerProgression;
        var body = playerBody;

        int level = p != null ? p.Level : 1;
        int evo = p != null ? p.EvolutionStage : 1;

        float levelFactor = 1f + 0.03f * Mathf.Max(0, level - 1);
        float evoFactor = 1f + 0.12f * Mathf.Max(0, evo - 1);

        CombatBonusAccumulator bonus = AggregatePlayerBonuses();

        OrganismRuntimeStats s = new OrganismRuntimeStats();

        s.strengthExt = 1.0f * levelFactor * evoFactor;
        s.strengthInt = 1.0f * levelFactor * evoFactor;
        s.enduranceExt = 1.0f * levelFactor * evoFactor;
        s.enduranceInt = 1.0f * levelFactor * evoFactor;

        s.maxChitinHp = 20f * s.enduranceExt * (1f + bonus.maxChitinHpMult);
        s.maxBodyHp = 10f * s.enduranceInt * (1f + bonus.maxBodyHpMult);
        s.maxJawHp = 8f * s.enduranceInt * (1f + bonus.maxJawHpMult);
        s.maxLegHp = 5f * s.enduranceInt * (1f + bonus.maxLegHpMult);

        s.maxStamina = 20f + s.strengthInt * 8f + s.enduranceInt * 12f;
        s.staminaRegen = 5f + s.strengthInt * 0.5f + s.enduranceInt * 1.0f;

        s.attackDamage = s.strengthExt * (1f + bonus.attackDamageMult);
        s.staminaMoveCost = Mathf.Max(0.01f, 2f * (1f + bonus.staminaMoveCostMult));
        s.staminaAttackCost = Mathf.Max(0.01f, 4f * (1f + bonus.staminaAttackCostMult));

        s.moveSpeed = 3.5f
                      * (1f + bonus.moveSpeedMult)
                      * Mathf.Clamp(1f - s.strengthExt * 0.05f + s.strengthInt * 0.015f, 0.35f, 3f);

        s.turnSpeed = 6f * (1f + bonus.turnSpeedMult) * Mathf.Clamp(1f - s.strengthExt * 0.03f, 0.4f, 2f);

        s.sizeMultiplier = Mathf.Clamp(
            1f + s.strengthExt * 0.08f - s.strengthInt * 0.03f + bonus.sizeMult,
            0.55f,
            3f
        );

        s.detectionRadius = 4.5f * s.sizeMultiplier * (1f + bonus.detectRadiusMult);

        s.chitinDamageTakenMult = bonus.chitinDamageTakenMult;
        s.bodyDamageTakenMult = bonus.bodyDamageTakenMult;
        s.limbDamageTakenMult = bonus.limbDamageTakenMult;
        s.chitinReflectPercent = bonus.reflectPercent;

        s.chitinRegenPerSec = bonus.chitinRegenPerSec;
        s.jawsRegenPerSec = bonus.jawsRegenPerSec;
        s.legsRegenPerSec = bonus.legsRegenPerSec;
        s.bodyRegenPerSec = bonus.bodyRegenPerSec;

        s.attackVsHealthyMult = bonus.attackVsHealthyMult <= 0f ? 1f : bonus.attackVsHealthyMult;
        s.attackVsLowMult = bonus.attackVsLowMult <= 0f ? 1f : bonus.attackVsLowMult;
        s.bodyBypassBonus = bonus.bodyBypassBonus;
        s.bleedPercent = bonus.bleedPercent;
        s.lifestealPercent = bonus.lifestealPercent;
        s.jawsRegrow = bonus.jawsRegrow;
        s.legsRegrow = bonus.legsRegrow;
        s.chitinRegrow = bonus.chitinRegrow;

        return s;
    }

    private OrganismRuntimeStats BuildEnemyStats()
    {
        float level = Mathf.Max(1, enemyLevel);
        float evo = Mathf.Max(1, enemyEvolutionStage);

        float levelFactor = 1f + 0.04f * (level - 1f);
        float evoFactor = 1f + 0.10f * (evo - 1f);

        OrganismRuntimeStats s = new OrganismRuntimeStats();

        float strengthExt = enemyTemplate != null ? RandomInRange(enemyTemplate.baseStrengthExtMin, enemyTemplate.baseStrengthExtMax) : 1f;
        float strengthInt = enemyTemplate != null ? RandomInRange(enemyTemplate.baseStrengthIntMin, enemyTemplate.baseStrengthIntMax) : 1f;
        float enduranceExt = enemyTemplate != null ? RandomInRange(enemyTemplate.baseEnduranceExtMin, enemyTemplate.baseEnduranceExtMax) : 1f;
        float enduranceInt = enemyTemplate != null ? RandomInRange(enemyTemplate.baseEnduranceIntMin, enemyTemplate.baseEnduranceIntMax) : 1f;

        s.strengthExt = strengthExt * levelFactor * evoFactor;
        s.strengthInt = strengthInt * levelFactor * evoFactor;
        s.enduranceExt = enduranceExt * levelFactor * evoFactor;
        s.enduranceInt = enduranceInt * levelFactor * evoFactor;

        s.maxChitinHp = 20f * s.enduranceExt;
        s.maxBodyHp = 10f * s.enduranceInt;
        s.maxJawHp = 8f * s.enduranceInt;
        s.maxLegHp = 5f * s.enduranceInt;

        s.maxStamina = 15f + s.strengthInt * 6f + s.enduranceInt * 8f;
        s.staminaRegen = 4f + s.enduranceInt * 0.8f;

        s.attackDamage = s.strengthExt;
        s.staminaMoveCost = 2f;
        s.staminaAttackCost = 4f;

        float speedBase = enemyTemplate != null ? RandomInRange(enemyTemplate.baseMoveSpeedMin, enemyTemplate.baseMoveSpeedMax) : 3f;
        float turnBase = enemyTemplate != null ? RandomInRange(enemyTemplate.baseTurnSpeedMin, enemyTemplate.baseTurnSpeedMax) : 5f;
        float detectBase = enemyTemplate != null ? RandomInRange(enemyTemplate.baseDetectionRadiusMin, enemyTemplate.baseDetectionRadiusMax) : 4.5f;

        s.moveSpeed = speedBase * Mathf.Clamp(1f - s.strengthExt * 0.04f + s.strengthInt * 0.01f, 0.4f, 2.5f);
        s.turnSpeed = turnBase * Mathf.Clamp(1f - s.strengthExt * 0.02f, 0.5f, 2f);
        s.sizeMultiplier = Mathf.Clamp(1f + s.strengthExt * 0.06f - s.strengthInt * 0.02f, 0.65f, 2.5f);
        s.detectionRadius = detectBase * s.sizeMultiplier;

        return s;
    }

    private CombatBonusAccumulator AggregatePlayerBonuses()
    {
        CombatBonusAccumulator b = new CombatBonusAccumulator
        {
            attackVsHealthyMult = 1f,
            attackVsLowMult = 1f
        };

        if (playerBody == null)
            return b;

        foreach (var state in playerBody.States)
        {
            if (state == null || state.definition == null)
                continue;

            foreach (var applied in state.appliedVariants)
            {
                if (applied?.variant == null)
                    continue;

                int partLevel = state.level;
                int evo = playerProgression != null ? playerProgression.EvolutionStage : 1;

                foreach (var mod in applied.variant.modifiers)
                {
                    float v = mod.value + mod.perLevel * partLevel + mod.perEvolutionStage * evo;

                    switch (mod.stat)
                    {
                        case BodyStatType.AttackDamageMult: b.attackDamageMult += v; break;
                        case BodyStatType.ChitinDamageTakenMult: b.chitinDamageTakenMult += v; break;
                        case BodyStatType.BodyDamageTakenMult: b.bodyDamageTakenMult += v; break;
                        case BodyStatType.LimbDamageTakenMult: b.limbDamageTakenMult += v; break;
                        case BodyStatType.BodyBypassBonus: b.bodyBypassBonus += v; break;
                        case BodyStatType.BleedPercent: b.bleedPercent += v; break;
                        case BodyStatType.LifestealPercent: b.lifestealPercent += v; break;
                        case BodyStatType.ReflectPercent: b.reflectPercent += v; break;
                        case BodyStatType.MoveSpeedMult: b.moveSpeedMult += v; break;
                        case BodyStatType.TurnSpeedMult: b.turnSpeedMult += v; break;
                        case BodyStatType.StaminaMoveCostMult: b.staminaMoveCostMult += v; break;
                        case BodyStatType.StaminaAttackCostMult: b.staminaAttackCostMult += v; break;
                        case BodyStatType.MaxChitinHpMult: b.maxChitinHpMult += v; break;
                        case BodyStatType.MaxBodyHpMult: b.maxBodyHpMult += v; break;
                        case BodyStatType.MaxJawHpMult: b.maxJawHpMult += v; break;
                        case BodyStatType.MaxLegHpMult: b.maxLegHpMult += v; break;
                        case BodyStatType.DetectRadiusMult: b.detectRadiusMult += v; break;
                        case BodyStatType.SizeMult: b.sizeMult += v; break;
                        case BodyStatType.ChitinRegenPerSec: b.chitinRegenPerSec += v; break;
                        case BodyStatType.JawsRegenPerSec: b.jawsRegenPerSec += v; break;
                        case BodyStatType.LegsRegenPerSec: b.legsRegenPerSec += v; break;
                        case BodyStatType.BodyRegenPerSec: b.bodyRegenPerSec += v; break;
                        case BodyStatType.AttackVsHealthyMult: b.attackVsHealthyMult = Mathf.Max(b.attackVsHealthyMult, v); break;
                        case BodyStatType.AttackVsLowMult: b.attackVsLowMult = Mathf.Max(b.attackVsLowMult, v); break;
                        case BodyStatType.JawsRegrow: if (v > 0f) b.jawsRegrow = true; break;
                        case BodyStatType.LegsRegrow: if (v > 0f) b.legsRegrow = true; break;
                        case BodyStatType.ChitinRegrow: if (v > 0f) b.chitinRegrow = true; break;
                    }
                }
            }
        }

        return b;
    }

    private float RandomInRange(float min, float max)
    {
        return UnityEngine.Random.Range(min, max);
    }

    public void SpendStamina(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentStamina = Mathf.Min(Stats.maxStamina, CurrentStamina + amount);
    }

    private void Regenerate(float dt)
    {
        CurrentChitinHp = Mathf.Min(Stats.maxChitinHp, CurrentChitinHp + Stats.chitinRegenPerSec * dt * Stats.maxChitinHp);
        CurrentBodyHp = Mathf.Min(Stats.maxBodyHp, CurrentBodyHp + Stats.bodyRegenPerSec * dt * Stats.maxBodyHp);
        CurrentJawsHp = Mathf.Min(Stats.maxJawHp, CurrentJawsHp + Stats.jawsRegenPerSec * dt * Stats.maxJawHp);
        CurrentLeftLegHp = Mathf.Min(Stats.maxLegHp, CurrentLeftLegHp + Stats.legsRegenPerSec * dt * Stats.maxLegHp);
        CurrentRightLegHp = Mathf.Min(Stats.maxLegHp, CurrentRightLegHp + Stats.legsRegenPerSec * dt * Stats.maxLegHp);
    }

    public void TryStartMeleeAttack()
    {
        if (IsDead || attackBusy || Time.time < nextAttackTime)
            return;

        float staminaCost = Stats.staminaAttackCost;
        if (CurrentStamina < staminaCost)
            return;

        SpendStamina(staminaCost);
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        attackBusy = true;
        nextAttackTime = Time.time + attackCooldown;

        if (attackHitbox != null)
            attackHitbox.SetActiveSwing(false);

        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox != null)
            attackHitbox.SetActiveSwing(true);

        yield return new WaitForSeconds(attackActiveTime);

        if (attackHitbox != null)
            attackHitbox.SetActiveSwing(false);

        attackBusy = false;
    }

    public AttackPacket BuildAttackPacket()
    {
        var bonuses = AggregatePlayerBonuses();

        return new AttackPacket
        {
            rawDamage = Stats.attackDamage * (1f + bonuses.attackDamageMult),
            staminaCost = Stats.staminaAttackCost * (1f + bonuses.staminaAttackCostMult),
            bleedPercent = bonuses.bleedPercent,
            lifestealPercent = bonuses.lifestealPercent,
            reflectPercent = bonuses.reflectPercent,
            bodyBypassBonus = bonuses.bodyBypassBonus,
            attackVsHealthyMult = bonuses.attackVsHealthyMult,
            attackVsLowMult = bonuses.attackVsLowMult
        };
    }

    public void ResolveMeleeHit(OrganismCombatant defender, BodyHitbox hitbox)
    {
        if (defender == null || defender == this)
            return;

        if (defender.FactionGroupId == FactionGroupId)
            return;

        var packet = BuildAttackPacket();
        defender.TakeAttack(this, packet, hitbox);
    }

    public void TakeAttack(OrganismCombatant attacker, AttackPacket packet, BodyHitbox hitbox)
    {
        if (IsDead)
            return;

        if (attacker != null && attacker.FactionGroupId == factionGroupId && attacker != this)
            return;

        OnDamagedBy?.Invoke(attacker);

        float damage = packet.rawDamage;

        if (CurrentBodyHpNormalized > 0.9f)
            damage *= packet.attackVsHealthyMult;

        if (CurrentBodyHpNormalized > 0f && CurrentBodyHpNormalized < 0.3f)
            damage *= packet.attackVsLowMult;

        bool hadChitin = CurrentChitinHp > 0f;

        float chitinDamage = hadChitin ? damage * Mathf.Max(0f, 0.5f + Stats.chitinDamageTakenMult + attacker?.Stats.chitinDamageTakenMult ?? 0f) : 0f;
        float partShare = hadChitin ? 0.3f : 1f;

        float defenderBodyBypassReduction = 0f;
        if (hadChitin)
        {
            defenderBodyBypassReduction = GetDefensiveBypassReduction();
        }

        float finalBypass = Mathf.Max(0f, packet.bodyBypassBonus - defenderBodyBypassReduction);

        float slotDamage = damage * Mathf.Max(0f, partShare + finalBypass);

        switch (hitbox != null ? hitbox.slot : BodyHitboxSlot.Body)
        {
            case BodyHitboxSlot.Chitin:
                ApplyChitinDamage(chitinDamage > 0f ? chitinDamage : damage, attacker);
                break;

            case BodyHitboxSlot.Jaws:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyJawsDamage(slotDamage, hadChitin);
                break;

            case BodyHitboxSlot.LeftLeg:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyLegDamage(true, slotDamage, hadChitin);
                break;

            case BodyHitboxSlot.RightLeg:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyLegDamage(false, slotDamage, hadChitin);
                break;

            case BodyHitboxSlot.Body:
            default:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyBodyDamage(slotDamage, hadChitin);
                break;
        }

        if (attacker != null && packet.reflectPercent > 0f)
        {
            float reflected = damage * packet.reflectPercent;
            attacker.TakeReflectedDamage(reflected);
        }

        if (packet.lifestealPercent > 0f)
        {
            float heal = damage * packet.lifestealPercent;
            HealBody(heal);
        }

        if (CurrentChitinHp <= 0f && hadChitin && Stats.chitinReflectPercent > 0f && attacker != null)
        {
            float extraReflect = damage * Stats.chitinReflectPercent;
            attacker.TakeReflectedDamage(extraReflect);
        }

        CheckDeath();
    }

    private float GetDefensiveBypassReduction()
    {
        return 0f;
    }

    private void ApplyChitinDamage(float amount, OrganismCombatant attacker)
    {
        if (amount <= 0f)
            return;

        CurrentChitinHp = Mathf.Max(0f, CurrentChitinHp - amount);
        if (CurrentChitinHp <= 0f)
            CurrentChitinHp = 0f;
    }

    private void ApplyBodyDamage(float amount, bool hadChitin)
    {
        if (amount <= 0f)
            return;

        float mult = 1f + Stats.bodyDamageTakenMult;
        float reduced = amount * mult;
        CurrentBodyHp = Mathf.Max(0f, CurrentBodyHp - reduced);
    }

    private void ApplyJawsDamage(float amount, bool hadChitin)
    {
        if (amount <= 0f)
            return;

        float mult = 1f + Stats.bodyDamageTakenMult;
        CurrentJawsHp = Mathf.Max(0f, CurrentJawsHp - amount * mult);
    }

    private void ApplyLegDamage(bool left, float amount, bool hadChitin)
    {
        if (amount <= 0f)
            return;

        float mult = 1f + Stats.limbDamageTakenMult;
        float reduced = amount * mult;

        if (left)
            CurrentLeftLegHp = Mathf.Max(0f, CurrentLeftLegHp - reduced);
        else
            CurrentRightLegHp = Mathf.Max(0f, CurrentRightLegHp - reduced);
    }

    public void TakeReflectedDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        CurrentBodyHp = Mathf.Max(0f, CurrentBodyHp - amount);
        CheckDeath();
    }

    public void HealBody(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentBodyHp = Mathf.Min(Stats.maxBodyHp, CurrentBodyHp + amount);
    }

    public void ApplyFoodGain(float biomass)
    {
        if (biomass <= 0f)
            return;

        CurrentBodyHp = Mathf.Min(Stats.maxBodyHp, CurrentBodyHp + biomass * 0.75f);
        CurrentChitinHp = Mathf.Min(Stats.maxChitinHp, CurrentChitinHp + biomass * 0.35f);
        CurrentJawsHp = Mathf.Min(Stats.maxJawHp, CurrentJawsHp + biomass * 0.35f);
        CurrentLeftLegHp = Mathf.Min(Stats.maxLegHp, CurrentLeftLegHp + biomass * 0.35f);
        CurrentRightLegHp = Mathf.Min(Stats.maxLegHp, CurrentRightLegHp + biomass * 0.35f);
        CurrentStamina = Mathf.Min(Stats.maxStamina, CurrentStamina + biomass * 2f);
    }

    private void CheckDeath()
    {
        if (IsDead)
            return;

        bool noBody = CurrentBodyHp <= 0f;
        bool noChitin = CurrentChitinHp <= 0f && CurrentBodyHp <= 0f;

        if (noBody && noChitin)
            Die();
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (corpsePrefab != null)
        {
            float biomass = EstimateCorpseBiomass();
            var corpse = Instantiate(corpsePrefab, transform.position, Quaternion.identity);
            corpse.InitializeRuntime(biomass, 10f);
        }

        OnDied?.Invoke();
        Destroy(gameObject);
    }

    private float EstimateCorpseBiomass()
    {
        float sum = Stats.maxBodyHp + Stats.maxChitinHp * 0.5f + Stats.maxJawHp * 0.3f + Stats.maxLegHp * 0.3f * 2f;
        return Mathf.Max(1f, sum * corpseBiomassMultiplier * 0.05f);
    }

    public bool CanSee(Transform target, float extraRange = 0f)
    {
        if (target == null)
            return false;

        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= Stats.detectionRadius + extraRange;
    }

    public bool IsFriendlyTo(OrganismCombatant other)
    {
        if (other == null)
            return false;

        return other.factionGroupId == factionGroupId;
    }
}