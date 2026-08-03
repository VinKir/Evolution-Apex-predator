using UnityEngine;

public enum AIStateType
{
    Dead,
    Flee,
    Fight,
    Eat,
    Hunt,
    Rest,
    Patrol,
    ReturnHome,
    Wander,
    Idle
}

public abstract class AIState
{
    protected readonly EnemyAIController Owner;
    protected readonly AIContext Context;
    protected readonly AIMemory Memory;

    protected AIState(EnemyAIController owner, AIContext context, AIMemory memory)
    {
        Owner = owner;
        Context = context;
        Memory = memory;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
    public virtual bool CanExit() => true;

    protected void SetMovementDirection(Vector2 direction)
    {
        if (Owner != null && Owner.Movement != null)
            Owner.Movement.SetDesiredDirection(direction);
    }

    protected void StopMovement()
    {
        if (Owner != null && Owner.Movement != null)
            Owner.Movement.Stop();
    }
}
