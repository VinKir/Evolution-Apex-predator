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
    public float bleedDurationSeconds;
    public float lifestealPercent;
    public float chitinDamageMultiplierDealt;
    public float internalDamageMultiplierDealt;
    public float bodyDamageMultiplierDealt;
    public float limbDamageMultiplierDealt;
    public float legsDamageMultiplierDealt;
    public float jawsDamageMultiplierDealt;
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

    public float chitinRegenPerSec;
    public float jawsRegenPerSec;
    public float legsRegenPerSec;
    public float bodyRegenPerSec;

    public float attackVsHealthyMult;
    public float attackVsLowMult;

    public float bleedPercent;
    public float bleedDurationSeconds;
    public float lifestealPercent;
    public float chitinReflectPercent;

    public float jawsRegrowPercent;
    public float legsRegrowPercent;
    public float chitinRegrowPercent;
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

    private Transform graphicsChitin;
    private Transform graphicsJaws;
    private Transform graphicsLegs;

    private bool chitinDisabled = false;
    private bool jawsDisabled = false;
    private bool legsDisabled = false;
    public bool ChitinDisabled => chitinDisabled;
    public bool JawsDisabled => jawsDisabled;
    public bool LegsDisabled => legsDisabled;

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

        Stats = BuildStats();

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

    private OrganismRuntimeStats BuildStats()
    {
        int level = isPlayer && playerProgression != null ? playerProgression.Level : enemyLevel;
        int evo = isPlayer && playerProgression != null ? playerProgression.EvolutionStage : enemyEvolutionStage;
        level = Mathf.Max(1, level);
        evo = Mathf.Max(1, evo);

        float levelFactor = 1f + 0.03f * Mathf.Max(0, level - 1);
        float evoFactor = 1f + 0.12f * Mathf.Max(0, evo - 1);

        CombatBonusAccumulator bonus = AggregateBonuses();

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

        // TODO: сделать зависимость цены на движение и атаку от какого-то параметра, чтоб из-за высокой силы какой-то стоимость атаки и движения увеличивалась, а от чего-то другого уменьшалась
        s.staminaMoveCost = Mathf.Max(0.01f, 5f * (1f - bonus.staminaMoveCostReduction));
        s.staminaAttackCost = Mathf.Max(0.01f, 10f * (1f - bonus.staminaAttackCostReduction));

        s.moveSpeed = 3.5f
                      * (1f + bonus.moveSpeedMult)
                      * Mathf.Clamp(1f - s.strengthExt * 0.05f + s.strengthInt * 0.015f, 0.35f, 3f);

        s.turnSpeed = 6f * (1f + bonus.turnSpeedMult) * Mathf.Clamp(1f - s.strengthExt * 0.03f, 0.4f, 2f);

        s.sizeMultiplier = Mathf.Clamp(
            1f + s.strengthExt * 0.08f - s.strengthInt * 0.03f + bonus.sizeMult,
            0.55f,
            3f
        );

        s.detectionRadius = 4.5f * s.sizeMultiplier * (1f - bonus.detectRadiusReduction);

        s.chitinDamageMultiplierDealt = bonus.chitinDamageMultiplierDealt;
        s.chitinDamageMultiplierTaken = bonus.chitinDamageMultiplierTaken;
        s.internalDamageMultiplierDealt = bonus.internalDamageMultiplierDealt;
        s.internalDamageMultiplierTaken = bonus.internalDamageMultiplierTaken;
        s.bodyDamageMultiplierDealt = bonus.bodyDamageMultiplierDealt;
        s.bodyDamageMultiplierTaken = bonus.bodyDamageMultiplierTaken;
        s.limbDamageMultiplierDealt = bonus.limbDamageMultiplierDealt;
        s.limbDamageMultiplierTaken = bonus.limbDamageMultiplierTaken;
        s.legsDamageMultiplierDealt = bonus.legsDamageMultiplierDealt;
        s.legsDamageMultiplierTaken = bonus.legsDamageMultiplierTaken;
        s.jawsDamageMultiplierDealt = bonus.jawsDamageMultiplierDealt;
        s.jawsDamageMultiplierTaken = bonus.jawsDamageMultiplierTaken;
        s.chitinReflectPercent = bonus.chitinReflectPercent;

        s.chitinRegenPerSec = bonus.chitinRegenPerSec;
        s.jawsRegenPerSec = bonus.jawsRegenPerSec;
        s.legsRegenPerSec = bonus.legsRegenPerSec;
        s.bodyRegenPerSec = bonus.bodyRegenPerSec;

        s.attackVsHealthyMult = 1f + Mathf.Max(0f, bonus.attackVsHealthyMult);
        s.attackVsLowMult = 1f + Mathf.Max(0f, bonus.attackVsLowMult);
        s.bleedPercent = bonus.bleedPercent;
        s.lifestealPercent = bonus.lifestealPercent;
        s.bleedDurationSeconds = bonus.bleedDurationSeconds;
        s.jawsRegrowPercent = bonus.jawsRegrowPercent;
        s.legsRegrowPercent = bonus.legsRegrowPercent;
        s.chitinRegrowPercent = bonus.chitinRegrowPercent;

        s.jawsRegrowCooldown = Mathf.Max(5f, CombatSettings.BaseRegrowCooldown - bonus.jawsRegrowCooldownReduction);
        s.legsRegrowCooldown = Mathf.Max(5f, CombatSettings.BaseRegrowCooldown - bonus.legsRegrowCooldownReduction);
        s.chitinRegrowCooldown = Mathf.Max(5f, CombatSettings.BaseRegrowCooldown - bonus.chitinRegrowCooldownReduction);

        return s;
    }

    private CombatBonusAccumulator AggregateBonuses()
    {
        CombatBonusAccumulator bonuses = default;
        int evolutionStage = isPlayer && playerProgression != null ? playerProgression.EvolutionStage : enemyEvolutionStage;

        if (isPlayer && playerBody != null)
        {
            foreach (var state in playerBody.States)
            {
                if (state == null)
                    continue;

                foreach (var applied in state.appliedVariants)
                {
                    if (applied?.variant == null)
                        continue;

                    foreach (var modifier in applied.variant.modifiers)
                        AddModifier(ref bonuses, modifier, state.level, evolutionStage);
                }
            }
        }
        else if (!isPlayer && enemyTemplate != null)
        {
            foreach (var state in enemyTemplate.bodyParts)
            {
                if (state == null)
                    continue;

                foreach (var applied in state.appliedVariants)
                {
                    if (applied?.variant == null)
                        continue;

                    foreach (var modifier in applied.variant.modifiers)
                        AddModifier(ref bonuses, modifier, state.level, evolutionStage);
                }
            }
        }

        return bonuses;
    }

    private void AddModifier(ref CombatBonusAccumulator bonuses, BodyStatModifier modifier, int level, int evolutionStage)
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
            case BodyStatType.ChitinRegenPerSec: bonuses.chitinRegenPerSec += value; break;
            case BodyStatType.JawsRegenPerSec: bonuses.jawsRegenPerSec += value; break;
            case BodyStatType.LegsRegenPerSec: bonuses.legsRegenPerSec += value; break;
            case BodyStatType.BodyRegenPerSec: bonuses.bodyRegenPerSec += value; break;
            case BodyStatType.AttackVsHealthyMult: bonuses.attackVsHealthyMult += value; break;
            case BodyStatType.AttackVsLowMult: bonuses.attackVsLowMult += value; break;
            case BodyStatType.JawsRegrowPercent: bonuses.jawsRegrowPercent = Mathf.Max(bonuses.jawsRegrowPercent, value); break;
            case BodyStatType.LegsRegrowPercent: bonuses.legsRegrowPercent = Mathf.Max(bonuses.legsRegrowPercent, value); break;
            case BodyStatType.ChitinRegrowPercent: bonuses.chitinRegrowPercent = Mathf.Max(bonuses.chitinRegrowPercent, value); break;
            case BodyStatType.JawsRegrowCooldownReduction: bonuses.jawsRegrowCooldownReduction += value; break;
            case BodyStatType.LegsRegrowCooldownReduction: bonuses.legsRegrowCooldownReduction += value; break;
            case BodyStatType.ChitinRegrowCooldownReduction: bonuses.chitinRegrowCooldownReduction += value; break;
        }
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
        return new AttackPacket
        {
            rawDamage = Stats.attackDamage,
            staminaCost = Stats.staminaAttackCost,
            bleedPercent = Stats.bleedPercent,
            bleedDurationSeconds = Stats.bleedDurationSeconds,
            lifestealPercent = Stats.lifestealPercent,
            chitinDamageMultiplierDealt = Stats.chitinDamageMultiplierDealt,
            internalDamageMultiplierDealt = Stats.internalDamageMultiplierDealt,
            bodyDamageMultiplierDealt = Stats.bodyDamageMultiplierDealt,
            limbDamageMultiplierDealt = Stats.limbDamageMultiplierDealt,
            legsDamageMultiplierDealt = Stats.legsDamageMultiplierDealt,
            jawsDamageMultiplierDealt = Stats.jawsDamageMultiplierDealt,
            attackVsHealthyMult = Stats.attackVsHealthyMult,
            attackVsLowMult = Stats.attackVsLowMult
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
        TakeAttackInternal(attacker, packet, hitbox != null ? hitbox.slot : BodyHitboxSlot.Body, true, false);
    }

    private void TakeAttackInternal(OrganismCombatant attacker, AttackPacket packet, BodyHitboxSlot slot, bool allowChitinReflect, bool plainDamage)
    {
        if (IsDead || (attacker != null && attacker != this && attacker.FactionGroupId == factionGroupId))
            return;

        OnDamagedBy?.Invoke(attacker);

        float damage = packet.rawDamage;
        if (CurrentBodyHpNormalized > 0.9f)
            damage *= packet.attackVsHealthyMult;
        else if (CurrentBodyHpNormalized < 0.3f)
            damage *= packet.attackVsLowMult;

        bool hadChitin = CurrentChitinHp > 0f;
        bool isLimb = slot == BodyHitboxSlot.Jaws || slot == BodyHitboxSlot.Legs;
        float chitinShare = CombatSettings.BaseChitinDamageMultiplier
            - Stats.chitinDamageMultiplierTaken
            + (plainDamage ? 0f : packet.chitinDamageMultiplierDealt);
        float internalShare = hadChitin ? CombatSettings.BaseInternalDamageMultiplier : 1f;
        internalShare += -Stats.internalDamageMultiplierTaken
            + (plainDamage ? 0f : packet.internalDamageMultiplierDealt);

        if (slot == BodyHitboxSlot.Body)
            internalShare += -Stats.bodyDamageMultiplierTaken
                + (plainDamage ? 0f : packet.bodyDamageMultiplierDealt);
        else if (isLimb)
        {
            internalShare += -Stats.limbDamageMultiplierTaken
                + (plainDamage ? 0f : packet.limbDamageMultiplierDealt);
            if (slot == BodyHitboxSlot.Jaws)
                internalShare += -Stats.jawsDamageMultiplierTaken
                    + (plainDamage ? 0f : packet.jawsDamageMultiplierDealt);
            else
                internalShare += -Stats.legsDamageMultiplierTaken
                    + (plainDamage ? 0f : packet.legsDamageMultiplierDealt);
        }

        float chitinDamage = hadChitin ? damage * Mathf.Max(0f, chitinShare) : 0f;
        float directDamage = damage * Mathf.Max(0f, internalShare);

        switch (slot)
        {
            case BodyHitboxSlot.Chitin:
                ApplyChitinDamage(chitinDamage > 0f ? chitinDamage : damage, attacker);
                break;
            case BodyHitboxSlot.Jaws:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyJawsDamage(directDamage, hadChitin);
                break;
            case BodyHitboxSlot.Legs:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyLegDamage(true, directDamage, hadChitin);
                break;
            default:
                ApplyChitinDamage(chitinDamage, attacker);
                ApplyBodyDamage(directDamage, hadChitin);
                break;
        }

        if (slot != BodyHitboxSlot.Chitin && packet.bleedPercent > 0f && packet.bleedDurationSeconds > 0f)
            StartCoroutine(BleedRoutine(slot, directDamage * packet.bleedPercent, packet.bleedDurationSeconds));

        if (attacker != null && packet.lifestealPercent > 0f)
            attacker.HealMostDamagedPart(directDamage * packet.lifestealPercent);

        if (allowChitinReflect && hadChitin && Stats.chitinReflectPercent > 0f && attacker != null)
        {
            AttackPacket spikePacket = new AttackPacket
            {
                rawDamage = damage * Stats.chitinReflectPercent,
                attackVsHealthyMult = 1f,
                attackVsLowMult = 1f
            };
            attacker.TakeAttackInternal(this, spikePacket, BodyHitboxSlot.Jaws, false, true);
        }

        CheckDeath();
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

        CurrentBodyHp = Mathf.Max(0f, CurrentBodyHp - amount);

        OnBodyHpChanged?.Invoke();
    }

    private IEnumerator BleedRoutine(BodyHitboxSlot slot, float damage, float duration)
    {
        const float tickInterval = CombatSettings.BleedingTickInterval;
        float elapsed = 0f;

        while (elapsed < duration && !IsDead)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            switch (slot)
            {
                case BodyHitboxSlot.Jaws:
                    ApplyJawsDamage(damage, false);
                    break;
                case BodyHitboxSlot.Legs:
                    ApplyLegDamage(true, damage, false);
                    break;
                default:
                    ApplyBodyDamage(damage, false);
                    break;
            }

            CheckDeath();
        }
    }

    private void ApplyJawsDamage(float amount, bool hadChitin)
    {
        if (amount <= 0f)
            return;

        CurrentJawsHp = Mathf.Max(0f, CurrentJawsHp - amount);
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

        if (left)
            CurrentLeftLegHp = Mathf.Max(0f, CurrentLeftLegHp - amount);
        else
            CurrentRightLegHp = Mathf.Max(0f, CurrentRightLegHp - amount);

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

    private void HealMostDamagedPart(float amount)
    {
        if (amount <= 0f)
            return;

        float bodyMissing = Stats.maxBodyHp - CurrentBodyHp;
        float chitinMissing = Stats.maxChitinHp - CurrentChitinHp;
        float jawsMissing = Stats.maxJawHp - CurrentJawsHp;
        float legsMissing = Stats.maxLegHp - Mathf.Min(CurrentLeftLegHp, CurrentRightLegHp);

        if (bodyMissing >= chitinMissing && bodyMissing >= jawsMissing && bodyMissing >= legsMissing)
        {
            HealBody(amount);
        }
        else if (chitinMissing >= jawsMissing && chitinMissing >= legsMissing)
        {
            CurrentChitinHp = Mathf.Min(Stats.maxChitinHp, CurrentChitinHp + amount);
            OnChitinHpChanged?.Invoke();
        }
        else if (jawsMissing >= legsMissing)
        {
            CurrentJawsHp = Mathf.Min(Stats.maxJawHp, CurrentJawsHp + amount);
            if (CurrentJawsHp > 0f && jawsDisabled)
                EnablePart(BodyPartType.Jaws);
            OnJawsHpChanged?.Invoke();
        }
        else
        {
            CurrentLeftLegHp = Mathf.Min(Stats.maxLegHp, CurrentLeftLegHp + amount);
            CurrentRightLegHp = Mathf.Min(Stats.maxLegHp, CurrentRightLegHp + amount);
            if (CurrentLeftLegHp > 0f && CurrentRightLegHp > 0f && legsDisabled)
                EnablePart(BodyPartType.Legs);
            OnLegsHpChanged?.Invoke();
        }
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