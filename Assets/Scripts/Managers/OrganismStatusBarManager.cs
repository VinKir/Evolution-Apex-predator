using System.Collections.Generic;
using UnityEngine;

public class OrganismStatusBarManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private OrganismStatusBarPool pool;

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;

    [Header("Visibility")]
    [SerializeField] private bool hideDead = true;
    [SerializeField] private float viewportMargin = 0f;

    private readonly Dictionary<
        OrganismCombatant,
        OrganismStatusBar
    > activeBars = new();

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        SubscribeToRegistry();
    }

    private void Start()
    {
        SubscribeToRegistry();
    }

    private void OnDisable()
    {
        UnsubscribeFromRegistry();
        ReleaseAllBars();
    }

    private void SubscribeToRegistry()
    {
        if (OrganismCombatantRegistry.Instance == null)
            return;

        OrganismCombatantRegistry.Instance.CombatantUnregistered -= OnCombatantUnregistered;
        OrganismCombatantRegistry.Instance.CombatantUnregistered += OnCombatantUnregistered;
    }

    private void UnsubscribeFromRegistry()
    {
        if (OrganismCombatantRegistry.Instance == null)
            return;

        OrganismCombatantRegistry.Instance.CombatantUnregistered -= OnCombatantUnregistered;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || canvas == null || pool == null)
            return;

        OrganismCombatantRegistry registry = OrganismCombatantRegistry.Instance;

        if (registry == null)
            return;

        foreach (OrganismCombatant combatant in registry.Combatants)
        {
            if (combatant == null)
                continue;

            bool visible = IsCombatantVisible(combatant);

            if (visible)
                ShowOrUpdateBar(combatant);
            else
                HideBar(combatant);
        }
    }

    private void ShowOrUpdateBar(OrganismCombatant combatant)
    {
        if (!activeBars.TryGetValue(combatant, out OrganismStatusBar bar))
        {
            bar = pool.Get();

            if (bar == null)
                return;

            bar.transform.SetParent(canvas.transform, false);
            bar.Bind(combatant);

            activeBars.Add(combatant, bar);
        }

        UpdateBarPosition(combatant, bar);
    }

    private void HideBar(OrganismCombatant combatant)
    {
        if (!activeBars.TryGetValue(combatant, out OrganismStatusBar bar))
            return;

        activeBars.Remove(combatant);

        pool.Release(bar);
    }

    private void OnCombatantUnregistered(OrganismCombatant combatant)
    {
        HideBar(combatant);
    }

    private void UpdateBarPosition(
        OrganismCombatant combatant,
        OrganismStatusBar bar)
    {
        Vector3 worldPosition;

        if (combatant.StatusBarAnchor != null)
        {
            worldPosition = combatant.StatusBarAnchor.position;
        }
        else
        {
            worldPosition = combatant.transform.position;
        }

        Vector3 screenPosition =
            targetCamera.WorldToScreenPoint(worldPosition);

        RectTransform rectTransform =
            bar.transform as RectTransform;

        if (rectTransform == null)
            return;

        rectTransform.position = screenPosition;
    }

    private bool IsCombatantVisible(
        OrganismCombatant combatant)
    {
        if (hideDead && combatant.IsDead)
            return false;

        Vector3 worldPosition;

        if (combatant.StatusBarAnchor != null)
            worldPosition = combatant.StatusBarAnchor.position;
        else
            worldPosition = combatant.transform.position;

        Vector3 viewportPosition =
            targetCamera.WorldToViewportPoint(worldPosition);

        if (viewportPosition.z < 0f)
            return false;

        float min = -viewportMargin;
        float max = 1f + viewportMargin;

        return
            viewportPosition.x >= min &&
            viewportPosition.x <= max &&
            viewportPosition.y >= min &&
            viewportPosition.y <= max;
    }

    private void ReleaseAllBars()
    {
        foreach (OrganismStatusBar bar in activeBars.Values)
        {
            if (bar != null)
                pool.Release(bar);
        }

        activeBars.Clear();
    }
}