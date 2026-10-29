# Zone-based enemy spawning and bosses

## Context
`EnemySpawner` currently spawns roaming enemies anywhere around the player, plus base guardians at `EnemySpawnPoint`. Level is an offset from the player's level, and every enemy shares one temporary `EvolutionSO`. Despawning already works: `CullFarEnemies` destroys enemies beyond `despawnDistance`.
Goal:
- Enemies exist only in spawn zones that the designer places and sizes in the Scene view. Each zone has an absolute level range and weighted evolutions, spawns only while the player is near, and keeps the distance despawn.
- Zones tie into the achievement flags: a zone can be shut off by flags and can grant a flag after a number of kills.
- Bosses are a separate script on hand-placed enemies.

## Decisions (agreed)
- Zones **replace** roaming spawn and `EnemySpawnPoint`. A zone option `guardHome` reproduces the old guardian behaviour.
- Shape: **Circle or Box** on one component, edited with Scene-view handles and gizmos.
- Level: **absolute min–max per zone**, clamped to the chosen evolution's cap `(stage+1)*5` (the same clamp as `OrganismProgression.InitializeRuntime`).
- Evolution: **weighted entries**. Each entry is a template plus a weight plus a weighted list of `EvolutionSO`.
- Lifetime: a spawned enemy is removed **only** when it dies or is farther than `despawnDistance` from the player. Leaving its zone never destroys it. It counts toward its zone's `maxAlive` (by registration, not position) until it dies or despawns.
- Walls: physics will **not** fix an enemy spawned inside a wall. The `Walls` `CompositeCollider2D` in StartZone uses Geometry Type = Outlines (edges only), so the enemy stays trapped. The spawner rejects spawn points that overlap obstacles, which needs Walls switched to **Polygons** (see section 6).
- Zone shutdown: the zone has a list of `disableWhenFlags`. It stops spawning only when the player has **all** of them; an empty list means the zone never shuts down. Enemies already alive are not removed.
- Zone kill counter: optional, with a mode of **any death** or **player kills only** (the player dealt the last damage). Reaching `requiredKills` grants `killRewardFlag` once. The zone keeps spawning until all of its `disableWhenFlags` are present; the reward flag counts only if it's in that list.
- Bosses: not spawned by zones. A hand-placed enemy with its own `BossController`:
  - stays dormant (stationary, no AI) until the player comes within its activation distance or damages it
  - when the player goes past its leash distance, it walks back to its anchor, goes dormant and **heals to full**
  - is never despawned by distance
  - on death, grants its `rewardFlag` and never returns this session
- Flags and kill counters live only for the session, because there is no save system.

## Changes

### 1. New `Assets/Scripts/Data/EnemySpawnZone.cs` (MonoBehaviour)
- Shape: `ZoneShape { Circle, Box }`, `radius`, `boxSize`. Methods `Contains(Vector2)`, `RandomPoint()`, `DistanceToEdge(Vector2)`.
- Activation: `activationDistance`, the player's distance to the zone edge at which the zone starts spawning.
- Population: `maxAlive`, `respawnDelay` (after a death), `spawnInterval` (gap between spawns while filling), `minDistanceFromPlayer` (no pop-in on screen).
- Placement: `spawnClearance` (default 1), the free radius a spawn point needs, checked against the spawner's `obstacleMask`.
- Level: `minLevel`, `maxLevel`.
- Faction: `factionGroupId` (0 gives each enemy a random group, as roaming enemies get now).
- AI: `guardHome` (enemies get the zone centre as home) and `homeRadius`.
- Flags:
  - `List<string> disableWhenFlags`
  - `bool countKills`
  - `KillCountMode killCountMode { AnyDeath, PlayerOnly }`
  - `int requiredKills`
  - `string killRewardFlag`
- `List<EnemySpawnEntry> entries`, both `[Serializable]` classes in the same file:
  - `EnemySpawnEntry { EnemyTemplateSO template; float weight = 1; List<EvolutionOption> evolutions; }`
  - `EvolutionOption { EvolutionSO evolution; float weight = 1; }`
- Runtime state on the zone: `aliveCount`, `nextSpawnTime`, `killCount`, `Register()`, `Unregister(died)`. `respawnDelay` applies only when the enemy died.
- `IsDisabledFor(OrganismProgression)`: `disableWhenFlags` is non-empty and the player has every flag in it.
- `OnDrawGizmos`: zone fill and outline, greyed out at runtime while disabled. `OnDrawGizmosSelected`: activation ring. `OnValidate`: clamp values and set `maxLevel >= minLevel`.

