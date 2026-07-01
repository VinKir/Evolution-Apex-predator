using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BodyPartRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image buttonImage;

    private Action onClick;
    private Color defaultButtonColor;

    private void Awake()
    {
        if (buttonImage != null)
            defaultButtonColor = buttonImage.color;

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public void Bind(
        string displayName,
        int committedLevel,
        int queuedAdds,
        int nextCost,
        bool blockedByLimit,
        bool canAfford,
        bool isLocked,
        Action onClickCallback)
    {
        onClick = onClickCallback;

        string currentText = committedLevel > 0 ? $" (+{committedLevel})" : "";
        string pendingText = queuedAdds > 0 ? $" <color=green>-> (+{committedLevel + queuedAdds})</color>" : "";

        if (titleText != null)
            titleText.text = $"{displayName}{currentText}{pendingText}";

        if (costText != null)
        {
            costText.text = blockedByLimit
                ? "<color=red>Предел мутации</color>"
                : $"[Улучшить {nextCost} ОБ]";
        }

        bool interactable = canAfford && !blockedByLimit && !isLocked;

        if (upgradeButton != null)
            upgradeButton.interactable = interactable;

        if (buttonImage != null)
            buttonImage.color = blockedByLimit ? new Color(0.9f, 0.35f, 0.35f, 1f) : defaultButtonColor;
    }
}