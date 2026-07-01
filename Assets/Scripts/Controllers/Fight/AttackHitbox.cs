using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D hitCollider;
    private OrganismCombatant owner;
    private bool activeSwing;
    private readonly HashSet<int> hitTargets = new();

    private void Awake()
    {
        if (hitCollider == null)
            hitCollider = GetComponent<Collider2D>();

        if (hitCollider != null)
            hitCollider.enabled = false;
    }

    public void Setup(OrganismCombatant newOwner)
    {
        owner = newOwner;
    }

    public void SetActiveSwing(bool value)
    {
        activeSwing = value;
        hitTargets.Clear();

        if (hitCollider != null)
            hitCollider.enabled = value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activeSwing || owner == null)
            return;

        if (!other.TryGetComponent(out BodyHitbox bodyHitbox))
            return;

        var defender = bodyHitbox.GetComponentInParent<OrganismCombatant>();
        if (defender == null || defender == owner)
            return;

        if (hitTargets.Contains(defender.GetInstanceID()))
            return;

        hitTargets.Add(defender.GetInstanceID());
        owner.ResolveMeleeHit(defender, bodyHitbox);
    }
}