### 2. New `Assets/Scripts/Editor/EnemySpawnZoneEditor.cs`
- Wrap the file in `#if UNITY_EDITOR`, following `Controllers/Player/PlayerProgressionEditor.cs`.
- `OnSceneGUI`: a radius handle for Circle and a `BoxBoundsHandle` for Box, with Undo.
- Inspector: hide the kill-counter fields unless `countKills` is on. Show warnings for:
  - an entry with no template, prefab or evolutions
  - `maxLevel` above an evolution's level cap
  - `countKills` turned on with an empty `killRewardFlag`
  - `activationDistance` + zone size too large compared with the spawner's `despawnDistance` (spawn/despawn loop)
- In play mode, show `alive / maxAlive`, `killCount / requiredKills`, and whether the zone is disabled.

### 3. Rework `Assets/Scripts/Managers/EnemySpawner.cs`
- Remove the template lists, the roaming fields, `spawnPoints`, the Temp `evolution` field, `roamingTemplates()` and `PlayerEvo()`.
- Keep `player`, `despawnDistance` and `[SerializeField] tickInterval = 0.5f`. Add `[SerializeField] LayerMask obstacleMask`. Cache the player's `OrganismProgression` and `OrganismCombatant`. Find all zones once in `Start` with `FindObjectsByType<EnemySpawnZone>()`.
- Track each spawned enemy in a list of records `{ enemy, zone, lastAttacker }`. Bosses are never in this list.
- Each tick:
  - **Cull**: iterate that list instead of calling `FindObjectsByType` every frame. Destroy enemies beyond `despawnDistance` and call `zone.Unregister(died: false)`, so the slot frees immediately.
  - **Spawn**: for each zone that passes all of these checks:
    - `!zone.IsDisabledFor(progression)`
    - within its activation distance
    - `aliveCount < maxAlive` and `Time.time >= nextSpawnTime`

    Try up to N random points inside the zone. A point is valid when it is at least `minDistanceFromPlayer` from the player and `Physics2D.OverlapCircle(point, spawnClearance, obstacleMask)` returns nothing. Spawn at the first valid point. If none is found, retry on the next tick.
- `SpawnEnemy(zone)`:
  1. Pick an entry by weight, then an evolution by weight.
  2. Set level = `Random.Range(min, max + 1)`, clamped to `[1, (stage+1)*5]`.
  3. Instantiate, then call `ConfigureEnemy(template, level, evolution, groupId)` and `ai.Initialize(player, zone.guardHome, zone.transform.position, zone.homeRadius)`.
  4. Subscribe to `combatant.OnDamagedBy`, which stores `lastAttacker`.
  5. Subscribe to `combatant.OnDied`, which:
     - calls `zone.Unregister(died: true)`
     - if `countKills` is on and the death counts for the zone's mode, increments `killCount`
     - when `killCount >= requiredKills`, calls `progression.SetAchievementFlag(killRewardFlag)`. `SetAchievementFlag` already ignores flags the player has.
- Skip entries with a null evolution and log a warning once, because `InitializeRuntime` throws on a null evolution.

### 4. New `Assets/Scripts/Controllers/AI/BossController.cs`
- Goes on a hand-placed enemy GameObject that has `OrganismCombatant`, `EnemyAIController` and `OrganismMovementMotor`.
- Inspector fields:
  - setup: `template`, `level`, `evolution`, `factionGroupId`
  - distances: `activationDistance`, `leashDistance` (measured from the anchor), `homeRadius`
  - reward: `rewardFlag`
- Finds the player with `FindFirstObjectByType<PlayerMovementController>()`.
- `Start`:
  - destroy itself if the player already has `rewardFlag` (covers a scene reload within the session)
  - store `anchor` = its start position
  - call `ConfigureEnemy`
  - stay **Dormant**: the AI isn't initialized, so `EnemyAIController.Update` returns early and the boss stands still
- States:
  - **Dormant → Active**: when the player is within `activationDistance`, or on `OnDamagedBy`. Calls `ai.Initialize(player, true, anchor, homeRadius)`.
  - **Active → Returning**: when the player is farther than `leashDistance` from the anchor. Sets `ai.ForceReturnHome = true`.
  - **Returning → Dormant**: once within `homeRadius` of the anchor. Calls `ai.Deactivate()`, sets `combatant.RestoreFull()` and `ForceReturnHome = false`.
