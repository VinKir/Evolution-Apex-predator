public class AINeeds
{
    public bool NeedRest(AIContext context)
    {
        if (context == null)
            return false;

        if (context.IsLowStamina)
            return true;

        return context.StaminaRatio < 0.5f && context.HasThreat == false && context.VisibleFood.Count == 0;
    }

    public bool NeedFood(AIContext context)
    {
        if (context == null)
            return false;

        return context.IsHungry || context.HealthRatio < 0.7f;
    }

    public bool NeedRun(AIContext context)
    {
        if (context == null)
            return false;

        return context.HasThreat && context.CurrentThreat != null && context.IsLowHealth;
    }

    public bool NeedFight(AIContext context)
    {
        if (context == null)
            return false;

        return context.CanAttack && context.CurrentTarget != null;
    }

    public bool NeedSleep(AIContext context)
    {
        if (context == null)
            return false;

        return context.StaminaRatio < 0.15f && context.HasThreat == false;
    }
}
