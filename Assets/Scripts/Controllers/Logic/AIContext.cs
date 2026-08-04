using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIContext
{
    public Transform OwnerTransform { get; set; }
    public OrganismCombatant Combatant { get; set; }
    public OrganismMovementMotor Movement { get; set; }
    public EnemySpawnPoint HomePoint { get; set; }
    public OrganismBehaviourType BehaviourType { get; set; }

    public Vector2 CurrentPosition { get; set; }
    public float CurrentHp { get; set; }
    public float CurrentStamina { get; set; }
    public float CurrentMana { get; set; }
    public float CurrentCombatPower { get; set; }
    public float HealthRatio { get; set; }
    public float StaminaRatio { get; set; }

    public OrganismCombatant CurrentTarget { get; set; }
    public OrganismCombatant CurrentThreat { get; set; }
    public OrganismCombatant NearestEnemy { get; set; }
    public FoodItem NearestFood { get; set; }
    public OrganismCombatant NearestAlly { get; set; }

    public List<OrganismCombatant> VisibleEnemies { get; } = new();
    public List<OrganismCombatant> VisibleAllies { get; } = new();
    public List<FoodItem> VisibleFood { get; } = new();

    public float DistanceToHome { get; set; }
    public float DistanceToEnemy { get; set; }
    public float DistanceToFood { get; set; }

    public bool CanAttack { get; set; }
    public bool CanEat { get; set; }
    public bool CanMove { get; set; }
    public bool HasTarget { get; set; }
    public bool HasThreat { get; set; }
    public bool IsHungry { get; set; }
    public bool IsLowHealth { get; set; }
    public bool IsLowStamina { get; set; }
    public bool IsHome { get; set; }

    public float AttackRange { get; set; }
    public float EatRange { get; set; }
    public float HomeRadius { get; set; }

    public void Reset()
    {
        VisibleEnemies.Clear();
        VisibleAllies.Clear();
        VisibleFood.Clear();

        CurrentTarget = null;
        CurrentThreat = null;
        NearestEnemy = null;
        NearestFood = null;
        NearestAlly = null;
    }

    public void UpdateFrom(EnemyAIController owner, AIMemory memory)
    {
        Reset();

        if (owner == null)
            return;

        OwnerTransform = owner.transform;
        Combatant = owner.Combatant;
        Movement = owner.Movement;
        HomePoint = owner.BasePoint;
        BehaviourType = owner.BehaviourType;

        if (Combatant == null)
            return;

        CurrentPosition = owner.transform.position;
        CurrentHp = Combatant.CurrentBodyHp;
        CurrentStamina = Combatant.CurrentStamina;
        CurrentMana = 0f;
        CurrentCombatPower = Combatant.CombatPower;
        HealthRatio = Combatant.CurrentBodyHpNormalized;
        StaminaRatio = Combatant.Stats.maxStamina <= 0.001f ? 0f : Combatant.CurrentStamina / Combatant.Stats.maxStamina;

        AttackRange = owner.AttackRange;
        EatRange = owner.EatRange;
        HomeRadius = HomePoint != null ? HomePoint.activationRadius * 0.35f : 0.25f;

        if (HomePoint != null)
        {
            DistanceToHome = Vector2.Distance(CurrentPosition, HomePoint.transform.position);
            IsHome = DistanceToHome <= HomeRadius;
        }
        else
        {
            DistanceToHome = 0f;
            IsHome = true;
        }

        var colliders = Physics2D.OverlapCircleAll(CurrentPosition, Combatant.Stats.detectionRadius);

        foreach (var collider in colliders)
        {
            if (collider == null)
                continue;

            var organism = collider.GetComponentInParent<OrganismCombatant>();
            if (organism != null && organism != Combatant)
            {
                if (Combatant.IsFriendlyTo(organism))
                    VisibleAllies.Add(organism);
                else
                    VisibleEnemies.Add(organism);
            }

            var food = collider.GetComponent<FoodItem>();
            if (food != null && !food.IsFullyEaten)
                VisibleFood.Add(food);
        }

        CurrentThreat = SelectThreat(memory, owner);
        CurrentTarget = SelectTarget(memory);
        DistanceToEnemy = CurrentTarget == null ? 999f : Vector2.Distance(CurrentPosition, CurrentTarget.transform.position);

        NearestEnemy = GetNearestVisibleEnemy();
        NearestFood = GetNearestVisibleFood();
        NearestAlly = GetNearestVisibleAlly();

        if (NearestFood != null)
        {
            DistanceToFood = Vector2.Distance(CurrentPosition, NearestFood.transform.position);
            CurrentTarget = null;
        }
        else
        {
            DistanceToFood = 999f;
        }

        HasThreat = CurrentThreat != null && !CurrentThreat.IsDead;
        HasTarget = CurrentTarget != null && !CurrentTarget.IsDead;
        CanAttack = HasTarget && DistanceToEnemy <= AttackRange && CurrentStamina > 0.01f;
        CanEat = NearestFood != null && DistanceToFood <= EatRange;
        CanMove = !Combatant.IsDead;
        IsHungry = HealthRatio < 0.7f;
        IsLowHealth = HealthRatio < 0.35f;
        IsLowStamina = StaminaRatio < 0.25f;
    }

    private OrganismCombatant SelectThreat(AIMemory memory, EnemyAIController owner)
    {
        if (owner == null || Combatant == null)
            return null;

        var visibleThreats = VisibleEnemies
            .Where(o => o != null && !o.IsDead)
            .ToList();

        if (visibleThreats.Count == 0)
            return memory != null ? memory.LastThreat : null;

        var preferred = visibleThreats
            .OrderBy(o => Vector2.Distance(CurrentPosition, o.transform.position))
            .FirstOrDefault();

        return preferred;
    }

    private OrganismCombatant SelectTarget(AIMemory memory)
    {
        if (Combatant == null)
            return null;

        if (VisibleEnemies.Count > 0)
        {
            return VisibleEnemies
                .Where(o => o != null && !o.IsDead)
                .OrderBy(o => Vector2.Distance(CurrentPosition, o.transform.position))
                .FirstOrDefault();
        }

        return memory != null ? memory.CurrentTarget : null;
    }

    private OrganismCombatant GetNearestVisibleEnemy()
    {
        if (VisibleEnemies.Count == 0)
            return null;

        return VisibleEnemies
            .OrderBy(o => Vector2.Distance(CurrentPosition, o.transform.position))
            .FirstOrDefault();
    }

    private OrganismCombatant GetNearestVisibleAlly()
    {
        if (VisibleAllies.Count == 0)
            return null;

        return VisibleAllies
            .OrderBy(o => Vector2.Distance(CurrentPosition, o.transform.position))
            .FirstOrDefault();
    }

    private FoodItem GetNearestVisibleFood()
    {
        if (VisibleFood.Count == 0)
            return null;

        return VisibleFood
            .OrderBy(f => Vector2.Distance(CurrentPosition, f.transform.position))
            .FirstOrDefault();
    }
}
