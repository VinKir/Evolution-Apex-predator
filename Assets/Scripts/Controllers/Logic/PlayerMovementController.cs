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
            FacingDirection = input.normalized;
            RotateToDirection(FacingDirection);
        }

        rb.linearVelocity = input * combatant.Stats.moveSpeed;
    }

    private void RotateToDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.MoveRotation(angle + rotationOffset);
    }
}