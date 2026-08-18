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

        gameObject.SetActive(true);

        Refresh();
    }

    public void Unbind()
    {
        Combatant = null;
    }

    public void Refresh()
    {
        if (Combatant == null)
            return;

        SetSlider(
            chitinHpSlider,
            Combatant.CurrentChitinHp,
            Combatant.Stats.maxChitinHp
        );

        SetSlider(
            bodyHpSlider,
            Combatant.CurrentBodyHp,
            Combatant.Stats.maxBodyHp
        );

        SetSlider(
            staminaSlider,
            Combatant.CurrentStamina,
            Combatant.Stats.maxStamina
        );
    }

    private static void SetSlider(
        Slider slider,
        float current,
        float max)
    {
        if (slider == null)
            return;

        max = Mathf.Max(0.01f, max);

        slider.minValue = 0f;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(current, 0f, max);
    }
}