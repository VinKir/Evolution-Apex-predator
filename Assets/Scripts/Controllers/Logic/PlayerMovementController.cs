using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private OrganismCombatant combatant;
    [SerializeField] private Joystick joystick;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private float rotationOffset = -90f;
    [SerializeField] private float baseTurnSpeed = 50f;
    [SerializeField] private float turnSlowdownAngle = 45f;
    [SerializeField] private float turnSlowdownMultiplier = 0.1f;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    private Vector2 input;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (actionLock != null && !actionLock.CanMove)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetBool("IsMoving", false);
            return;
        }

        input = joystick.Direction;
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            Vector2 moveDirection = input.normalized;

            float angleToMove = Vector2.Angle(GetCurrentFacingDirection(), moveDirection);

            float moveSpeed = combatant != null ? combatant.Stats.moveSpeed : 0f;
            float speedMultiplier = GetMoveSpeedMultiplier(angleToMove);

            RotateToDirection(moveDirection);

            rb.linearVelocity = moveDirection * (moveSpeed * speedMultiplier);
            FacingDirection = moveDirection;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
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