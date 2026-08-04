public class OrganismBrain
{
    private readonly AINeeds needs = new();

    public AIStateType DecideState(AIContext context, AIMemory memory)
    {
        if (context == null || context.Combatant == null)
            return AIStateType.Idle;

        if (context.Combatant.IsDead)
            return AIStateType.Dead;

        if (context.BehaviourType == OrganismBehaviourType.Scavenger)
        {
            if (IsDirectlyThreatened(context, memory))
                return AIStateType.Flee;

            if (context.NearestFood != null)
                return AIStateType.Eat;

            return AIStateType.Wander;
        }

        if (context.BehaviourType == OrganismBehaviourType.Guardian)
        {
            if (ownerWasAttacked(context, memory))
            {
                if (context.HomePoint != null && context.DistanceToHome > context.HomeRadius * 0.8f)
                    return AIStateType.ReturnHome;

                return AIStateType.Fight;
            }

            if (context.HomePoint != null && !context.IsHome)
                return AIStateType.ReturnHome;

            if (context.HomePoint != null && context.VisibleEnemies.Count == 0)
                return AIStateType.Patrol;

            if (context.HasThreat && context.CurrentThreat != null)
                return AIStateType.Fight;

            return AIStateType.Patrol;
        }

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

        if (context.IsHungry && context.NearestFood != null)
            return AIStateType.Eat;

        return AIStateType.Wander;
    }

    private bool ownerWasAttacked(AIContext context, AIMemory memory)
    {
        if (context == null || memory == null)
            return false;

        return memory.LastThreat != null && !memory.LastThreat.IsDead && context.HasThreat;
    }

    private bool IsDirectlyThreatened(AIContext context, AIMemory memory)
    {
        if (context == null || memory == null)
            return false;

        return ownerWasAttacked(context, memory);
    }
}
