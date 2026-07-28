using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private Joystick joystick;
    [SerializeField] private OrganismMovementMotor movement;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrganismMovementMotor>();
    }

    private void FixedUpdate()
    {
        if (movement == null)
            return;

        movement.MovementLocked = actionLock != null && !actionLock.CanMove;

        if (movement.MovementLocked)
        {
            movement.Stop();
            return;
        }

        movement.SetDesiredDirection(joystick != null ? joystick.Direction : Vector2.zero);
    }
}