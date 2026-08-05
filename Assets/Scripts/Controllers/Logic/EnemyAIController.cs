using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private OrganismCombatant combatant;
    [SerializeField] private OrganismMovementMotor movement;
    [SerializeField] private OrganismBehaviourType behaviourType = OrganismBehaviourType.Predator;

    [Header("Brain")]
    [SerializeField] private float thinkInterval = 0.25f;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float corpseEatRange = 1.0f;

    [Header("Movement")]
    [SerializeField] private float wanderChangeInterval = 2.5f;
    [SerializeField] private float targetReachThreshold = 0.15f;

    public bool IsRoamingEnemy { get; private set; }
    public EnemySpawnPoint BasePoint { get; private set; }
    public OrganismCombatant Combatant => combatant;
    public OrganismMovementMotor Movement => movement;
    public OrganismBehaviourType BehaviourType => behaviourType;
    public float AttackRange => attackRange;
    public float EatRange => corpseEatRange;
    public float WanderChangeInterval => wanderChangeInterval;
    public float TargetReachThreshold => targetReachThreshold;

    private Transform player;
    private float nextThinkTime;
    private AIContext context;
    private AIMemory memory;
    private OrganismBrain brain;
    private AIStateMachine stateMachine;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrganismMovementMotor>();

        if (combatant == null)
            combatant = GetComponent<OrganismCombatant>();

        context = new AIContext();
        memory = new AIMemory();
        brain = new OrganismBrain();
        stateMachine = new AIStateMachine(this, context, memory);
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
        memory.LastAttacker = attacker;
        memory.LastThreat = attacker;
        memory.TimeSinceLastDamage = 0f;
    }

    private void Update()
    {
        if (combatant == null || combatant.IsDead)
            return;

        if (player == null)
            return;

        memory.Update(Time.deltaTime);

        if (Time.time >= nextThinkTime)
        {
            Think();
            nextThinkTime = Time.time + thinkInterval;
        }

        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        if (movement == null || combatant == null || combatant.IsDead)
            return;

        stateMachine.FixedUpdate();
    }

    private void Think()
    {
        context.UpdateFrom(this, memory);

        if (memory.LastAttacker != null && !memory.LastAttacker.IsDead)
            memory.LastThreat = memory.LastAttacker;
        else
            memory.LastThreat = context.CurrentThreat;

        var nextState = brain.DecideState(context, memory);
        stateMachine.ChangeState(nextState);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out BodyHitbox hitbox))
        {
            var attacker = hitbox.GetComponentInParent<OrganismCombatant>();
            if (attacker != null && !combatant.IsFriendlyTo(attacker))
            {
                memory.LastAttacker = attacker;
                memory.LastThreat = attacker;
                memory.TimeSinceLastDamage = 0f;
            }
        }
    }
}