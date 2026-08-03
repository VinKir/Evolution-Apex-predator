using UnityEngine;

public class AIMemory
{
    public OrganismCombatant LastThreat { get; set; }
    public OrganismCombatant CurrentTarget { get; set; }
    public FoodItem CurrentFood { get; set; }
    public Vector2 LastKnownEnemyPosition { get; set; }
    public float TimeSinceLastEnemy { get; set; }
    public float TimeSinceLastDamage { get; set; }

    public void Update(float deltaTime)
    {
        if (LastThreat != null && LastThreat.IsDead)
            LastThreat = null;

        if (CurrentTarget != null && CurrentTarget.IsDead)
            CurrentTarget = null;

        if (CurrentFood != null && CurrentFood.IsFullyEaten)
            CurrentFood = null;

        TimeSinceLastEnemy += deltaTime;
        TimeSinceLastDamage += deltaTime;
    }
}