- `OnDied`: `progression.SetAchievementFlag(rewardFlag)`. The GameObject is already destroyed by `Die()`, so it can't return this session.
- Gizmos: activation and leash circles around the anchor.

### 5. AI home and boss support
- `EnemyAIController`:
  - change the signature to `Initialize(Transform player, bool hasHome, Vector2 homePosition, float homeRadius)` and replace `BasePoint` with `HasHome`, `HomePosition` and `HomeRadius`
  - move the `OnDamagedBy` subscription from `Initialize` to `Awake`, so a boss that reactivates doesn't subscribe twice
  - add `Deactivate()`, which sets player = null, calls `movement.Stop()` and clears memory
  - add `ForceReturnHome { get; set; }`
  - drop `IsRoamingEnemy`, since the spawner no longer needs it
- `AIContext`:
  - replace `HomePoint` with `HasHome` and `HomePosition`
  - take `HomeRadius` from the controller instead of `activationRadius * 0.35f`
  - add `ForceReturnHome`
- `OrganismBrain.DecideState`: right after the `Dead` check, `if (context.ForceReturnHome) return ReturnHome;`. Replace each `HomePoint != null` with `HasHome`.
- `AIStates.cs` (`PatrolState`, `ReturnHomeState`): use `HasHome` and `HomePosition` instead of `HomePoint.transform.position`.
- `OrganismCombatant.RestoreFull()`: a new public method that sets every HP pool and stamina to max and clears bleed and disabled parts. Anchor the edit to a new method next to `HealBody`.
- Delete `EnemySpawnPoint.cs` and its `.meta`.

### 6. Scene and docs
- Manual editor steps for the user:
  - set `Walls` → `CompositeCollider2D` → Geometry Type to **Polygons**
  - ideally, move `Walls` to its own `Walls` layer and use that layer for `obstacleMask`. It is on `Default` now, together with the enemies.
- `StartZone.unity` contains `EnemySpawnPoint` objects. After the script is deleted they show as missing scripts, and the user replaces them with zones. The `EnemySpawner` loses its old serialized fields, which is harmless.
- Update `.claude/rules/enemy-ai.md`:
  - replace the spawner bullet with zones, bosses and flags
  - update the paths: replace `EnemySpawnPoint.cs` with `EnemySpawnZone.cs`, and add `Editor/EnemySpawnZoneEditor.cs`

### 7. Camera-based spawn and despawn distances (follow-up)
Why: the camera's ortho size is `baseOrthographicSize * player sizeMultiplier` (`PlayerMovementController.UpdateCameraSize`). With a fixed per-zone `minDistanceFromPlayer` and a fixed `despawnDistance`, enemies pop in on screen or get culled on screen once the player grows.
Decision: `EnemySpawner` computes both distances in one place from the real camera:
- `MinSpawnDistance = cam.orthographicSize * sqrt(1 + cam.aspect²) + offscreenMargin`, the view's half-diagonal plus a margin
- `DespawnDistance = MinSpawnDistance + despawnBuffer`

Changes:
- `EnemySpawnZone`: remove `minDistanceFromPlayer`. The serialized data is simply dropped.
- `EnemySpawner`:
  - remove the serialized `despawnDistance`
  - add `[SerializeField] Camera viewCamera` (falls back to `Camera.main`), `offscreenMargin` (default 2), and `despawnBuffer` (default 15)
  - expose `MinSpawnDistance` and `DespawnDistance` as read-only properties, recomputed at the start of each tick. They work in edit mode too, for the inspector warning.
  - `TryFindSpawnPoint` uses `MinSpawnDistance` instead of `zone.minDistanceFromPlayer`. Cull and the max-distance check use `DespawnDistance`.
- Docs: update `.claude/rules/enemy-ai.md`, which mentions `despawnDistance`.

#### 7.1 Spawn only where the zone overlaps the player's spawn ring
Why: in a zone bigger than the despawn radius, a random point in a far corner gets culled at once. The current reject-only check also wastes most attempts there, so a large zone barely spawns.
Decision:
- The spawn region is **zone ∩ ring(MinSpawnDistance, MaxSpawnDistance)** around the player.
- `MaxSpawnDistance = MinSpawnDistance + despawnBuffer * 0.5`. This leaves half the buffer as slack, so an enemy that takes a step outward isn't culled right away.
- `activationDistance` becomes redundant: a zone is active exactly when that region is non-empty.

