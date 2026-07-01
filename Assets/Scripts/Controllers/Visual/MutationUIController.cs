using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MutationUIController : MonoBehaviour
{
    [System.Serializable]
    private class MilestoneRequest
    {
        public string partId;
        public string partDisplayName;
        public int milestoneLevel;
        public List<BodyPartVariantSO> options = new();
    }

    [Header("Core")]
    [SerializeField] private PlayerProgression progression;
    [SerializeField] private PlayerBody body;
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private PlayerMovementController movement;

    [Header("Visuals")]
    [SerializeField] private BodyVisualController worldBodyVisual;
    [SerializeField] private BodyVisualController previewBodyVisual;
    [SerializeField] private PlayerWorldProgressUI worldProgressUI;

    [Header("Window")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("UI")]
    [SerializeField] private TMP_Text biomassText;
    [SerializeField] private Button mutateButton;
    [SerializeField] private Button evolveButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform rowsRoot;
    [SerializeField] private BodyPartRowUI rowPrefab;

    [Header("Popups")]
    [SerializeField] private SimplePopupUI alertPopup;
    [SerializeField] private VariantChoiceWindowUI variantWindow;

    [Header("Main menu button stays active")]
    [SerializeField] private Button mainMenuButton;

    private readonly Dictionary<string, BodyPartRowUI> rows = new();
    private readonly Dictionary<string, int> pendingLevelAdds = new();
    private readonly List<BodyVariantSelection> chosenVariants = new();
    private readonly Queue<MilestoneRequest> requestQueue = new();

    private bool mutationInProgress;
    private float mutationTimer;
    private const float mutationDuration = 6f;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (mutateButton != null)
            mutateButton.onClick.AddListener(OnMutatePressed);

        if (evolveButton != null)
            evolveButton.onClick.AddListener(OnEvolvePressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        BuildRows();
        RefreshAll();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (!mutationInProgress)
            return;

        mutationTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(mutationTimer / mutationDuration);

        worldProgressUI?.SetMutationProgress(progress, true);

        if (worldBodyVisual != null)
            worldBodyVisual.SetTransactionProgress(progress);

        if (previewBodyVisual != null)
            previewBodyVisual.SetTransactionProgress(progress);

        if (mutationTimer >= mutationDuration)
        {
            FinishMutation();
        }
    }

    public void OpenPanel()
    {
        if (actionLock != null && !actionLock.CanOpenMutation)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshAll();
    }

    public void ClosePanel()
    {
        if (mutationInProgress)
            return;

        if (variantWindow != null)
            variantWindow.Close();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void TogglePanel()
    {
        if (IsOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    private void BuildRows()
    {
        if (body == null || rowsRoot == null || rowPrefab == null)
            return;

        body.EnsureStates();

        foreach (var state in body.States)
        {
            if (state == null || state.definition == null)
                continue;

            if (rows.ContainsKey(state.definition.partId))
                continue;

            var row = Instantiate(rowPrefab, rowsRoot);
            rows.Add(state.definition.partId, row);
        }
    }

    private void RefreshAll()
    {
        if (body == null)
            return;

        body.EnsureStates();

        float pendingCost = GetTotalPendingCost();
        float remainingBiomass = progression != null ? progression.Biomass - pendingCost : 0f;

        if (biomassText != null && progression != null)
        {
            if (pendingCost > 0f)
                biomassText.text = $"{progression.Biomass:0.##} <color=red>({remainingBiomass:0.##})</color>";
            else
                biomassText.text = $"{progression.Biomass:0.##}";
        }

        foreach (var state in body.States)
        {
            if (state == null || state.definition == null)
                continue;

            if (!rows.TryGetValue(state.definition.partId, out var row) || row == null)
                continue;

            int queuedAdds = GetQueuedAdds(state.definition.partId);
            int currentLevel = state.level;
            int nextCost = currentLevel + queuedAdds + 1;

            bool blockedByLimit = nextCost > (progression != null ? progression.MutationCap : 5);
            bool canAfford = progression != null && progression.Biomass >= (pendingCost + nextCost);

            row.Bind(
                state.definition.displayName,
                currentLevel,
                queuedAdds,
                nextCost,
                blockedByLimit,
                canAfford,
                mutationInProgress || (variantWindow != null && variantWindow.IsOpen),
                () => OnUpgradeClicked(state.definition.partId)
            );
        }

        if (mutateButton != null)
            mutateButton.interactable = !mutationInProgress && pendingCost > 0f;

        if (evolveButton != null)
            evolveButton.interactable = !mutationInProgress && progression != null && progression.CanEvolve;
    }

    private void OnUpgradeClicked(string partId)
    {
        if (body == null || progression == null)
            return;

        var state = body.GetState(partId);
        if (state == null || state.definition == null)
            return;

        int queuedAdds = GetQueuedAdds(partId);
        int currentLevel = state.level;
        int nextLevel = currentLevel + queuedAdds + 1;

        if (nextLevel > progression.MutationCap)
        {
            alertPopup?.Show("Вы достигли предела мутации на текущем уровне эволюции.");
            return;
        }

        int nextCost = nextLevel;
        float pendingCost = GetTotalPendingCost();

        if (progression.Biomass < pendingCost + nextCost)
        {
            alertPopup?.Show("Недостаточно Очков Биомассы.");
            return;
        }

        pendingLevelAdds[partId] = queuedAdds + 1;
        RefreshAll();
    }

    private void OnMutatePressed()
    {
        if (body == null || progression == null || mutationInProgress)
            return;

        float totalCost = GetTotalPendingCost();
        if (totalCost <= 0f)
            return;

        if (!progression.SpendBiomass(totalCost))
        {
            alertPopup?.Show("Недостаточно Очков Биомассы.");
            return;
        }

        BuildMilestoneRequests();
        RefreshAll();

        if (requestQueue.Count > 0)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = true;
            }

            OpenNextVariantRequest();
            return;
        }

        BeginMutationProgress();
    }

    private void BuildMilestoneRequests()
    {
        requestQueue.Clear();
        chosenVariants.Clear();

        foreach (var state in body.States)
        {
            if (state == null || state.definition == null)
                continue;

            int current = state.level;
            int queued = GetQueuedAdds(state.definition.partId);
            int target = current + queued;

            for (int lvl = current + 1; lvl <= target; lvl++)
            {
                if (lvl % 5 != 0)
                    continue;

                // Все варианты для этого milestone уровня
                var allOptions = state.definition.GetVariantsForLevel(lvl);

                if (allOptions == null || allOptions.Count == 0)
                    continue;

                // Уже применённые варианты у этой части тела
                var alreadyApplied = state.appliedVariants
                    .Where(v => v != null && v.variant != null)
                    .Select(v => v.variant)
                    .ToHashSet();

                // Уже выбранные в текущей очереди мутации
                var alreadyChosenThisSession = chosenVariants
                    .Where(v => v != null &&
                                v.partId == state.definition.partId &&
                                v.variant != null)
                    .Select(v => v.variant)
                    .ToHashSet();

                // Убираем повторяющиеся варианты
                var availableOptions = allOptions
                    .Where(v =>
                        v != null &&
                        !alreadyApplied.Contains(v) &&
                        !alreadyChosenThisSession.Contains(v))
                    .ToList();

                // Если всё уже выбрано — пропускаем
                if (availableOptions.Count == 0)
                    continue;

                requestQueue.Enqueue(new MilestoneRequest
                {
                    partId = state.definition.partId,
                    partDisplayName = state.definition.displayName,
                    milestoneLevel = lvl,
                    options = availableOptions
                });
            }
        }
    }

    private void OpenNextVariantRequest()
    {
        if (variantWindow == null)
        {
            BeginMutationProgress();
            return;
        }

        if (requestQueue.Count == 0)
        {
            BeginMutationProgress();
            return;
        }

        var request = requestQueue.Dequeue();

        variantWindow.Open(
            request.partDisplayName,
            request.milestoneLevel,
            request.options,
            selectedVariant =>
            {
                chosenVariants.Add(new BodyVariantSelection
                {
                    partId = request.partId,
                    partDisplayName = request.partDisplayName,
                    milestoneLevel = request.milestoneLevel,
                    variant = selectedVariant
                });

                if (worldBodyVisual != null)
                    worldBodyVisual.BeginTransactionPreview(chosenVariants);

                if (previewBodyVisual != null)
                    previewBodyVisual.BeginTransactionPreview(chosenVariants);

                if (requestQueue.Count > 0)
                {
                    OpenNextVariantRequest();
                }
                else
                {
                    BeginMutationProgress();
                }
            });
    }

    private void BeginMutationProgress()
    {
        mutationInProgress = true;
        mutationTimer = 0f;

        if (variantWindow != null)
            variantWindow.Close();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (actionLock != null)
            actionLock.SetMutating(true);

        if (movement != null)
            movement.enabled = false;

        if (worldProgressUI != null)
            worldProgressUI.SetMutationProgress(0f, true);

        if (worldBodyVisual != null)
            worldBodyVisual.BeginTransactionPreview(chosenVariants);

        if (previewBodyVisual != null)
            previewBodyVisual.BeginTransactionPreview(chosenVariants);
    }

    private void FinishMutation()
    {
        mutationInProgress = false;

        body.ApplyMutation(pendingLevelAdds, chosenVariants);

        worldBodyVisual?.ClearTransactionPreview();
        previewBodyVisual?.ClearTransactionPreview();

        worldBodyVisual?.RebuildFromBody();
        previewBodyVisual?.RebuildFromBody();

        worldProgressUI?.SetMutationProgress(1f, false);

        pendingLevelAdds.Clear();
        chosenVariants.Clear();
        requestQueue.Clear();

        if (actionLock != null)
            actionLock.SetMutating(false);

        if (movement != null)
            movement.enabled = true;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        RefreshAll();
    }

    private void OnEvolvePressed()
    {
        if (progression == null || body == null || mutationInProgress)
            return;

        if (!progression.CanEvolve)
        {
            alertPopup?.Show("Эволюция доступна только на 5 уровне.");
            return;
        }

        progression.Evolve();

        pendingLevelAdds.Clear();
        chosenVariants.Clear();
        requestQueue.Clear();

        body.EnsureStates();
        worldBodyVisual?.RebuildFromBody();
        previewBodyVisual?.RebuildFromBody();

        RefreshAll();

        ClosePanel();
    }

    private int GetQueuedAdds(string partId)
    {
        return pendingLevelAdds.TryGetValue(partId, out int value) ? value : 0;
    }

    private float GetTotalPendingCost()
    {
        if (body == null)
            return 0f;

        float total = 0f;

        foreach (var state in body.States)
        {
            if (state == null || state.definition == null)
                continue;

            int queued = GetQueuedAdds(state.definition.partId);
            for (int i = 1; i <= queued; i++)
            {
                total += state.level + i;
            }
        }

        return total;
    }
}