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
            if (ownerWasAttacked(context, memory))
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

    /// <summary>
    /// Attempts to regrow disabled body parts.
    /// Should be called regularly from EnemyAIController's Think() or Update() loop.
    /// </summary>
    public void TryRegrowParts(AIContext context, AIMemory memory)
    {
        if (context?.Combatant == null || context.Combatant.IsDead)
            return;

        var combatant = context.Combatant;

        // Try to regrow each body part if it's disabled and ready
        if (combatant.CanRegrowPart(BodyPartType.Jaws) && combatant.GetRegrowCooldownRemaining(BodyPartType.Jaws) <= 0f)
            combatant.TryRegrowPart(BodyPartType.Jaws);

        if (combatant.CanRegrowPart(BodyPartType.Legs) && combatant.GetRegrowCooldownRemaining(BodyPartType.Legs) <= 0f)
            combatant.TryRegrowPart(BodyPartType.Legs);

        if (combatant.CanRegrowPart(BodyPartType.Chitin) && combatant.GetRegrowCooldownRemaining(BodyPartType.Chitin) <= 0f)
            combatant.TryRegrowPart(BodyPartType.Chitin);
    }

    private bool ownerWasAttacked(AIContext context, AIMemory memory)
    {
        if (context == null || memory == null)
            return false;

        return memory.LastAttacker != null && !memory.LastAttacker.IsDead;
    }
}
