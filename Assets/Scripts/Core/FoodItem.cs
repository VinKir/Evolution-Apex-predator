using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FoodItem : MonoBehaviour
{
    [Header("Food data")]
    [SerializeField] private float totalBiomass = 1f;
    [SerializeField] private float consumeDuration = 10f;

    [Header("Runtime progress")]
    [SerializeField, Range(0f, 1f)]
    private float consumedProgress = 0f;

    public float TotalBiomass => totalBiomass;
    public float ConsumeDuration => consumeDuration;
    public float ConsumedProgress => consumedProgress;
    public bool IsFullyEaten => consumedProgress >= 0.999f;

    /// <summary>
    /// Настройка параметров при создании объекта в рантайме.
    /// </summary>
    public void InitializeRuntime(float biomass, float duration)
    {
        totalBiomass = Mathf.Max(0.01f, biomass);
        consumeDuration = Mathf.Max(0.01f, duration);
        consumedProgress = 0f;
    }

    /// <summary>
    /// Добавляет прогресс поедания (0..1)
    /// и возвращает количество полученной биомассы.
    /// Биомасса выдается по четвертям: 25%, 50%, 75%, 100%.
    /// </summary>
    public float AddProgress(float progressDelta01)
    {
        if (progressDelta01 <= 0f || IsFullyEaten)
            return 0f;

        float before = consumedProgress;
        consumedProgress = Mathf.Clamp01(consumedProgress + progressDelta01);

        int beforeQuarter = Mathf.FloorToInt(before * 4f);
        int afterQuarter = Mathf.FloorToInt(consumedProgress * 4f);

        float gainedBiomass =
            Mathf.Max(0, afterQuarter - beforeQuarter) * (totalBiomass / 4f);

        return gainedBiomass;
    }

    [ContextMenu("Reset Consume Progress")]
    public void ResetConsumeProgress()
    {
        consumedProgress = 0f;
    }
}