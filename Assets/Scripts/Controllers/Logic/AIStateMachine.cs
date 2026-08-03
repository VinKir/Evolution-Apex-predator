public class AIStateMachine
{
    private readonly EnemyAIController owner;
    private readonly AIContext context;
    private readonly AIMemory memory;

    private AIState currentState;

    public AIStateMachine(EnemyAIController owner, AIContext context, AIMemory memory)
    {
        this.owner = owner;
        this.context = context;
        this.memory = memory;
    }

    public AIStateType CurrentStateType { get; private set; } = AIStateType.Idle;
    public AIState CurrentState => currentState;

    public void ChangeState(AIStateType nextType)
    {
        if (CurrentStateType == nextType && currentState != null)
            return;

        if (currentState != null && !currentState.CanExit())
            return;

        currentState?.Exit();
        currentState = CreateState(nextType);
        CurrentStateType = nextType;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    private AIState CreateState(AIStateType stateType)
    {
        return stateType switch
        {
            AIStateType.Dead => new IdleState(owner, context, memory),
            AIStateType.Flee => new FleeState(owner, context, memory),
            AIStateType.Fight => new FightState(owner, context, memory),
            AIStateType.Eat => new EatState(owner, context, memory),
            AIStateType.Hunt => new HuntState(owner, context, memory),
            AIStateType.Rest => new RestState(owner, context, memory),
            AIStateType.Patrol => new PatrolState(owner, context, memory),
            AIStateType.ReturnHome => new ReturnHomeState(owner, context, memory),
            AIStateType.Wander => new WanderState(owner, context, memory),
            _ => new IdleState(owner, context, memory)
        };
    }
}
