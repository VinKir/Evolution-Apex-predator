using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private OrganismCombatant combatant;
    [SerializeField] private Rigidbody2D rb;

    [Header("Brain")]
    [SerializeField] private float thinkInterval = 0.25f;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float fleeHealthThreshold = 0.25f;
    [SerializeField] private float corpseEatRange = 1.0f;

    [Header("Movement")]
    [SerializeField] private float wanderChangeInterval = 2.5f;
    [SerializeField] private float targetReachThreshold = 0.15f;
    [SerializeField] private float baseCrawlSpeed = 0.6f;

    public bool IsRoamingEnemy { get; private set; }
    public EnemySpawnPoint BasePoint { get; private set; }

    private Transform player;
    private float nextThinkTime;
    private float nextWanderChangeTime;
    private Vector2 wanderTarget;
    private OrganismCombatant lastThreat;
    private FoodItem targetCorpse;
    private bool isFleeing;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (combatant == null)
            combatant = GetComponent<OrganismCombatant>();
    }

    public void Initialize(Transform playerTarget, EnemySpawnPoint basePoint)
    {
        player = playerTarget;
        BasePoint = basePoint;
        IsRoamingEnemy = basePoint == null;

        if (combatant != null)
            combatant.OnDamagedBy += OnDamagedBy;
    }

    private void OnDestroy()
    {
        if (combatant != null)
            combatant.OnDamagedBy -= OnDamagedBy;
    }

    private void OnDamagedBy(OrganismCombatant attacker)
    {
        lastThreat = attacker;
        isFleeing = true;
        targetCorpse = null;
    }

    private void Update()
    {
        if (combatant == null || combatant.IsDead)
            return;

        if (player == null)
            return;

        if (Time.time >= nextThinkTime)
        {
            Think();
            nextThinkTime = Time.time + thinkInterval;
        }

        if (targetCorpse != null)
        {
            float dist = Vector2.Distance(transform.position, targetCorpse.transform.position);
            if (dist <= corpseEatRange)
            {
                float gained = targetCorpse.AddProgress(Time.deltaTime / Mathf.Max(0.01f, targetCorpse.ConsumeDuration));
                if (gained > 0f)
                    combatant.ApplyFoodGain(gained);

                if (targetCorpse.IsFullyEaten)
                    targetCorpse = null;

                rb.linearVelocity = Vector2.zero;
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || combatant == null || combatant.IsDead)
            return;

        Vector2 move = Vector2.zero;

        if (isFleeing && lastThreat != null && !lastThreat.IsDead)
        {
            move = ((Vector2)transform.position - (Vector2)lastThreat.transform.position).normalized;
        }
        else if (targetCorpse != null)
        {
            move = ((Vector2)targetCorpse.transform.position - (Vector2)transform.position).normalized;
        }
        else
        {
            move = DecideMove();
        }

        float speed = combatant.Stats.moveSpeed;
        if (combatant.CurrentStamina <= 0.1f)
            speed *= baseCrawlSpeed;

        rb.linearVelocity = move.normalized * speed;

        if (move.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
            rb.rotation = angle - 90f;
        }
    }

    private void Think()
    {
        var nearbyOrganisms = Physics2D.OverlapCircleAll(transform.position, combatant.Stats.detectionRadius)
            .Select(c => c.GetComponentInParent<OrganismCombatant>())
            .Where(o => o != null && o != combatant)
            .ToList();

        var nearbyFood = Physics2D.OverlapCircleAll(transform.position, combatant.Stats.detectionRadius)
            .Select(c => c.GetComponent<FoodItem>())
            .Where(f => f != null && !f.IsFullyEaten)
            .ToList();

        float healthRatio = combatant.CurrentBodyHpNormalized;

        if (combatant != null && healthRatio <= fleeHealthThreshold && lastThreat != null && !lastThreat.IsDead)
        {
            isFleeing = true;
            return;
        }

        switch (combatant != null && combatant.FactionGroupId >= 0 ? combatant.FactionGroupId : 0)
        {
            default:
                break;
        }

        if (BasePoint != null)
        {
            HandleGuardianBrain(nearbyOrganisms);
            return;
        }

        if (combatant != null && combatant.name.ToLower().Contains("scav"))
        {
            HandleScavengerBrain(nearbyFood, nearbyOrganisms);
            return;
        }

        HandlePredatorBrain(nearbyOrganisms, nearbyFood);
    }

    private void HandleScavengerBrain(System.Collections.Generic.List<FoodItem> nearbyFood, System.Collections.Generic.List<OrganismCombatant> nearbyOrganisms)
    {
        if (lastThreat != null && !lastThreat.IsDead)
        {
            isFleeing = true;
            return;
        }

        targetCorpse = nearbyFood.OrderBy(f => Vector2.Distance(transform.position, f.transform.position)).FirstOrDefault();
        if (targetCorpse != null)
            isFleeing = false;
    }

    private void HandlePredatorBrain(System.Collections.Generic.List<OrganismCombatant> nearbyOrganisms, System.Collections.Generic.List<FoodItem> nearbyFood)
    {
        if (lastThreat != null && !lastThreat.IsDead && lastThreat.CombatPower > combatant.CombatPower * 1.1f && combatant.CurrentBodyHpNormalized < 0.35f)
        {
            isFleeing = true;
            return;
        }

        isFleeing = false;
        targetCorpse = null;

        var prey = nearbyOrganisms
            .Where(o => !combatant.IsFriendlyTo(o) && o.CombatPower < combatant.CombatPower * 1.1f)
            .OrderBy(o => Vector2.Distance(transform.position, o.transform.position))
            .FirstOrDefault();

        if (prey != null)
        {
            float dist = Vector2.Distance(transform.position, prey.transform.position);
            if (dist <= attackRange)
                combatant.TryStartMeleeAttack();

            wanderTarget = prey.transform.position;
            return;
        }

        var corpse = nearbyFood.OrderBy(f => Vector2.Distance(transform.position, f.transform.position)).FirstOrDefault();
        if (corpse != null && combatant.CurrentBodyHpNormalized < 0.7f)
        {
            targetCorpse = corpse;
            return;
        }

        if (Time.time >= nextWanderChangeTime)
        {
            wanderTarget = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            nextWanderChangeTime = Time.time + wanderChangeInterval;
        }
    }

    private void HandleGuardianBrain(System.Collections.Generic.List<OrganismCombatant> nearbyOrganisms)
    {
        isFleeing = false;
        targetCorpse = null;

        if (BasePoint == null)
            return;

        float baseDistance = Vector2.Distance(transform.position, BasePoint.transform.position);

        var hostile = nearbyOrganisms
            .Where(o => !combatant.IsFriendlyTo(o))
            .OrderBy(o => Vector2.Distance(transform.position, o.transform.position))
            .FirstOrDefault();

        if (hostile != null)
        {
            float dist = Vector2.Distance(transform.position, hostile.transform.position);
            if (dist <= attackRange)
                combatant.TryStartMeleeAttack();

            wanderTarget = hostile.transform.position;
            return;
        }

        if (baseDistance > BasePoint.activationRadius * 0.8f)
            wanderTarget = BasePoint.transform.position;
    }

    private Vector2 DecideMove()
    {
        if (BasePoint != null)
        {
            float dist = Vector2.Distance(transform.position, BasePoint.transform.position);
            if (dist > BasePoint.activationRadius)
                return ((Vector2)BasePoint.transform.position - (Vector2)transform.position).normalized;
        }

        if (targetCorpse != null)
        {
            Vector2 dir = (Vector2)targetCorpse.transform.position - (Vector2)transform.position;
            if (dir.magnitude <= targetReachThreshold)
                return Vector2.zero;
            return dir.normalized;
        }

        if (wanderTarget != Vector2.zero)
        {
            Vector2 dir = wanderTarget - (Vector2)transform.position;
            if (dir.magnitude <= targetReachThreshold)
            {
                wanderTarget = Vector2.zero;
                return Vector2.zero;
            }
            return dir.normalized;
        }

        return Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out BodyHitbox hitbox))
        {
            var attacker = hitbox.GetComponentInParent<OrganismCombatant>();
            if (attacker != null && !combatant.IsFriendlyTo(attacker))
                lastThreat = attacker;
        }
    }
}