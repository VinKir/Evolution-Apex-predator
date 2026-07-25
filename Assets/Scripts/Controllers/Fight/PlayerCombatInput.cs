using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatInput : MonoBehaviour
{
    [SerializeField] private OrganismCombatant combatant;
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.Attack.performed += OnAttack;
    }

    private void OnDisable()
    {
        controls.Player.Attack.performed -= OnAttack;

        controls.Disable();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        OnAttackButton();
    }

    public void OnAttackButton()
    {
        if (combatant != null)
            combatant.TryStartMeleeAttack();
    }
}