using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action yesAction;

    private void Awake()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(ConfirmYes);

        if (noButton != null)
            noButton.onClick.AddListener(ConfirmNo);
    }

    public void Show(string message, Action onYes)
    {
        yesAction = onYes;

        if (messageText != null)
            messageText.text = message;

        if (root != null)
            root.SetActive(true);
    }

    private void ConfirmYes()
    {
        if (root != null)
            root.SetActive(false);

        var cb = yesAction;
        yesAction = null;
        cb?.Invoke();
    }

    private void ConfirmNo()
    {
        if (root != null)
            root.SetActive(false);

        yesAction = null;
    }
}