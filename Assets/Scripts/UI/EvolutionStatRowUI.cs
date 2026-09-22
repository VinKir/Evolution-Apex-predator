using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionStatRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text costText;

    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("Stat")]
    [SerializeField] private EvolutionStatType stat;

    private EvolutionUIController controller;

    public void Initialize(EvolutionUIController controller)
    {
        this.controller = controller;

        plusButton.onClick.RemoveAllListeners();
        minusButton.onClick.RemoveAllListeners();

        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);

        Refresh();
    }

    public void Refresh()
    {
        if (controller == null)
            return;

        float initialValue = controller.GetInitialStatValue(stat);
        int increase = controller.GetStatIncrease(stat);
        float resultValue = initialValue + increase;
        valueText.text =
            increase > 0
                ? $"<color=white>{initialValue:0.##}</color> -> <color=green>{resultValue:0.##}</color>"
                : $"<color=white>{initialValue:0.##}</color>";

        int nextCost = controller.GetNextStatUpgradeCost(stat);

        costText.text =
            increase >= 0
                ? $"{nextCost} ОЭ"
                : "";

        plusButton.interactable = controller.GetRemainingEvolutionPoints() >= controller.GetNextStatUpgradeCost(stat);
        minusButton.interactable = increase > 0;
    }

    private void OnPlusClicked()
    {
        if (controller.TryBuyStat(stat))
            Refresh();
    }

    private void OnMinusClicked()
    {
        if (controller.TryRemoveStat(stat))
            Refresh();
    }
}