using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnZone : MonoBehaviour
{
    public enum ZoneShape
    {
        Circle,
        Box
    }

    public enum KillCountMode
    {
        AnyDeath,
        PlayerOnly
    }

    [Serializable]
    public class EvolutionOption
    {
        public EvolutionSO evolution;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public class EnemySpawnEntry
    {
        public EnemyAIController prefab;
        [Min(0f)] public float weight = 1f;
        public List<EvolutionOption> evolutions = new();

        public EvolutionSO PickEvolution()
        {
            var option = PickWeighted(evolutions, o => o.evolution != null ? o.weight : 0f);
            return option?.evolution;
        }
    }

    [Header("Shape")]
    [Tooltip("Ось-ориентированная форма в мировых координатах, поворот и масштаб объекта не учитываются")]
    public ZoneShape shape = ZoneShape.Circle;
    [Min(0.1f)] public float radius = 6f;
    public Vector2 boxSize = new(12f, 8f);

    [Header("Population")]
    [Min(0)] public int maxAlive = 4;
    [Tooltip("Задержка перед заменой погибшего врага. Деспавн по расстоянию освобождает место сразу")]
    [Min(0f)] public float respawnDelay = 30f;
    [Tooltip("Пауза между спавнами, пока зона заполняется")]
    [Min(0f)] public float spawnInterval = 1.5f;
    [Tooltip("Свободный радиус вокруг точки спавна (проверяется по obstacleMask спавнера)")]
    [Min(0f)] public float spawnClearance = 1f;

    [Header("Level")]
    [Min(1)] public int minLevel = 1;
    [Min(1)] public int maxLevel = 3;

    [Header("Danger")]
    [Tooltip("За каждую стадию эволюции враг получает от 0 до N дополнительных органов из purchasableOrgans этой стадии")]
    [Min(0)] public int dangerModificator = 0;

    [Header("Faction")]
    [Tooltip("0 = каждый враг в своей группе")]
    public int factionGroupId = 0;

    [Header("AI")]
    [Tooltip("Враги считают центр зоны домом: патрулируют и возвращаются")]
    public bool guardHome = false;
    [Min(0.1f)] public float homeRadius = 4f;

    [Header("Flags")]
    [Tooltip("Зона перестаёт спавнить, когда у игрока есть ВСЕ эти флаги. Пусто = спавнит всегда")]
    public List<string> disableWhenFlags = new();
    public bool countKills = false;
    public KillCountMode killCountMode = KillCountMode.PlayerOnly;
    [Min(1)] public int requiredKills = 10;
    public string killRewardFlag;

    [Header("Enemies")]
    public List<EnemySpawnEntry> entries = new();

    public int AliveCount { get; private set; }
    public int KillCount { get; private set; }
    public float NextSpawnTime { get; private set; }

    // Set by EnemySpawner every tick, used only for gizmos and the inspector
    public bool IsDisabled { get; set; }

    public bool IsDisabledFor(OrganismProgression progression)
    {
        if (disableWhenFlags == null || disableWhenFlags.Count == 0)
            return false;

        if (progression == null)
            return false;

        foreach (var flag in disableWhenFlags)
        {
            if (!progression.HasAchievementFlag(flag))
                return false;
        }

        return true;
    }

    public bool CanSpawnNow => AliveCount < maxAlive && Time.time >= NextSpawnTime;

    public void Register()
    {
        AliveCount++;
        NextSpawnTime = Time.time + spawnInterval;
    }

    public void Unregister(bool died)
    {
        AliveCount = Mathf.Max(0, AliveCount - 1);

        if (died)
            NextSpawnTime = Mathf.Max(NextSpawnTime, Time.time + respawnDelay);
    }

    // Returns true when this kill reaches requiredKills
    public bool AddKill()
    {
        KillCount++;
        return KillCount >= requiredKills;
    }

    public EnemySpawnEntry PickEntry()
    {
        return PickWeighted(entries, e => e.prefab != null ? e.weight : 0f);
    }

    public bool Contains(Vector2 point)
    {
        Vector2 center = transform.position;

        if (shape == ZoneShape.Circle)
            return (point - center).sqrMagnitude <= radius * radius;

        Vector2 half = boxSize * 0.5f;
        return Mathf.Abs(point.x - center.x) <= half.x && Mathf.Abs(point.y - center.y) <= half.y;
    }

    public Rect Bounds
    {
        get
        {
            Vector2 size = shape == ZoneShape.Circle ? new Vector2(radius * 2f, radius * 2f) : boxSize;
            return new Rect((Vector2)transform.position - size * 0.5f, size);
        }
    }

    // Samples a point inside both the zone and the ring [minDistance, maxDistance] around ringCenter.
    // Sampling is limited to the overlap of the zone bounds and the ring's bounding square, so far parts of a big zone cost nothing.
    public bool TryGetRandomPoint(Vector2 ringCenter, float minDistance, float maxDistance, out Vector2 point)
    {
        point = default;

        Rect zoneBounds = Bounds;
        float xMin = Mathf.Max(zoneBounds.xMin, ringCenter.x - maxDistance);
        float xMax = Mathf.Min(zoneBounds.xMax, ringCenter.x + maxDistance);
        float yMin = Mathf.Max(zoneBounds.yMin, ringCenter.y - maxDistance);
        float yMax = Mathf.Min(zoneBounds.yMax, ringCenter.y + maxDistance);

        if (xMin >= xMax || yMin >= yMax)
            return false;

        Vector2 candidate = new(UnityEngine.Random.Range(xMin, xMax), UnityEngine.Random.Range(yMin, yMax));
        if (!Contains(candidate))
            return false;

        float sqrDistance = (candidate - ringCenter).sqrMagnitude;
        if (sqrDistance < minDistance * minDistance || sqrDistance > maxDistance * maxDistance)
            return false;

        point = candidate;
        return true;
    }

    public float DistanceToEdge(Vector2 point)
    {
        Vector2 center = transform.position;

        if (shape == ZoneShape.Circle)
            return Mathf.Max(0f, Vector2.Distance(point, center) - radius);

        Vector2 half = boxSize * 0.5f;
        float outsideX = Mathf.Max(0f, Mathf.Abs(point.x - center.x) - half.x);
        float outsideY = Mathf.Max(0f, Mathf.Abs(point.y - center.y) - half.y);
        return new Vector2(outsideX, outsideY).magnitude;
    }

    private static T PickWeighted<T>(List<T> items, Func<T, float> getWeight) where T : class
    {
        if (items == null || items.Count == 0)
            return null;

        float total = 0f;
        foreach (var item in items)
        {
            if (item != null)
                total += Mathf.Max(0f, getWeight(item));
        }

        if (total <= 0f)
            return null;

        float roll = UnityEngine.Random.value * total;
        T last = null;

        foreach (var item in items)
        {
            if (item == null)
                continue;

            float weight = Mathf.Max(0f, getWeight(item));
            if (weight <= 0f)
                continue;

            last = item;
            roll -= weight;
            if (roll <= 0f)
                return item;
        }

        return last;
    }

    private void OnValidate()
    {
        boxSize = new Vector2(Mathf.Max(0.1f, boxSize.x), Mathf.Max(0.1f, boxSize.y));
        maxLevel = Mathf.Max(maxLevel, minLevel);
    }

    private void OnDrawGizmos()
    {
        Color color = Application.isPlaying && IsDisabled
            ? new Color(0.5f, 0.5f, 0.5f, 1f)
            : new Color(1f, 0.35f, 0.2f, 1f);

        Vector3 center = transform.position;

        if (shape == ZoneShape.Circle)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(center, radius);
            return;
        }

        Vector3 size = new(boxSize.x, boxSize.y, 0f);

        Gizmos.color = new Color(color.r, color.g, color.b, 0.12f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = color;
        Gizmos.DrawWireCube(center, size);
    }

    private void OnDrawGizmosSelected()
    {
        if (guardHome)
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, homeRadius);
        }
    }
}
