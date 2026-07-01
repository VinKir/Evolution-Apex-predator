using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VariantChoiceWindowUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;

    [Header("Options")]
    [SerializeField] private Transform optionsRoot;
    [SerializeField] private Button optionButtonPrefab;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private ConfirmPopupUI confirmPopup;

    private readonly List<Button> spawnedButtons = new();
    private BodyPartVariantSO selectedVariant;
    private string currentPartName;
    private int currentMilestone;
    private Action<BodyPartVariantSO> onConfirmed;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(AskConfirm);
    }

    public void Open(string partDisplayName, int milestoneLevel, List<BodyPartVariantSO> options, Action<BodyPartVariantSO> confirmedCallback)
    {
        currentPartName = partDisplayName;
        currentMilestone = milestoneLevel;
        onConfirmed = confirmedCallback;
        selectedVariant = null;

        if (root != null)
            root.SetActive(true);

        if (titleText != null)
            titleText.text = $"Выберите улучшение для {partDisplayName} (уровень {milestoneLevel})";

        if (infoText != null)
            infoText.text = "Выберите вариант слева.";

        ClearButtons();

        if (options == null)
            options = new List<BodyPartVariantSO>();

        foreach (var option in options)
        {
            if (option == null)
                continue;

            var btn = Instantiate(optionButtonPrefab, optionsRoot);
            spawnedButtons.Add(btn);

            var text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = option.displayName;

            btn.onClick.AddListener(() => Select(option));
        }

        if (confirmButton != null)
            confirmButton.interactable = false;
    }

    private void Select(BodyPartVariantSO variant)
    {
        selectedVariant = variant;

        if (infoText != null)
        {
            infoText.text =
                $"<b>{variant.displayName}</b>\n\n" +
                $"{variant.description}\n\n" +
                $"Открывается на уровне: {variant.unlockLevel}";
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void AskConfirm()
    {
        if (selectedVariant == null)
            return;

        if (confirmPopup != null)
        {
            string msg = $"Вы хотите выбрать улучшение \"{selectedVariant.displayName}\" для {currentPartName}?";
            confirmPopup.Show(msg, ConfirmSelection);
        }
        else
        {
            ConfirmSelection();
        }
    }

    private void ConfirmSelection()
    {
        if (root != null)
            root.SetActive(false);

        var chosen = selectedVariant;
        selectedVariant = null;

        onConfirmed?.Invoke(chosen);
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);

        ClearButtons();
        selectedVariant = null;
        onConfirmed = null;
    }

    private void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();
    }
}