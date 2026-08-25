using UnityEngine;

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

        return context.IsHungry || context.HealthRatio < 0.9f;
    }

    public bool NeedRun(AIContext context)
    {
        if (context == null)
            return false;

        if (!context.HasThreat || context.CurrentThreat == null)
            return false;

        if (context.IsLowHealth && context.CurrentThreat.CurrentBodyHpNormalized > 0.30f)
            return true;

        return IsStrongerThan(context.CurrentThreat, context.Combatant, 0.4f);
    }

    public bool NeedFight(AIContext context)
    {
        if (context == null)
            return false;

        if (context.CanAttack && context.CurrentTarget != null)
            return true;

        if (context.HasThreat && context.CurrentThreat != null)
        {
            if (IsStrongerThan(context.CurrentThreat, context.Combatant, 0.4f))
                return false;

            return true;
        }

        return false;
    }

    public bool NeedSleep(AIContext context)
    {
        if (context == null)
            return false;

        return context.StaminaRatio < 0.15f && context.HasThreat == false;
    }

    public bool IsStrongerThan(OrganismCombatant candidate, OrganismCombatant self, float margin)
    {
        if (candidate == null || self == null)
            return false;

        return candidate.CombatPower >= self.CombatPower * (1f + margin);
    }
}
