using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private OrganismCombatant combatant;

    [Header("Bars")]
    [SerializeField] private Slider chitinBar;
    [SerializeField] private Slider bodyBar;
    [SerializeField] private Slider jawsBar;
    [SerializeField] private Slider legsBar;
    [SerializeField] private Slider staminaBar;

    [SerializeField] private Image chitinRegrowImage;
    [SerializeField] private Image jawsRegrowImage;
    [SerializeField] private Image legsRegrowImage;

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
        jawsBar.value = combatant.CurrentJawsHp / combatant.Stats.maxJawHp;
        legsBar.value = Math.Min(combatant.CurrentLeftLegHp, combatant.CurrentRightLegHp) / combatant.Stats.maxLegHp;
        staminaBar.value = combatant.CurrentStamina / combatant.Stats.maxStamina;

        UpdateRegrowImage(chitinRegrowImage, BodyPartType.Chitin, combatant.Stats.chitinRegrowPercent, combatant.Stats.chitinRegrowCooldown);
        UpdateRegrowImage(jawsRegrowImage, BodyPartType.Jaws, combatant.Stats.jawsRegrowPercent, combatant.Stats.jawsRegrowCooldown);
        UpdateRegrowImage(legsRegrowImage, BodyPartType.Legs, combatant.Stats.legsRegrowPercent, combatant.Stats.legsRegrowCooldown);
    }

    private void UpdateRegrowImage(Image image, BodyPartType part, float regrowPercent, float cooldown)
    {
        if (image == null)
            return;

        bool abilityAvailable = regrowPercent > 0f;
        image.gameObject.SetActive(abilityAvailable);

        if (!abilityAvailable)
            return;

        float remaining = combatant.GetRegrowCooldownRemaining(part);
        float fillAmount = float.IsPositiveInfinity(remaining) || remaining == float.MaxValue || cooldown <= 0f
            ? 0f
            : 1f - Mathf.Clamp01(remaining / cooldown);

        image.fillAmount = fillAmount;
        image.color = fillAmount >= 1f ? Color.white : Color.gray;
    }
}