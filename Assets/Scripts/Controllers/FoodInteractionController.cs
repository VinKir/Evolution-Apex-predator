using UnityEngine;
using UnityEngine.UI;

public class FoodInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerMovementController movement;
    [SerializeField] private PlayerActionLock actionLock;
    [SerializeField] private PlayerProgression progression;
    [SerializeField] private PlayerWorldProgressUI worldProgressUI;
    [SerializeField] private Button eatButton;
    [SerializeField] private MutationUIController mutationUI;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 1.25f;
    [SerializeField] private float frontDotThreshold = 0.35f;

    private FoodItem currentFood;
    private bool isEating;

    private void Start()
    {
        if (eatButton != null)
        {
            eatButton.onClick.AddListener(ToggleEating);
            eatButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isEating)
        {
            EatTick();
            return;
        }

        bool show = CanShowEatButton();
        if (eatButton != null)
            eatButton.gameObject.SetActive(show);
    }

    private bool CanShowEatButton()
    {
        if (currentFood == null || actionLock == null || actionLock.IsMutating)
            return false;

        if (movement == null)
            return false;

        float distance = Vector2.Distance(transform.position, currentFood.transform.position);
        if (distance > interactionDistance)
            return false;

        Vector2 toFood = ((Vector2)currentFood.transform.position - (Vector2)transform.position).normalized;
        float dot = Vector2.Dot(movement.FacingDirection.normalized, toFood);

        return dot >= frontDotThreshold;
    }

    public void ToggleEating()
    {
        if (actionLock != null && actionLock.IsMutating)
            return;

        if (!isEating)
        {
            if (!CanShowEatButton())
                return;

            StartEating();
        }
        else
        {
            StopEating(false);
        }
    }

    private void StartEating()
    {
        if (currentFood == null || isEating)
            return;

        isEating = true;
        if (actionLock != null) actionLock.SetEating(true);
        if (movement != null) movement.enabled = false;
        if (mutationUI != null) mutationUI.ClosePanel();

        worldProgressUI?.SetEatProgress(currentFood.ConsumedProgress, true);
    }

    private void EatTick()
    {
        if (currentFood == null)
        {
            StopEating(false);
            return;
        }

        float delta01 = Time.deltaTime / Mathf.Max(0.01f, currentFood.ConsumeDuration);
        float gained = currentFood.AddProgress(delta01);

        if (gained > 0f && progression != null)
            progression.AddBiomass(gained);

        worldProgressUI?.SetEatProgress(currentFood.ConsumedProgress, true);

        if (currentFood.IsFullyEaten)
        {
            StopEating(true);
            Destroy(currentFood.gameObject);
            currentFood = null;
        }
    }

    private void StopEating(bool consumedCompleted)
    {
        isEating = false;

        if (actionLock != null)
            actionLock.SetEating(false);

        if (movement != null)
            movement.enabled = true;

        if (!consumedCompleted)
        {
            worldProgressUI?.SetEatProgress(0f, false);
        }
        else
        {
            worldProgressUI?.SetEatProgress(1f, false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out FoodItem food))
        {
            currentFood = food;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentFood != null && other.gameObject == currentFood.gameObject)
        {
            if (isEating)
                StopEating(false);

            currentFood = null;
            eatButton?.gameObject.SetActive(false);
            worldProgressUI?.SetEatProgress(0f, false);
        }
    }
}