using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Templates")]
    [SerializeField] private List<EnemyTemplateSO> scavengerTemplates = new();
    [SerializeField] private List<EnemyTemplateSO> predatorTemplates = new();
    [SerializeField] private List<EnemyTemplateSO> guardianTemplates = new();

    [Header("Spawn Points")]
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new();

    [Header("Roaming Spawn")]
    [SerializeField] private float roamSpawnInterval = 8f;
    [SerializeField] private int maxRoamingEnemies = 20;
    [SerializeField] private float roamMinSpawnDistance = 12f;
    [SerializeField] private float roamMaxSpawnDistance = 24f;

    [Header("Culling")]
    [SerializeField] private float despawnDistance = 45f;

    private float nextRoamSpawnTime;

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
            spawnPoints = FindObjectsByType<EnemySpawnPoint>().ToList();
    }

    private void Update()
    {
        if (player == null)
            return;

        CullFarEnemies();

        if (Time.time >= nextRoamSpawnTime)
        {
            TrySpawnRoamers();
            nextRoamSpawnTime = Time.time + roamSpawnInterval;
        }

        TrySpawnGuardiansNearBases();
    }

    private void TrySpawnRoamers()
    {
        int currentRoamers = FindObjectsByType<EnemyAIController>().Count(e => e.IsRoamingEnemy);
        if (currentRoamers >= maxRoamingEnemies)
            return;

        var template = PickRandom(scavengerTemplates.Concat(predatorTemplates).ToList());
        if (template == null || template.prefab == null)
            return;

        Vector2 spawnPos = RandomPointAroundPlayer(roamMinSpawnDistance, roamMaxSpawnDistance);
        SpawnEnemy(template, spawnPos, null);
    }

    private void TrySpawnGuardiansNearBases()
    {
        foreach (var point in spawnPoints.Where(p => p != null && p.isBase))
        {
            if (player == null)
                continue;

            if (Vector2.Distance(player.position, point.transform.position) > point.activationRadius)
                continue;

            int current = FindObjectsByType<EnemyAIController>()
                .Count(e => e.BasePoint == point && !e.IsRoamingEnemy);

            if (current >= point.maxGuardians)
                continue;

            var template = point.guardianTemplate != null ? point.guardianTemplate : PickRandom(guardianTemplates);
            if (template == null || template.prefab == null)
                continue;

            Vector2 pos = (Vector2)point.transform.position + Random.insideUnitCircle.normalized * point.spawnRadius;
            SpawnEnemy(template, pos, point);
        }
    }

    private void SpawnEnemy(EnemyTemplateSO template, Vector2 position, EnemySpawnPoint basePoint)
    {
        var go = Instantiate(template.prefab, position, Quaternion.identity);
        var combatant = go.GetComponent<OrganismCombatant>();
        var ai = go.GetComponent<EnemyAIController>();

        if (combatant != null)
        {
            int level = Random.Range(
                Mathf.Max(1, PlayerLevel() + template.minLevelOffset),
                Mathf.Max(2, PlayerLevel() + template.maxLevelOffset + 1)
            );

            int evo = Random.Range(
                Mathf.Max(1, PlayerEvo() + template.minEvolutionOffset),
                Mathf.Max(2, PlayerEvo() + template.maxEvolutionOffset + 1)
            );

            int groupId = basePoint != null ? basePoint.factionGroupId : Random.Range(1000, 9999);

            combatant.ConfigureEnemy(template, level, evo, groupId);
        }

        if (ai != null)
        {
            ai.Initialize(player, basePoint);
        }
    }

    private void CullFarEnemies()
    {
        foreach (var enemy in FindObjectsByType<EnemyAIController>())
        {
            if (enemy == null)
                continue;

            if (Vector2.Distance(player.position, enemy.transform.position) > despawnDistance)
                Destroy(enemy.gameObject);
        }
    }

    private Vector2 RandomPointAroundPlayer(float minDist, float maxDist)
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        float dist = Random.Range(minDist, maxDist);
        return (Vector2)player.position + dir * dist;
    }

    private IEnumerable<EnemyTemplateSO> roamingTemplates()
    {
        foreach (var t in scavengerTemplates) yield return t;
        foreach (var t in predatorTemplates) yield return t;
    }

    private EnemyTemplateSO PickRandom(List<EnemyTemplateSO> list)
    {
        if (list == null || list.Count == 0)
            return null;

        return list[Random.Range(0, list.Count)];
    }

    private int PlayerLevel()
    {
        var p = player != null ? player.GetComponent<OrganismProgression>() : null;
        return p != null ? p.Level : 1;
    }

    private int PlayerEvo()
    {
        var p = player != null ? player.GetComponent<OrganismProgression>() : null;
        return p != null ? p.EvolutionStage : 1;
    }
}