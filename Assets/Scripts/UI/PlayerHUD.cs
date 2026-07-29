using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private OrganismCombatant combatant;

    [Header("Bars")]
    [SerializeField] private Slider chitinBar;
    [SerializeField] private Slider bodyBar;
    [SerializeField] private Slider staminaBar;

    private void Update()
    {
        UpdateBars();
    }

    private void UpdateBars()
    {
        if (combatant == null)
            return;

        chitinBar.value = combatant.CurrentChitinHp / combatant.Stats.maxChitinHp;
        bodyBar.value = combatant.CurrentBodyHp / combatant.Stats.maxBodyHp;
        staminaBar.value = combatant.CurrentStamina / combatant.Stats.maxStamina;
    }
}