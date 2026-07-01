using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public bool isBase = false;
    public int factionGroupId = 100;
    public float activationRadius = 12f;
    public float spawnRadius = 3.5f;
    public int maxGuardians = 3;
    public EnemyTemplateSO guardianTemplate;
}