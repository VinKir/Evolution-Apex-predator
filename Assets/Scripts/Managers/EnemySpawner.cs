using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private class SpawnedEnemy
    {
        public EnemyAIController ai;
        public OrganismCombatant combatant;
        public EnemySpawnZone zone;
        public OrganismCombatant lastAttacker;
    }

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Spawning")]
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Слои стен и препятствий: точка спавна не должна их задевать")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private int spawnPointAttempts = 12;

    [Header("Distances")]
    [Tooltip("Камера, за пределами обзора которой появляются враги. Пусто = Camera.main")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Запас за углом экрана для минимальной дистанции спавна")]
    [SerializeField, Min(0f)] private float offscreenMargin = 2f;
    [Tooltip("Деспавн = минимальная дистанция спавна + этот запас. Спавн идёт в первой половине запаса")]
    [SerializeField, Min(0f)] private float despawnBuffer = 15f;

    // Half-diagonal of the camera view plus a margin, so enemies never appear on screen
    public float MinSpawnDistance
    {
        get
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
                return offscreenMargin;

            return cam.orthographicSize * Mathf.Sqrt(1f + cam.aspect * cam.aspect) + offscreenMargin;
        }
    }

    // Spawning stops halfway through the buffer, so a fresh enemy isn't culled after one step outward
    public float MaxSpawnDistance => MinSpawnDistance + despawnBuffer * 0.5f;
    public float DespawnDistance => MinSpawnDistance + despawnBuffer;

    private readonly List<SpawnedEnemy> spawnedEnemies = new();
    private readonly HashSet<EnemySpawnZone.EnemySpawnEntry> warnedEntries = new();
    private List<EnemySpawnZone> zones = new();
    private OrganismProgression playerProgression;
    private OrganismCombatant playerCombatant;
    private float nextTickTime;

    private void Start()
    {
        zones = new List<EnemySpawnZone>(FindObjectsByType<EnemySpawnZone>());

        if (player != null)
        {
            playerProgression = player.GetComponent<OrganismProgression>();
            playerCombatant = player.GetComponent<OrganismCombatant>();
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        if (Time.time < nextTickTime)
            return;

        nextTickTime = Time.time + tickInterval;

        CullFarEnemies(DespawnDistance);
        TrySpawnInZones(MinSpawnDistance, MaxSpawnDistance);
    }

    private void TrySpawnInZones(float minSpawnDistance, float maxSpawnDistance)
    {
        Vector2 playerPosition = player.position;

        foreach (var zone in zones)
        {
            if (zone == null)
                continue;

            zone.IsDisabled = zone.IsDisabledFor(playerProgression);
            if (zone.IsDisabled)
                continue;

            // The zone doesn't reach the spawn ring
            if (zone.DistanceToEdge(playerPosition) > maxSpawnDistance)
                continue;

            if (!zone.CanSpawnNow)
                continue;

            if (TryFindSpawnPoint(zone, playerPosition, minSpawnDistance, maxSpawnDistance, out Vector2 spawnPosition))
                SpawnEnemy(zone, spawnPosition);
        }
    }

    private bool TryFindSpawnPoint(EnemySpawnZone zone, Vector2 playerPosition, float minSpawnDistance, float maxSpawnDistance, out Vector2 spawnPosition)
    {
        for (int attempt = 0; attempt < spawnPointAttempts; attempt++)
        {
            if (!zone.TryGetRandomPoint(playerPosition, minSpawnDistance, maxSpawnDistance, out spawnPosition))
                continue;

            if (obstacleMask != 0 && Physics2D.OverlapCircle(spawnPosition, zone.spawnClearance, obstacleMask) != null)
                continue;

            return true;
        }

        spawnPosition = default;
        return false;
    }

    private void SpawnEnemy(EnemySpawnZone zone, Vector2 position)
    {
        var entry = zone.PickEntry();
        if (entry == null)
            return;

        var evolution = entry.PickEvolution();
        if (evolution == null)
        {
            if (warnedEntries.Add(entry))
                Debug.LogWarning($"EnemySpawnZone '{zone.name}': entry '{entry.prefab.name}' has no evolutions, skipped", zone);
            return;
        }

        // OrganismProgression.InitializeRuntime clamps the level to the evolution's cap
        int level = Random.Range(zone.minLevel, zone.maxLevel + 1);
        int groupId = zone.factionGroupId != 0 ? zone.factionGroupId : Random.Range(1000, 9999);

        var ai = Instantiate(entry.prefab, position, Quaternion.identity);
        var combatant = ai.Combatant;

        if (combatant == null)
        {
            Debug.LogWarning($"Enemy prefab '{entry.prefab.name}' needs OrganismCombatant", entry.prefab);
            Destroy(ai.gameObject);
            return;
        }

        combatant.ConfigureEnemy(level, EnemyGenerator.Generate(evolution, zone.dangerModificator), groupId);
        ai.Initialize(player, zone.guardHome, zone.transform.position, zone.homeRadius);

        var spawned = new SpawnedEnemy { ai = ai, combatant = combatant, zone = zone };
        combatant.OnDamagedBy += attacker => spawned.lastAttacker = attacker;
        combatant.OnDied += () => OnEnemyDied(spawned);

        spawnedEnemies.Add(spawned);
        zone.Register();
    }

    private void OnEnemyDied(SpawnedEnemy spawned)
    {
        if (!spawnedEnemies.Remove(spawned))
            return;

        var zone = spawned.zone;
        if (zone == null)
            return;

        zone.Unregister(died: true);

        if (!zone.countKills)
            return;

        bool killedByPlayer = playerCombatant != null && spawned.lastAttacker == playerCombatant;
        if (zone.killCountMode == EnemySpawnZone.KillCountMode.PlayerOnly && !killedByPlayer)
            return;

        if (zone.AddKill() && playerProgression != null)
            playerProgression.SetAchievementFlag(zone.killRewardFlag);
    }

    private void CullFarEnemies(float despawnDistance)
    {
        Vector2 playerPosition = player.position;

        for (int index = spawnedEnemies.Count - 1; index >= 0; index--)
        {
            var spawned = spawnedEnemies[index];

            // Destroyed outside of Die(), e.g. by another script
            if (spawned.ai == null)
            {
                spawnedEnemies.RemoveAt(index);
                if (spawned.zone != null)
                    spawned.zone.Unregister(died: false);
                continue;
            }

            if (Vector2.Distance(playerPosition, spawned.ai.transform.position) <= despawnDistance)
                continue;

            spawnedEnemies.RemoveAt(index);
            if (spawned.zone != null)
                spawned.zone.Unregister(died: false);

            Destroy(spawned.ai.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Vector3 center = player.position;

        Gizmos.color = new Color(0.3f, 1f, 0.3f, 1f);
        Gizmos.DrawWireSphere(center, MinSpawnDistance);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, MaxSpawnDistance);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, DespawnDistance);
    }
}
