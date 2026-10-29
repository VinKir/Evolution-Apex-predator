using UnityEngine;

// Hand-placed boss: dormant until the player comes close, returns and heals when the player
// leaves the leash, grants a flag on death. Not managed or culled by EnemySpawner.
// Evolution, level, stats, organs and faction are set by hand on the organism components.
[RequireComponent(typeof(OrganismCombatant), typeof(EnemyAIController))]
public class BossController : MonoBehaviour
{
    private enum BossState
    {
        Dormant,
        Active,
        Returning
    }

    [Header("Distances")]
    [Tooltip("Босс просыпается, когда игрок ближе этого расстояния к точке появления")]
    [SerializeField, Min(0f)] private float activationDistance = 8f;
    [Tooltip("Если игрок дальше этого расстояния от точки появления, босс возвращается и восстанавливается")]
    [SerializeField, Min(0f)] private float leashDistance = 20f;
    [SerializeField, Min(0.1f)] private float homeRadius = 3f;

    [Header("Reward")]
    [SerializeField] private string rewardFlag;

    private OrganismCombatant combatant;
    private EnemyAIController ai;
    private Transform player;
    private OrganismProgression playerProgression;
    private Vector2 anchor;
    private BossState state = BossState.Dormant;

    private void Awake()
    {
        combatant = GetComponent<OrganismCombatant>();
        ai = GetComponent<EnemyAIController>();
        anchor = transform.position;
    }

    private void Start()
    {
        var playerMovement = FindAnyObjectByType<PlayerMovementController>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
            playerProgression = playerMovement.GetComponent<OrganismProgression>();
        }

        if (playerProgression != null && playerProgression.HasAchievementFlag(rewardFlag))
        {
            Destroy(gameObject);
            return;
        }

        combatant.OnDamagedBy += OnDamagedBy;
        combatant.OnDied += OnDied;
    }

    private void OnDestroy()
    {
        if (combatant != null)
        {
            combatant.OnDamagedBy -= OnDamagedBy;
            combatant.OnDied -= OnDied;
        }
    }

    private void Update()
    {
        if (player == null || combatant.IsDead)
            return;

        float playerDistance = Vector2.Distance(player.position, anchor);

        switch (state)
        {
            case BossState.Dormant:
                if (playerDistance <= activationDistance)
                    Activate();
                break;

            case BossState.Active:
                if (playerDistance > leashDistance)
                {
                    state = BossState.Returning;
                    ai.ForceReturnHome = true;
                }
                break;

            case BossState.Returning:
                if (Vector2.Distance(transform.position, anchor) <= homeRadius)
                {
                    state = BossState.Dormant;
                    ai.Deactivate();
                    combatant.RestoreFull();
                }
                break;
        }
    }

    private void Activate()
    {
        state = BossState.Active;
        ai.Initialize(player, true, anchor, homeRadius);
    }

    private void OnDamagedBy(OrganismCombatant attacker)
    {
        if (state == BossState.Dormant)
            Activate();
    }

    private void OnDied()
    {
        if (playerProgression != null)
            playerProgression.SetAchievementFlag(rewardFlag);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)anchor : transform.position;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, activationDistance);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, leashDistance);

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 1f);
        Gizmos.DrawWireSphere(center, homeRadius);
    }
}
