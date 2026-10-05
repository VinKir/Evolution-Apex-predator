using UnityEngine;
using Unity.Cinemachine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private Joystick joystick;
    [SerializeField] private OrganismMovementMotor movement;
    [SerializeField] private OrganismCombatant combatant;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField, Min(0.01f)] private float baseOrthographicSize = 5f;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrganismMovementMotor>();

        if (combatant == null)
            combatant = GetComponent<OrganismCombatant>();
    }

    private void OnEnable()
    {
        if (combatant  != null)
            combatant.OnRecalculated += UpdateCameraSize;
    }

    private void Start()
    {
        UpdateCameraSize();
    }

    private void OnDisable()
    {
        if (combatant != null)
            combatant.OnRecalculated -= UpdateCameraSize;
    }

    private void UpdateCameraSize()
    {
        if (combatant == null || cinemachineCamera == null)
            return;

        var lens = cinemachineCamera.Lens;
        lens.OrthographicSize = Mathf.Max(0.01f, baseOrthographicSize * combatant.Stats.sizeMultiplier);
        cinemachineCamera.Lens = lens;
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