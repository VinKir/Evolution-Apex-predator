using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerCombatInput : MonoBehaviour
{
    [SerializeField] private OrganismCombatant combatant;
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void Update()
    {
        if (!controls.Player.Attack.WasPressedThisFrame())
            return;

        if (IsPointerOverUI())
            return;

        OnAttackButton();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    public void OnAttackButton()
    {
        if (combatant != null)
            combatant.TryStartMeleeAttack();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Mouse.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed &&
                    EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                    return true;
            }
        }

        return false;
    }
}