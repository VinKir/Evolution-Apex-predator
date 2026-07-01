using UnityEngine;
using UnityEngine.UI;

public class PlayerWorldProgressUI : MonoBehaviour
{
    [Header("Eat")]
    [SerializeField] private GameObject eatRoot;
    [SerializeField] private Slider eatSlider;

    [Header("Mutation")]
    [SerializeField] private GameObject mutationRoot;
    [SerializeField] private Slider mutationSlider;

    public void SetEatProgress(float progress01, bool visible)
    {
        if (eatRoot != null)
            eatRoot.SetActive(visible);

        if (eatSlider != null)
            eatSlider.value = Mathf.Clamp01(progress01);
    }

    public void SetMutationProgress(float progress01, bool visible)
    {
        if (mutationRoot != null)
            mutationRoot.SetActive(visible);

        if (mutationSlider != null)
            mutationSlider.value = Mathf.Clamp01(progress01);
    }
}