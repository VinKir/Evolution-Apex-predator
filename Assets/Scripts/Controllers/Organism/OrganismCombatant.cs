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

    // percent (0..1) chance/amount to which part will be restored by regrow ability
    public float jawsRegrowPercent;
    public float legsRegrowPercent;
    public float chitinRegrowPercent;

    // Cooldown for regrow abilities in seconds
    public float jawsRegrowCooldown;
    public float legsRegrowCooldown;
    public float chitinRegrowCooldown;
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
    [SerializeField] private int enemyLevel = 0;
    [SerializeField] private int enemyEvolutionStage = 0;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private OrganismMovementMotor movement;
    [SerializeField] private AttackHitbox attackHitbox;
    [SerializeField] private FoodItem corpsePrefab;
    [Header("Visuals")]
    [SerializeField] private Transform graphicsRoot;
    [SerializeField] private Transform statusBarAnchor;
    [SerializeField] private SpriteRenderer chitinCracks;
    [SerializeField] private SpriteRenderer chitinBroken;

    [Header("Attack")]
    [SerializeField] private float attackWindup = 0.12f;
    [SerializeField] private float attackActiveTime = 0.18f;
    [SerializeField] private float attackCooldown = 0.55f;

    [Header("Death Drop")]
    [SerializeField] private float corpseBiomassMultiplier = 1f;

    public OrganismRuntimeStats Stats { get; private set; }
    public Transform StatusBarAnchor => statusBarAnchor;

    public float CurrentChitinHp { get; private set; }
    public float CurrentBodyHp { get; private set; }
    public float CurrentJawsHp { get; private set; }
    public float CurrentLeftLegHp { get; private set; }
    public float CurrentRightLegHp { get; private set; }
    public float CurrentStamina { get; private set; }
    public bool LegsDisabled => legsDisabled;

    public float CurrentBodyHpNormalized => Stats.maxBodyHp <= 0.001f ? 0f : CurrentBodyHp / Stats.maxBodyHp;
    public bool IsDead { get; private set; }

    public int FactionGroupId => factionGroupId;
    public float CombatPower => Stats.strengthExt + Stats.strengthInt + Stats.enduranceExt + Stats.enduranceInt + enemyLevel + enemyEvolutionStage * 10 + (playerProgression != null ? playerProgression.Level + playerProgression.EvolutionStage * 10 : 0);

    public event Action<OrganismCombatant> OnDamagedBy;
    public event Action OnChitinHpChanged;
    public event Action OnBodyHpChanged;
    public event Action OnJawsHpChanged;
    public event Action OnLegsHpChanged;
    public event Action OnStaminaChanged;
    public event Action OnRecalculated;
    public event Action OnDied;

    private bool attackBusy;
    private float nextAttackTime;
    private readonly List<Coroutine> runningCoroutines = new();
    private bool statsInitialized = false;

    // Visual part roots and disabled flags
    private Transform graphicsChitin;
    private Transform graphicsJaws;
    private Transform graphicsLegs;

    private bool chitinDisabled = false;
    private bool jawsDisabled = false;
    private bool legsDisabled = false;

    private float lastRegrowChitinTime = -9999f;
    private float lastRegrowJawsTime = -9999f;
    private float lastRegrowLegsTime = -9999f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (movement == null)
            movement = GetComponent<OrganismMovementMotor>();
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (attackHitbox != null)
            attackHitbox.Setup(this);

        HookSources();
        RecalculateStats();

        // find graphics parts under graphicsRoot if available
        if (graphicsRoot == null)
        {
            graphicsRoot = transform.Find("graphics") ?? transform.Find("Graphics");
        }

        if (graphicsRoot != null)
        {
            graphicsChitin = graphicsRoot.Find("chitin") ?? graphicsRoot.Find("Chitin");
            graphicsJaws = graphicsRoot.Find("jaws") ?? graphicsRoot.Find("Jaws");
            graphicsLegs = graphicsRoot.Find("legs") ?? graphicsRoot.Find("Legs");
        }
    }

    private void Start()
    {
        OrganismCombatantRegistry.Instance?.Register(this);
        
        OnChitinHpChanged?.Invoke();
        OnBodyHpChanged?.Invoke();
        OnJawsHpChanged?.Invoke();
        OnLegsHpChanged?.Invoke();
        OnStaminaChanged?.Invoke();
    }

    private void OnEnable()
    {
        HookSources();
        RecalculateStats();
        OnChitinHpChanged += UpdateChitinVisuals;
        OrganismCombatantRegistry.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UnhookSources();
        OnChitinHpChanged -= UpdateChitinVisuals;
        OrganismCombatantRegistry.Instance?.Unregister(this);
    }

    private void OnDestroy()
    {
        OrganismCombatantRegistry.Instance?.Unregister(this);
    }

    private void Update()
    {
        if (IsDead)
            return;

        Regenerate(Time.deltaTime);

        if (movement != null && movement.IsSelfMoving)
        {
            float speed01 = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.01f, movement.CurrentMoveSpeed));
            SpendStamina(Stats.staminaMoveCost * speed01 * Time.deltaTime);
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
                playerProgression.OnEvolve += RecalculateStats;

            if (playerBody != null)
                playerBody.OnBodyChanged += RecalculateStats;
        }
    }

    private void UnhookSources()
    {
        if (isPlayer)
        {
            if (playerProgression != null)
                playerProgression.OnEvolve -= RecalculateStats;

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

        // On first initialization, fill HP to max. Afterwards, preserve zero HP (so broken parts stay broken).
        if (!statsInitialized)
        {
            CurrentChitinHp = Mathf.Max(CurrentChitinHp, Stats.maxChitinHp);
            CurrentBodyHp = Mathf.Max(CurrentBodyHp, Stats.maxBodyHp);
            CurrentJawsHp = Mathf.Max(CurrentJawsHp, Stats.maxJawHp);
            CurrentLeftLegHp = Mathf.Max(CurrentLeftLegHp, Stats.maxLegHp);
            CurrentRightLegHp = Mathf.Max(CurrentRightLegHp, Stats.maxLegHp);
            CurrentStamina = Mathf.Max(CurrentStamina, Stats.maxStamina);
            statsInitialized = true;
        }

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

        // TODO: значения strengthExt и т.д. должны повышаться игроком, он сам выбирает при эволюции какую характеристику увеличить
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
        s.jawsRegrowPercent = bonus.jawsRegrowPercent;
        s.legsRegrowPercent = bonus.legsRegrowPercent;
        s.chitinRegrowPercent = bonus.chitinRegrowPercent;

        // Regrow cooldowns: base 180 seconds, reduced by bonuses
        const float baseRegrowCooldown = 180f;
        s.jawsRegrowCooldown = Mathf.Max(5f, baseRegrowCooldown - bonus.jawsRegrowCooldownReduction);
        s.legsRegrowCooldown = Mathf.Max(5f, baseRegrowCooldown - bonus.legsRegrowCooldownReduction);
        s.chitinRegrowCooldown = Mathf.Max(5f, baseRegrowCooldown - bonus.chitinRegrowCooldownReduction);

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
                        case BodyStatType.JawsRegrow: b.jawsRegrowPercent = Mathf.Max(b.jawsRegrowPercent, v); break;
                        case BodyStatType.LegsRegrow: b.legsRegrowPercent = Mathf.Max(b.legsRegrowPercent, v); break;
                        case BodyStatType.ChitinRegrow: b.chitinRegrowPercent = Mathf.Max(b.chitinRegrowPercent, v); break;
                        case BodyStatType.JawsRegrowCooldownReduction: b.jawsRegrowCooldownReduction += v; break;
                        case BodyStatType.LegsRegrowCooldownReduction: b.legsRegrowCooldownReduction += v; break;
                        case BodyStatType.ChitinRegrowCooldownReduction: b.chitinRegrowCooldownReduction += v; break;
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

    private void UpdateChitinVisuals()
    {
        if (chitinCracks == null && chitinBroken == null)
            return;

        float damagePercent = 0f;

        if (Stats.maxChitinHp > 0f)
            damagePercent = Mathf.Clamp01(1f - (CurrentChitinHp / Stats.maxChitinHp));

        // Completely broken chitin
        if (CurrentChitinHp <= 0f)
        {
            if (chitinCracks != null)
                chitinCracks.gameObject.SetActive(false);

            if (chitinBroken != null)
                chitinBroken.gameObject.SetActive(true);

            return;
        }

        // Chitin exists again -> disable broken state
        if (chitinBroken != null)
            chitinBroken.gameObject.SetActive(false);

        if (chitinCracks != null)
        {
            chitinCracks.gameObject.SetActive(true);

            Color color = chitinCracks.color;
            color.a = damagePercent;
            chitinCracks.color = color;
            Debug.Log("AAAA" + color.a);
        }
    }

    public void SpendStamina(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentStamina = Mathf.Max(0f,  CurrentStamina - amount);
        OnStaminaChanged?.Invoke();
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
            return;
 
        CurrentStamina = Mathf.Min(Stats.maxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke();
    }

    private void Regenerate(float dt)
    {
        if (Stats.chitinRegenPerSec > 0f && !chitinDisabled && CurrentChitinHp < Stats.maxChitinHp)
        {
            CurrentChitinHp = Mathf.Min(Stats.maxChitinHp, CurrentChitinHp + Stats.chitinRegenPerSec * dt * Stats.maxChitinHp);
            OnChitinHpChanged?.Invoke();
        }

        if (Stats.bodyRegenPerSec > 0f && CurrentBodyHp < Stats.maxBodyHp)
        {
            CurrentBodyHp = Mathf.Min(Stats.maxBodyHp, CurrentBodyHp + Stats.bodyRegenPerSec * dt * Stats.maxBodyHp);
            OnBodyHpChanged?.Invoke();
        }

        if (Stats.jawsRegenPerSec > 0f && !jawsDisabled && CurrentJawsHp < Stats.maxJawHp)
        {
            CurrentJawsHp = Mathf.Min(Stats.maxJawHp, CurrentJawsHp + Stats.jawsRegenPerSec * dt * Stats.maxJawHp);
            OnJawsHpChanged?.Invoke();
        }

        if (Stats.legsRegenPerSec > 0f && !legsDisabled)
        {
            bool changed = false;

            if (CurrentLeftLegHp < Stats.maxLegHp)
            {
                CurrentLeftLegHp = Mathf.Min(Stats.maxLegHp, CurrentLeftLegHp + Stats.legsRegenPerSec * dt * Stats.maxLegHp);
                changed = true;
            }

            if (CurrentRightLegHp < Stats.maxLegHp)
            {
                CurrentRightLegHp = Mathf.Min(Stats.maxLegHp, CurrentRightLegHp + Stats.legsRegenPerSec * dt * Stats.maxLegHp);
                changed = true;
            }

            if (changed)
                OnLegsHpChanged?.Invoke();
        }
    }

    public void TryStartMeleeAttack()
    {
        if (IsDead || attackBusy || Time.time < nextAttackTime)
            return;

        // cannot attack without jaws
        if (CurrentJawsHp <= 0f)
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

        if (animator != null)
            animator.SetTrigger("Attack");

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

            case BodyHitboxSlot.Legs:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyLegDamage(true, slotDamage, hadChitin);
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
        
        OnChitinHpChanged?.Invoke();
    }

    private void ApplyBodyDamage(float amount, bool hadChitin)
    {
        if (amount <= 0f)
            return;

        float mult = 1f + Stats.bodyDamageTakenMult;
        float reduced = amount * mult;
        CurrentBodyHp = Mathf.Max(0f, CurrentBodyHp - reduced);

        OnBodyHpChanged?.Invoke();
    }

    private void ApplyJawsDamage(float amount, bool hadChitin)
    {
        if (amount <= 0f)
            return;

        float mult = 1f + Stats.limbDamageTakenMult;
        CurrentJawsHp = Mathf.Max(0f, CurrentJawsHp - amount * mult);
        if (CurrentJawsHp <= 0f)
        {
            CurrentJawsHp = 0f;
            DisablePart(BodyPartType.Jaws);
        }

        OnJawsHpChanged?.Invoke();
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

        // if either leg is lost, consider all legs lost
        if (CurrentLeftLegHp <= 0f || CurrentRightLegHp <= 0f)
        {
            CurrentLeftLegHp = Mathf.Max(0f, CurrentLeftLegHp);
            CurrentRightLegHp = Mathf.Max(0f, CurrentRightLegHp);
            DisablePart(BodyPartType.Legs);
        }
        
        OnLegsHpChanged?.Invoke();
    }

    public void TakeReflectedDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        CurrentBodyHp = Mathf.Max(0f, CurrentBodyHp - amount);

        OnBodyHpChanged?.Invoke();
        CheckDeath();
    }

    public void HealBody(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentBodyHp = Mathf.Min(Stats.maxBodyHp, CurrentBodyHp + amount);
        
        OnBodyHpChanged?.Invoke();
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
        CurrentStamina = Mathf.Min(Stats.maxStamina, CurrentStamina + biomass * 10f);

        // If eating restored a part from zero, re-enable it
        if (CurrentJawsHp > 0f && jawsDisabled)
            EnablePart(BodyPartType.Jaws);

        if ((CurrentLeftLegHp > 0f && CurrentRightLegHp > 0f) && legsDisabled)
            EnablePart(BodyPartType.Legs);

        if (CurrentChitinHp > 0f && chitinDisabled)
            EnablePart(BodyPartType.Chitin);
        
        OnChitinHpChanged?.Invoke();
        OnBodyHpChanged?.Invoke();
        OnJawsHpChanged?.Invoke();
        OnLegsHpChanged?.Invoke();
        OnStaminaChanged?.Invoke();
    }

    private void DisablePart(BodyPartType part)
    {
        switch (part)
        {
            case BodyPartType.Jaws:
                jawsDisabled = true;
                if (graphicsJaws != null)
                    graphicsJaws.gameObject.SetActive(false);
                break;
            case BodyPartType.Legs:
                legsDisabled = true;
                if (graphicsLegs != null)
                    graphicsLegs.gameObject.SetActive(false);
                break;
            case BodyPartType.Chitin:
                chitinDisabled = true;
                if (graphicsChitin != null)
                    graphicsChitin.gameObject.SetActive(false);
                break;
        }
    }

    private void EnablePart(BodyPartType part)
    {
        switch (part)
        {
            case BodyPartType.Jaws:
                jawsDisabled = false;
                if (graphicsJaws != null)
                    graphicsJaws.gameObject.SetActive(true);
                lastRegrowJawsTime = Time.time;
                break;
            case BodyPartType.Legs:
                legsDisabled = false;
                if (graphicsLegs != null)
                    graphicsLegs.gameObject.SetActive(true);
                lastRegrowLegsTime = Time.time;
                break;
            case BodyPartType.Chitin:
                chitinDisabled = false;
                if (graphicsChitin != null)
                    graphicsChitin.gameObject.SetActive(true);
                lastRegrowChitinTime = Time.time;
                break;
        }
    }

    public bool TryRegrowPart(BodyPartType part)
    {
        float percent = 0f;
        float maxHp = 0f;
        float cooldown = 0f;
        float last = 0f;

        switch (part)
        {
            case BodyPartType.Jaws:
                percent = Stats.jawsRegrowPercent;
                maxHp = Stats.maxJawHp;
                cooldown = Stats.jawsRegrowCooldown;
                last = lastRegrowJawsTime;
                break;
            case BodyPartType.Legs:
                percent = Stats.legsRegrowPercent;
                maxHp = Stats.maxLegHp;
                cooldown = Stats.legsRegrowCooldown;
                last = lastRegrowLegsTime;
                break;
            case BodyPartType.Chitin:
                percent = Stats.chitinRegrowPercent;
                maxHp = Stats.maxChitinHp;
                cooldown = Stats.chitinRegrowCooldown;
                last = lastRegrowChitinTime;
                break;
            default:
                return false;
        }

        if (percent <= 0f)
            return false;

        if (Time.time - last < cooldown)
            return false;

        // restore to at least percent of max HP
        if (part == BodyPartType.Jaws)
        {
            CurrentJawsHp = Mathf.Max(CurrentJawsHp, maxHp * percent);
            OnJawsHpChanged?.Invoke();
        }
        else if (part == BodyPartType.Legs)
        {
            CurrentLeftLegHp = Mathf.Max(CurrentLeftLegHp, maxHp * percent);
            CurrentRightLegHp = Mathf.Max(CurrentRightLegHp, maxHp * percent);
            OnLegsHpChanged?.Invoke();
        }
        else if (part == BodyPartType.Chitin)
        {
            CurrentChitinHp = Mathf.Max(CurrentChitinHp, maxHp * percent);
            OnChitinHpChanged?.Invoke();
        }

        EnablePart(part);

        if (part == BodyPartType.Jaws) lastRegrowJawsTime = Time.time;
        if (part == BodyPartType.Legs) lastRegrowLegsTime = Time.time;
        if (part == BodyPartType.Chitin) lastRegrowChitinTime = Time.time;

        return true;
    }

    /// <summary>
    /// Checks if a body part is disabled and can potentially regrow.
    /// </summary>
    public bool CanRegrowPart(BodyPartType part)
    {
        if (IsDead) return false;

        switch (part)
        {
            case BodyPartType.Jaws:
                return jawsDisabled && Stats.jawsRegrowPercent > 0f;
            case BodyPartType.Legs:
                return legsDisabled && Stats.legsRegrowPercent > 0f;
            case BodyPartType.Chitin:
                return chitinDisabled && Stats.chitinRegrowPercent > 0f;
            default:
                return false;
        }
    }

    /// <summary>
    /// Returns the remaining cooldown time for a regrow ability in seconds.
    /// Returns 0 if the ability is ready or not available.
    /// </summary>
    public float GetRegrowCooldownRemaining(BodyPartType part)
    {
        if (IsDead) return float.MaxValue;

        float cooldown = 0f;
        float last = 0f;

        switch (part)
        {
            case BodyPartType.Jaws:
                if (Stats.jawsRegrowPercent <= 0f) return float.MaxValue;
                cooldown = Stats.jawsRegrowCooldown;
                last = lastRegrowJawsTime;
                break;
            case BodyPartType.Legs:
                if (Stats.legsRegrowPercent <= 0f) return float.MaxValue;
                cooldown = Stats.legsRegrowCooldown;
                last = lastRegrowLegsTime;
                break;
            case BodyPartType.Chitin:
                if (Stats.chitinRegrowPercent <= 0f) return float.MaxValue;
                cooldown = Stats.chitinRegrowCooldown;
                last = lastRegrowChitinTime;
                break;
            default:
                return float.MaxValue;
        }

        float remaining = cooldown - (Time.time - last);
        return Mathf.Max(0f, remaining);
    }

    private void CheckDeath()
    {
        if (IsDead)
            return;

        if (CurrentBodyHp <= 0f)
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
        float sum = Mathf.Pow(10, enemyEvolutionStage - 1) + enemyLevel;
        return Mathf.Max(1f, sum * corpseBiomassMultiplier);
    }

    public bool IsFriendlyTo(OrganismCombatant other)
    {
        if (other == null)
            return false;

        return other.factionGroupId == factionGroupId;
    }
}