using UnityEngine;

public class IdleState : AIState
{
    public IdleState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Enter()
    {
        StopMovement();
    }
}

public class WanderState : AIState
{
    private Vector2 wanderTarget;
    private float nextWanderChangeTime;

    public WanderState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Enter()
    {
        nextWanderChangeTime = 0f;
        UpdateWanderTarget();
    }

    public override void Update()
    {
        if (Time.time >= nextWanderChangeTime)
        {
            UpdateWanderTarget();
        }

        if (wanderTarget != Vector2.zero)
        {
            Vector2 dir = wanderTarget - (Vector2)Owner.transform.position;
            if (dir.magnitude <= Owner.TargetReachThreshold)
            {
                wanderTarget = Vector2.zero;
                StopMovement();
                return;
            }

            SetMovementDirection(dir.normalized);
        }
        else
        {
            StopMovement();
        }
    }

    private void UpdateWanderTarget()
    {
        wanderTarget = (Vector2)Owner.transform.position + Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
        nextWanderChangeTime = Time.time + Owner.WanderChangeInterval;
    }
}

public class FleeState : AIState
{
    public FleeState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Update()
    {
        if (Context.CurrentThreat == null || Context.CurrentThreat.IsDead)
        {
            StopMovement();
            return;
        }

        Vector2 dir = ((Vector2)Owner.transform.position - (Vector2)Context.CurrentThreat.transform.position).normalized;
        SetMovementDirection(dir);
    }
}

public class FightState : AIState
{
    public FightState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Update()
    {
        if (Context.CurrentTarget == null || Context.CurrentTarget.IsDead)
        {
            StopMovement();
            return;
        }

        float dist = Vector2.Distance(Owner.transform.position, Context.CurrentTarget.transform.position);
        if (dist <= Owner.AttackRange)
        {
            Owner.Combatant.TryStartMeleeAttack();
            StopMovement();
            return;
        }

        Vector2 dir = ((Vector2)Context.CurrentTarget.transform.position - (Vector2)Owner.transform.position).normalized;
        SetMovementDirection(dir);
    }
}

public class EatState : AIState
{
    public EatState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Update()
    {
        if (Context.NearestFood == null)
            return;

        float dist = Vector2.Distance(Owner.transform.position, Context.NearestFood.transform.position);
        if (dist <= Owner.EatRange)
        {
            float gained = Context.NearestFood.AddProgress(Time.deltaTime / Mathf.Max(0.01f, Context.NearestFood.ConsumeDuration));
            if (gained > 0f)
                Owner.Combatant.ApplyFoodGain(gained);

            if (Context.NearestFood.IsFullyEaten)
                StopMovement();
            return;
        }

        Vector2 dir = ((Vector2)Context.NearestFood.transform.position - (Vector2)Owner.transform.position).normalized;
        SetMovementDirection(dir);
    }
}

public class HuntState : AIState
{
    public HuntState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Update()
    {
        if (Context.CurrentTarget == null || Context.CurrentTarget.IsDead)
        {
            StopMovement();
            return;
        }

        Vector2 dir = ((Vector2)Context.CurrentTarget.transform.position - (Vector2)Owner.transform.position).normalized;
        SetMovementDirection(dir);
    }
}

public class RestState : AIState
{
    public RestState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Enter()
    {
        StopMovement();
    }
}

public class PatrolState : AIState
{
    private Vector2 patrolPoint;

    public PatrolState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Enter()
    {
        patrolPoint = Context.HomePoint != null ? Context.HomePoint.transform.position : Owner.transform.position;
    }

    public override void Update()
    {
        if (Context.HomePoint == null)
            return;

        Vector2 dir = patrolPoint - (Vector2)Owner.transform.position;
        if (dir.magnitude <= Owner.TargetReachThreshold)
        {
            patrolPoint = (Vector2)Context.HomePoint.transform.position + Random.insideUnitCircle.normalized * 1.5f;
            return;
        }

        SetMovementDirection(dir.normalized);
    }
}

public class ReturnHomeState : AIState
{
    public ReturnHomeState(EnemyAIController owner, AIContext context, AIMemory memory) : base(owner, context, memory) { }

    public override void Update()
    {
        if (Context.HomePoint == null)
            return;

        Vector2 dir = ((Vector2)Context.HomePoint.transform.position - (Vector2)Owner.transform.position).normalized;
        SetMovementDirection(dir);
    }
}
