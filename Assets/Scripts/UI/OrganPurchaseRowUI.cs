using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrganPurchaseRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image background;

    private BodyPartDefinitionSO organ;
    private Action onClick;

    public BodyPartDefinitionSO Organ => organ;

    public void Bind(
        BodyPartDefinitionSO organ,
        int price,
        bool canAfford,
        bool selected,
        Action onClick)
    {
        this.organ = organ;
        this.onClick = onClick;

        nameText.text = organ.displayName;
        priceText.text = $"({price} ОЭ)";

        selectButton.interactable = selected || canAfford;

        nameText.color = canAfford || selected ? Color.white : Color.gray;
        priceText.color = canAfford || selected ? Color.white : Color.gray;

        if (background != null)
            background.color = selected ? Color.green : Color.white;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => this.onClick?.Invoke());
    }
}