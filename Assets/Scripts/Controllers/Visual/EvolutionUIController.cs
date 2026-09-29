using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionPurchasePlan
{
    public int strengthExt = 0;
    public int strengthInt = 0;
    public int enduranceExt = 0;
    public int enduranceInt = 0;

    public readonly List<BodyPartDefinitionSO> purchasedOrgans = new();

    public int EvolutionPointsSpent;
}

public class EvolutionUIController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private OrganismProgression progression;
    [SerializeField] private OrganismCombatant combatant;
    [SerializeField] private PlayerBody body;
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private PlayerMovementController movement;

    [Header("World Progress")]
    [SerializeField] private PlayerWorldProgressUI worldProgressUI;

    [Header("Windows")]
    [SerializeField] private GameObject evolutionPanel;
    [SerializeField] private GameObject purchasesPanel;

    [Header("Evolution List")]
    [SerializeField] private Transform evolutionRowsRoot;
    [SerializeField] private EvolutionRowUI evolutionRowPrefab;

    [Header("Evolution Info")]
    [SerializeField] private TMP_Text evolutionNameText;
    [SerializeField] private TMP_Text evolutionDescriptionText;
    [SerializeField] private TMP_Text evolutionBonusesText;

    [Header("Buttons")]
    [SerializeField] private Button confirmEvolutionButton;
    [SerializeField] private Button additionalButton;
    [SerializeField] private Button openEvoPanelButton;
    [SerializeField] private Button closeButton;
    
    [Header("Purchases")]
    [SerializeField] private TMP_Text evolutionPointsText;

    [Header("Organ Purchases")]
    [SerializeField] private Transform organPurchaseRowsRoot;
    [SerializeField] private OrganPurchaseRowUI organPurchaseRowPrefab;

    [Header("Organ Purchase Info")]
    [SerializeField] private TMP_Text selectedOrganNameText;
    [SerializeField] private TMP_Text selectedOrganDescriptionText;
    [SerializeField] private Button selectOrganButton;
    [SerializeField] private TMP_Text selectOrganButtonText;

    [Header("Stat Rows")]
    [SerializeField] private EvolutionStatRowUI strengthExtRow;
    [SerializeField] private EvolutionStatRowUI strengthIntRow;
    [SerializeField] private EvolutionStatRowUI enduranceExtRow;
    [SerializeField] private EvolutionStatRowUI enduranceIntRow;

    [Header("Popup")]
    [SerializeField] private SimplePopupUI alertPopup;

    private EvolutionSO selectedEvolution;
    private EvolutionPurchasePlan purchasePlan;

    private readonly List<OrganPurchaseRowUI> organPurchaseRows = new();

    private BodyPartDefinitionSO selectedPurchaseOrgan;

    private float evolutionTimer;
    private bool evolutionInProgress;

    private int previewEvolutionPointsBonus;

    private readonly List<EvolutionRowUI> evolutionRows = new();

    public bool IsOpen => evolutionPanel != null && evolutionPanel.activeSelf;

    private void Awake()
    {
        confirmEvolutionButton?.onClick.AddListener(OnConfirmEvolution);
        additionalButton?.onClick.AddListener(OpenPurchases);
        openEvoPanelButton?.onClick.AddListener(Open);
        closeButton?.onClick.AddListener(Close);

        purchasePlan = new EvolutionPurchasePlan();

        strengthExtRow?.Initialize(this);
        strengthIntRow?.Initialize(this);
        enduranceExtRow?.Initialize(this);
        enduranceIntRow?.Initialize(this);

        if (evolutionPanel != null)
            evolutionPanel.SetActive(false);
        if (purchasesPanel != null)
            purchasesPanel.SetActive(false);
    }

    private void Update()
    {
        openEvoPanelButton.interactable = progression.CanEvolve;

        if (!evolutionInProgress)
            return;

        evolutionTimer += Time.deltaTime;

        float duration = Mathf.Max(0.1f, selectedEvolution.evolutionDuration);
        float progress = Mathf.Clamp01(evolutionTimer / duration);

        worldProgressUI?.SetMutationProgress(progress, true);

        if (evolutionTimer >= duration)
            FinishEvolution();
    }

    public void Open()
    {
        if (!progression.CanEvolve || evolutionInProgress)
            return;

        purchasePlan = new EvolutionPurchasePlan();

        BuildEvolutionList();
        UpdateEvolutionInfo();
        evolutionPanel.SetActive(true);
    }

    public void Close()
    {
        if (evolutionInProgress)
            return;

        purchasesPanel?.SetActive(false);
        evolutionPanel?.SetActive(false);

        selectedEvolution = null;
        purchasePlan = null;
        previewEvolutionPointsBonus = 0;
    }

    private void BuildEvolutionList()
    {
        foreach (var row in evolutionRows)
                Destroy(row.gameObject);

        evolutionRows.Clear();

        var current = progression.CurrentEvolution;

        if (current == null)
            return;

        foreach (var evolution in current.nextEvolutions)
        {
            if (!evolution.CanBeSelected(progression, body))
                continue;

            var row = Instantiate(evolutionRowPrefab, evolutionRowsRoot);

            row.Setup(evolution, () => SelectEvolution(evolution));

            evolutionRows.Add(row);
        }
    }

    private int CalculateDuplicateEvolutionPoints(EvolutionSO evolution)
    {
        int total = 0;

        foreach (var mutation in evolution.mutations)
            if (body.HasMutation(mutation.bodyPart.partId, mutation))
                total += mutation.priceInEvolutionPoints * evolution.evolutionStage;

        foreach (var organ in evolution.organs)
            if (body.HasOrgan(organ))
                total += organ.priceInEvolutionPoints * evolution.evolutionStage;

        return total;
    }

    private void SelectEvolution(EvolutionSO evolution)
    {
        // Убираем временный бонус предыдущей выбранной эволюции.
        if (previewEvolutionPointsBonus != 0)
        {
            purchasePlan.EvolutionPointsSpent += previewEvolutionPointsBonus;
            previewEvolutionPointsBonus = 0;
        }

        selectedEvolution = evolution;

        // Начисляем временный бонус новой выбранной эволюции.
        previewEvolutionPointsBonus = CalculateDuplicateEvolutionPoints(evolution);
        purchasePlan.EvolutionPointsSpent -= previewEvolutionPointsBonus;

        foreach (var row in evolutionRows)
            row.SetSelected(row.Evolution == evolution);

        UpdateEvolutionInfo();
        RefreshPurchases();
    }

    private void UpdateEvolutionInfo()
    {
        if (selectedEvolution == null)
        {
            evolutionNameText.text = "Выберите эволюцию";
            evolutionDescriptionText.text = "";
            evolutionBonusesText.text = "";
            confirmEvolutionButton.interactable = false;
            return;
        }

        evolutionNameText.text = selectedEvolution.displayName;
        evolutionDescriptionText.text = selectedEvolution.description;
        evolutionBonusesText.text = BuildEvolutionSummary();
        confirmEvolutionButton.interactable = !evolutionInProgress && purchasePlan.EvolutionPointsSpent >= GetRemainingEvolutionPoints();
    }

    private string BuildEvolutionSummary()
    {
        float strengthExt = progression.StrengthExt + selectedEvolution.strengthExtBonus + purchasePlan.strengthExt;
        float strengthInt = progression.StrengthInt + selectedEvolution.strengthIntBonus + purchasePlan.strengthInt;
        float enduranceExt = progression.EnduranceExt + selectedEvolution.enduranceExtBonus + purchasePlan.enduranceExt;
        float enduranceInt = progression.EnduranceInt + selectedEvolution.enduranceIntBonus + purchasePlan.enduranceInt;

        var text = "";

        text += $"Сила внешняя: {progression.StrengthExt:0.##} → {strengthExt:0.##}\n";
        text += $"Сила внутренняя: {progression.StrengthInt:0.##} → {strengthInt:0.##}\n";
        text += $"Выносливость внешняя: {progression.EnduranceExt:0.##} → {enduranceExt:0.##}\n";
        text += $"Выносливость внутренняя: {progression.EnduranceInt:0.##} → {enduranceInt:0.##}\n";

        text += "\n\nОрганы:\n";

        foreach (var organ in selectedEvolution.organs)
            text += $"{organ.displayName}\n";

        foreach (var organ in purchasePlan.purchasedOrgans)
                text += $"{organ.displayName}\n";

        text += "\nМутации:\n";

        foreach (var mutation in selectedEvolution.mutations)
            text += $"Мутация: {mutation.displayName}\n";

        return text;
    }

    private void OpenPurchases()
    {
        if (selectedEvolution == null)
        {
            alertPopup?.Show("Сначала выберите эволюцию.");
            return;
        }

        purchasesPanel?.SetActive(true);

        RefreshPurchases();
    }

    public int GetRemainingEvolutionPoints()
    {
        return progression.EvolutionPoints - purchasePlan.EvolutionPointsSpent;
    }


    #region BuyStats

    private void RefreshStatRows()
    {
        strengthExtRow?.Refresh();
        strengthIntRow?.Refresh();
        enduranceExtRow?.Refresh();
        enduranceIntRow?.Refresh();
    }

    public float GetInitialStatValue(EvolutionStatType stat)
    {
        return stat switch
        {
            EvolutionStatType.StrengthExt => progression.StrengthExt,
            EvolutionStatType.StrengthInt => progression.StrengthInt,
            EvolutionStatType.EnduranceExt => progression.EnduranceExt,
            EvolutionStatType.EnduranceInt => progression.EnduranceInt,
            _ => 0f
        };
    }
    
    public int GetStatIncrease(EvolutionStatType stat)
    {
        return Mathf.RoundToInt(stat switch
        {
            EvolutionStatType.StrengthExt => purchasePlan.strengthExt,
            EvolutionStatType.StrengthInt => purchasePlan.strengthInt,
            EvolutionStatType.EnduranceExt => purchasePlan.enduranceExt,
            EvolutionStatType.EnduranceInt => purchasePlan.enduranceInt,
            _ => 0
        });
    }

    private void AddStatIncrease(EvolutionStatType stat)
    {
        switch (stat)
        {
            case EvolutionStatType.StrengthExt:
                purchasePlan.strengthExt++;
                break;
            case EvolutionStatType.StrengthInt:
                purchasePlan.strengthInt++;
                break;
            case EvolutionStatType.EnduranceExt:
                purchasePlan.enduranceExt++;
                break;
            case EvolutionStatType.EnduranceInt:
                purchasePlan.enduranceInt++;
                break;
        }
    }

    private void RemoveStatIncrease(EvolutionStatType stat)
    {
        switch (stat)
        {
            case EvolutionStatType.StrengthExt:
                purchasePlan.strengthExt--;
                break;
            case EvolutionStatType.StrengthInt:
                purchasePlan.strengthInt--;
                break;
            case EvolutionStatType.EnduranceExt:
                purchasePlan.enduranceExt--;
                break;
            case EvolutionStatType.EnduranceInt:
                purchasePlan.enduranceInt--;
                break;
        }
    }

    public int GetStatUpgradeCost(int increaseNumber)
    {
        if (selectedEvolution == null)
            return 0;

        return selectedEvolution.evolutionStage * increaseNumber;
    }

    public int GetNextStatUpgradeCost(EvolutionStatType stat)
    {
        int currentIncrease = GetStatIncrease(stat);
        int nextIncrease = currentIncrease + 1;
        return GetStatUpgradeCost(nextIncrease);
    }

    public bool TryBuyStat(EvolutionStatType stat)
    {
        if (selectedEvolution == null)
            return false;

        int currentIncrease = GetStatIncrease(stat);

        int nextIncrease = currentIncrease + 1;

        int cost = GetStatUpgradeCost(nextIncrease);

        if (GetRemainingEvolutionPoints() < cost)
            return false;

        AddStatIncrease(stat);

        purchasePlan.EvolutionPointsSpent += cost;

        RefreshPurchases();
        UpdateEvolutionInfo();

        return true;
    }

    public bool TryRemoveStat(EvolutionStatType stat)
    {
        int currentIncrease = GetStatIncrease(stat);

        if (currentIncrease <= 0)
            return false;

        int cost = GetStatUpgradeCost(currentIncrease);

        RemoveStatIncrease(stat);

        purchasePlan.EvolutionPointsSpent -= cost;

        RefreshPurchases();
        UpdateEvolutionInfo();

        return true;
    }
    #endregion

    #region PurchaseOrgans
    private void RefreshSelectedOrgan()
    {
        if (selectedPurchaseOrgan == null)
        {
            selectedOrganNameText.text = "";
            selectedOrganDescriptionText.text = "";
            selectOrganButton.gameObject.SetActive(false);
            return;
        }

        selectedOrganNameText.text = selectedPurchaseOrgan.displayName;
        selectedOrganDescriptionText.text = selectedPurchaseOrgan.description;
        selectOrganButton.gameObject.SetActive(true);

        bool alreadyPurchased = purchasePlan.purchasedOrgans.Contains(selectedPurchaseOrgan);

        if (alreadyPurchased)
        {
            selectOrganButtonText.text = "Убрать";
            selectOrganButton.interactable = true;
        }
        else
        {
            int price = GetOrganPurchasePrice(selectedPurchaseOrgan);
            selectOrganButtonText.text = "Выбрать";
            selectOrganButton.interactable = progression.EvolutionPoints >= price;
        }

        selectOrganButton.onClick.RemoveAllListeners();
        selectOrganButton.onClick.AddListener(ToggleSelectedOrganPurchase);
    }

    private void SelectPurchaseOrgan(BodyPartDefinitionSO organ)
    {
        selectedPurchaseOrgan = organ;
        RefreshSelectedOrgan();
    }

    private void BuildOrganPurchaseList()
    {
        foreach (var row in organPurchaseRows)
            Destroy(row.gameObject);

        organPurchaseRows.Clear();

        foreach (var organ in selectedEvolution.purchasableOrgans)
        {
            if (body.HasOrgan(organ))
                continue;

            int price = GetOrganPurchasePrice(organ);
            bool selected = purchasePlan.purchasedOrgans.Contains(organ);
            bool canAfford = GetRemainingEvolutionPoints() >= price;
            var row = Instantiate(organPurchaseRowPrefab, organPurchaseRowsRoot);
            row.Bind(organ, price, canAfford, selected, () => SelectPurchaseOrgan(organ));

            organPurchaseRows.Add(row);
        }
    }

    private int GetOrganPurchasePrice(BodyPartDefinitionSO organ)
    {
        return organ.priceInEvolutionPoints * selectedEvolution.evolutionStage;
    }

    private void ToggleSelectedOrganPurchase()
    {
        bool alreadyPurchased = purchasePlan.purchasedOrgans.Contains(selectedPurchaseOrgan);
        int price = GetOrganPurchasePrice(selectedPurchaseOrgan);
        if (alreadyPurchased)
        {
            purchasePlan.purchasedOrgans.Remove(selectedPurchaseOrgan);
            purchasePlan.EvolutionPointsSpent -= price;
        }
        else
        {
            if (GetRemainingEvolutionPoints() < price)
                return;

            purchasePlan.purchasedOrgans.Add(selectedPurchaseOrgan);
            purchasePlan.EvolutionPointsSpent += price;
        }
        RefreshPurchases();
        UpdateEvolutionInfo();
    }

    public bool IsOrganPurchased(BodyPartDefinitionSO organ)
    {
        return purchasePlan.purchasedOrgans.Contains(organ);
    }
    #endregion

    private void RefreshPurchases()
    {
        evolutionPointsText.text = GetRemainingEvolutionPoints().ToString();

        BuildOrganPurchaseList();
        RefreshSelectedOrgan();
        RefreshStatRows();
    }

    private void OnConfirmEvolution()
    {
        if (purchasePlan.EvolutionPointsSpent > progression.EvolutionPoints)
        {
            alertPopup?.Show("Недостаточно Очков Эволюции. Отмените часть покупок.");
            return;
        }

        if (!selectedEvolution.CanBeSelected(progression, body))
        {
            alertPopup?.Show("Вы больше не соответствуете требованиям этой эволюции.");
            return;
        }

        StartEvolution();
    }

    private void StartEvolution()
    {
        evolutionInProgress = true;

        evolutionTimer = 0f;

        evolutionPanel?.SetActive(false);
        purchasesPanel?.SetActive(false);

        actionLock?.SetMutating(true);

        movement.enabled = false;

        worldProgressUI?.SetMutationProgress(0f, true);
    }

    private void FinishEvolution()
    {
        evolutionInProgress = false;

        ApplyEvolution();

        worldProgressUI?.SetMutationProgress(1f, false);

        actionLock?.SetMutating(false);

        movement.enabled = true;

        selectedEvolution = null;
        purchasePlan = null;

        previewEvolutionPointsBonus = 0;

        combatant?.RecalculateStats();
    }

    private void ApplyEvolution()
    {
        progression.ApplyEvolution(
            selectedEvolution,
            purchasePlan.strengthExt,
            purchasePlan.strengthInt,
            purchasePlan.enduranceExt,
            purchasePlan.enduranceInt,
            purchasePlan.EvolutionPointsSpent);

        ApplyEvolutionMutations(selectedEvolution);

        foreach (var organ in selectedEvolution.organs)
            body.AddOrgan(organ);

        foreach (var organ in purchasePlan.purchasedOrgans)
            body.AddOrgan(organ);

        body.EnsureStates();
    }

    private void ApplyEvolutionMutations(EvolutionSO evolution)
    {
        var mutations_to_add_obj = new List<BodyVariantSelection>();

        foreach (var mutation in evolution.mutations)
        {
            if (body.HasMutation(mutation.bodyPart.partId, mutation))
                continue;

            mutations_to_add_obj.Add(new BodyVariantSelection() 
                {
                    partId = mutation.bodyPart.partId,
                    partDisplayName = mutation.displayName,
                    milestoneLevel = mutation.unlockLevel,
                    variant = mutation
                });
        }

        body.ApplyMutations(null, mutations_to_add_obj);
    }
}