Changes:
- `EnemySpawnZone`:
  - remove `activationDistance`
  - add `Rect Bounds` (the AABB of the circle or box)
  - replace `RandomPoint()` with `bool TryGetRandomPoint(Vector2 center, float minDistance, float maxDistance, out Vector2 point)`. It intersects `Bounds` with the square `center ± maxDistance`, returns false when that is empty, and otherwise samples uniformly in the intersection and accepts a point only if `Contains(point)` and its distance from `center` is within `[minDistance, maxDistance]`.
  - keep `DistanceToEdge`; the spawner uses it as a cheap reject. Remove `RandomPoint()` and `OuterRadius`.
- `EnemySpawner.TrySpawnInZones`:
  - skip the zone when `zone.DistanceToEdge(player) > MaxSpawnDistance` (a cheap reject)
  - `TryFindSpawnPoint` calls `zone.TryGetRandomPoint(player, MinSpawnDistance, MaxSpawnDistance, …)` up to `spawnPointAttempts` times, still applying the obstacle clearance check
- `EnemySpawnZoneEditor`:
  - remove the despawn/zone-size warning, since an instant despawn can no longer happen
  - stop drawing the `activationDistance` ring in `OnDrawGizmosSelected`
- `EnemySpawner.OnDrawGizmosSelected`: draw MinSpawn, MaxSpawn and Despawn circles around the player.

## Status
- Done in code: sections 1–5, plus the rules doc from section 6.
  - `RestoreFull` cancels running bleeds through a `bleedGeneration` counter.
  - The spawner also rejects points farther than `despawnDistance` from the player.
  - Code uses `FindAnyObjectByType` for the player and the spawner.
- Remaining, manual in the editor:
  - set Walls to Polygons and give it a layer, then assign `obstacleMask`
  - replace the 1 `EnemySpawnPoint` in StartZone, now a missing script, with zones
  - set up a boss prefab
  - run the verification below
- Not yet compiled in Unity.
- Sections 7 and 7.1 (camera-based distances, spawning only inside the ring) are done in code and not compiled yet. `spawnPointAttempts` default is 12.

## Out of scope / known limits
- An enemy's stats come only from its own evolution's bonuses (existing TODO in `OrganismProgression.InitializeRuntime`).
- Body parts still come from `EnemyTemplateSO.bodyParts`.
- Flags and kill counters aren't saved between sessions.
- A boss that dies from `accidentalDeathChance` still grants its flag.
- `EnemyTemplateSO` level and evolution offsets become unused. They stay in place so assets keep their data.

## Verification (Unity Editor)
1. Zones: place a Circle zone and a Box zone and resize them with handles. Gizmos appear, and the inspector shows warnings for an empty entry, a level above the cap, and kill counting without a reward flag.
2. Spawning:
   - Nothing spawns until the zone overlaps the player's spawn ring. The zone then fills to `maxAlive`, one enemy per `spawnInterval`, never on screen and never inside a wall.
   - In a zone much larger than the despawn radius, stand in one corner: enemies appear only within the ring near the player, none get culled right after spawning, and the zone fills at the normal rate.
3. Lifetime:
   - Lure an enemy out of its zone while staying near it: the enemy survives and the zone doesn't replace it.
   - Walk past `despawnDistance`: the enemies are destroyed. Return: the zone refills with no delay. A killed enemy is replaced only after `respawnDelay`.
4. Kill counter: set `requiredKills = 3`, `killRewardFlag = "zone_a_cleared"` and `disableWhenFlags = ["zone_a_cleared", "x"]`.
   - In `PlayerOnly` mode, enemies killing each other don't count.
   - After 3 player kills the flag appears and the zone keeps spawning. Add `"x"` in the player's inspector: the zone stops.
5. Boss: place a prefab with `BossController`.
   - It stands still until the player approaches or hits it.
   - Lead it past the leash: it walks home, stops and heals to full.
   - Kill it: `rewardFlag` appears on the player and the boss is gone for the session.
6. Camera distances: select the spawner in play mode. The min-spawn circle sits just outside the screen corners. Raise the player's `StrengthExt` so it grows: both circles grow with the camera, and enemies still appear off screen.
7. Check enemy levels and evolutions against the zone settings, and check that `guardHome` enemies patrol around the zone centre.
