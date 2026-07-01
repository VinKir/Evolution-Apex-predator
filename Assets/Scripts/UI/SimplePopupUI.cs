using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimplePopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button okButton;

    private Action onClosed;

    private void Awake()
    {
        if (okButton != null)
            okButton.onClick.AddListener(Hide);
    }

    public void Show(string message, Action onClosedCallback = null)
    {
        onClosed = onClosedCallback;
        if (messageText != null)
            messageText.text = message;

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        onClosed?.Invoke();
        onClosed = null;
    }
}