using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OrganismMovementMotor : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private OrganismCombatant combatant;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [Header("Rotation")]
    [SerializeField] private float rotationOffset = -90f;
    [SerializeField] private float baseTurnSpeed = 50f;
    [SerializeField] private float turnSlowdownAngle = 45f;
    [SerializeField] private float turnSlowdownMultiplier = 0.1f;

    [Header("Movement")]
    [SerializeField] private float lowStaminaSpeedMultiplier = 0.5f;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    public Vector2 DesiredDirection { get; private set; }

    public bool MovementLocked { get; set; }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        combatant = GetComponent<OrganismCombatant>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (combatant == null)
            combatant = GetComponent<OrganismCombatant>();
    }

    public void SetDesiredDirection(Vector2 direction)
    {
        DesiredDirection = direction;
    }

    public void Stop()
    {
        DesiredDirection = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (MovementLocked || (combatant != null && combatant.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetBool("IsMoving", false);
            return;
        }

        Vector2 input = DesiredDirection;
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);

        if (!isMoving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDirection = input.normalized;
        float angleToMove = Vector2.Angle(GetCurrentFacingDirection(), moveDirection);

        float moveSpeed = combatant != null ? combatant.Stats.moveSpeed : 0f;
        if (combatant != null && combatant.CurrentStamina <= 0.1f)
            moveSpeed *= lowStaminaSpeedMultiplier;

        float speedMultiplier = GetMoveSpeedMultiplier(angleToMove);

        RotateToDirection(moveDirection);
        rb.linearVelocity = moveDirection * (moveSpeed * speedMultiplier);

        FacingDirection = moveDirection;
    }

    private float GetMoveSpeedMultiplier(float angleToMove)
    {
        if (angleToMove <= turnSlowdownAngle)
            return 1f;

        float t = Mathf.InverseLerp(turnSlowdownAngle, 180f, angleToMove);
        return Mathf.Lerp(1f, turnSlowdownMultiplier, t);
    }

    private void RotateToDirection(Vector2 direction)
    {
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;

        float turnSpeed = baseTurnSpeed;
        if (combatant != null)
            turnSpeed += combatant.Stats.turnSpeed;

        float newAngle = Mathf.MoveTowardsAngle(
            rb.rotation,
            targetAngle,
            turnSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newAngle);
    }

    private Vector2 GetCurrentFacingDirection()
    {
        float angle = (rb.rotation - rotationOffset) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }
}