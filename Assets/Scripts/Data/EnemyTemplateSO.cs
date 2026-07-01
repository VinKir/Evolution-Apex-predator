using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Enemy Template", fileName = "EnemyTemplate")]
public class EnemyTemplateSO : ScriptableObject
{
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.Predator;
    public GameObject prefab;

    [Header("Level Range Around Player")]
    public int minLevelOffset = -1;
    public int maxLevelOffset = 2;
    public int minEvolutionOffset = 0;
    public int maxEvolutionOffset = 1;

    [Header("Base Stat Ranges")]
    public float baseStrengthExtMin = 0.8f;
    public float baseStrengthExtMax = 1.4f;
    public float baseStrengthIntMin = 0.8f;
    public float baseStrengthIntMax = 1.4f;

    public float baseEnduranceExtMin = 0.8f;
    public float baseEnduranceExtMax = 1.4f;
    public float baseEnduranceIntMin = 0.8f;
    public float baseEnduranceIntMax = 1.4f;

    public float baseMoveSpeedMin = 2.2f;
    public float baseMoveSpeedMax = 4.2f;
    public float baseTurnSpeedMin = 4f;
    public float baseTurnSpeedMax = 8f;
    public float baseDetectionRadiusMin = 4f;
    public float baseDetectionRadiusMax = 7f;

    [Header("Death Drop")]
    public float corpseBiomassMultiplier = 1f;
    public float corpseConsumeDuration = 10f;
}