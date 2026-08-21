using UnityEngine;
using UnityEngine.UI;

public class OrganismStatusBar : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider chitinHpSlider;
    [SerializeField] private Slider bodyHpSlider;
    [SerializeField] private Slider staminaSlider;

    public OrganismCombatant Combatant { get; private set; }

    public void Bind(OrganismCombatant combatant)
    {
        Combatant = combatant;
        Combatant.OnChitinHpChanged += RefreshChitinHp;
        Combatant.OnBodyHpChanged += RefreshBodyHp;
        Combatant.OnStaminaChanged += RefreshStamina;
        Combatant.OnRecalculated += RefreshStats;

        RefreshStats();

        gameObject.SetActive(true);
    }

    public void Unbind()
    {
        if (Combatant != null)
        {
            Combatant.OnChitinHpChanged -= RefreshChitinHp;
            Combatant.OnBodyHpChanged -= RefreshBodyHp;
            Combatant.OnStaminaChanged -= RefreshStamina;
            Combatant.OnRecalculated -= RefreshStats;

            Combatant = null;
        }

    }

    private void RefreshStats()
    {
        if (Combatant == null)
            return;

        SetSliderMax(chitinHpSlider, Combatant.Stats.maxChitinHp);
        SetSliderMax(bodyHpSlider, Combatant.Stats.maxBodyHp);
        SetSliderMax(staminaSlider, Combatant.Stats.maxStamina);

        RefreshChitinHp();
        RefreshBodyHp();
        RefreshStamina();
    }

    private void RefreshChitinHp()
    {
        if (Combatant == null)
            return;

        SetSliderValue(chitinHpSlider, Combatant.CurrentChitinHp);
    }

    private void RefreshBodyHp()
    {
        if (Combatant == null)
            return;

        SetSliderValue(bodyHpSlider, Combatant.CurrentBodyHp);
    }

    private void RefreshStamina()
    {
        if (Combatant == null)
            return;

        SetSliderValue(staminaSlider, Combatant.CurrentStamina);
    }

    private static void SetSliderMax(Slider slider, float max)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = Mathf.Max(0.01f, max);
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.value = Mathf.Clamp(
            value,
            slider.minValue,
            slider.maxValue
        );
    }
}