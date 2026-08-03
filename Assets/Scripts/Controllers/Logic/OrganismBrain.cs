public class OrganismBrain
{
    private readonly AINeeds needs = new();

    public AIStateType DecideState(AIContext context, AIMemory memory)
    {
        if (context == null || context.Combatant == null)
            return AIStateType.Idle;

        if (context.Combatant.IsDead)
            return AIStateType.Dead;

        if (needs.NeedRun(context))
            return AIStateType.Flee;

        if (needs.NeedRest(context))
            return AIStateType.Rest;

        if (needs.NeedFight(context))
            return AIStateType.Fight;

        if (needs.NeedFood(context) && context.NearestFood != null)
            return AIStateType.Eat;

        if (context.HasTarget && context.CurrentTarget != null)
            return AIStateType.Hunt;

        if (context.HomePoint != null && !context.IsHome)
            return AIStateType.ReturnHome;

        if (context.HomePoint != null && context.VisibleEnemies.Count == 0)
            return AIStateType.Patrol;

        if (context.IsHungry && context.NearestFood != null)
            return AIStateType.Eat;

        return AIStateType.Wander;
    }
}
