using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private Joystick joystick;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationOffset = -90f;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    private Vector2 input;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (actionLock != null && !actionLock.CanMove)
        {
            input = Vector2.zero;

            if (animator != null)
                animator.SetBool("IsMoving", false);

            return;
        }

        input = joystick != null ? joystick.Direction : Vector2.zero;

        bool isMoving = input.sqrMagnitude > 0.01f;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            FacingDirection = input.normalized;
            RotateToDirection(FacingDirection);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (actionLock != null && !actionLock.CanMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = input.normalized * moveSpeed;
    }

    private void RotateToDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle + rotationOffset;
    }
}