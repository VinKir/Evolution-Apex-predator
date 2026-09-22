using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image background;
    [SerializeField] private Button button;

    private EvolutionSO evolution;

    public EvolutionSO Evolution => evolution;

    public void Setup(EvolutionSO evolution, Action onClick)
    {
        this.evolution = evolution;
        nameText.text = evolution.displayName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (background != null && selected)
            background.color = Color.grey;
        else
            background.color = Color.white;
    }
}