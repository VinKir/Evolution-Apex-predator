using UnityEngine;

public class AIMemory
{
    public OrganismCombatant LastAttacker { get; set; }

    public void Update(float deltaTime)
    {
        if (LastAttacker != null && LastAttacker.IsDead)
            LastAttacker = null;
    }
}
