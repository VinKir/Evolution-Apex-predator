# Remove EnemyTemplateSO, prefab-only enemies, PlayerBody → OrganismBody

## Context
`EnemyTemplateSO` is mostly dead weight:
- Used fields: only `prefab` (by the spawner and the zone editor) and `bodyParts` (by `OrganismCombatant.BuildStats`). Both template assets serialize no body parts, so enemies currently have none.
- Unused fields: all level/evolution offsets, base stat ranges, `corpseBiomassMultiplier` (the combatant has its own) and `corpseConsumeDuration` (`Die()` hard-codes `10f`). The assets also carry a stale `behaviorType`, which now lives on `EnemyAIController`.
- Prefab analysis (`SimpleEnemy` = predator, `Scavanger` = scavenger): both are full, separate prefabs with Combatant, Progression, AI, Motor and the hitbox children. Both have `OrganismCombatant.enemyTemplate` pointing at `predator.asset`, which is wrong for the scavenger and overwritten at spawn anyway. Each also has a stale `isPlayer`.
- Today, creating an enemy means two assets (SO + prefab), a circular reference between them, and a runtime check for components.

Goal: one enemy type = one prefab. Organs live on an `OrganismBody` component, the same as on the player. Zones and bosses reference the prefab directly.

## Decisions (agreed)
- Delete `EnemyTemplateSO` and its two assets (`Assets/Data/Enemies/predator.asset`, `scavender.asset`).
- Rename `PlayerBody` to `OrganismBody` and use it on enemies. All organ and mutation access goes through `OrganismBody`.

## Changes

### 1. Rename PlayerBody → OrganismBody
- Rename `Assets/Scripts/Core/PlayerBody.cs` to `OrganismBody.cs`, together with its `.meta`, so the GUID is kept and scene/prefab script references survive. Use `git mv` for both, then rename the class. The nested `BodyPartRuntimeState` and `AppliedVariant` stay as they are.
- Update the type references in `BodyVisualController`, `EvolutionUIController`, `MutationUIController`, `OrganismStatsCalculator` (`OrganismBody.BodyPartRuntimeState`), `EvolutionSO.CanBeSelected`, and the requirement SOs (`EvolutionRequirementSO`, `AchievementRequirementSO`, `AllBodyPartsMaxedRequirementSO`, `MutationRequirementSO`, `StatThresholdRequirementSO`). These are type-only changes; the field names (`body`) stay.

### 2. OrganismCombatant (`Controllers/Organism/OrganismCombatant.cs`)
- Remove `isPlayer` and the `[Header("Enemy Source")] enemyTemplate` field.
- Change `[SerializeField] private PlayerBody playerBody` to `[FormerlySerializedAs("playerBody")] [SerializeField] private OrganismBody body`. In `Awake`, fall back to `GetComponent<OrganismBody>()`.
- `HookSources`/`UnhookSources`: subscribe to `body.OnBodyChanged` whenever `body != null`, with no `isPlayer` check.
- `BuildStats`: `OrganismStatsCalculator.Calculate(progression, body?.States, EvolutionStage)`. This removes the branch and the TODO.
- `ConfigureEnemy(EnemyTemplateSO, level, evolution, groupId)` becomes `ConfigureEnemy(int level, EvolutionSO evolution, int groupId)`.
- Add `[SerializeField] private float corpseConsumeDuration = 10f;` under `Death Drop` and use it in `Die()` instead of `10f`. Its default keeps the current behaviour.

### 3. EnemySpawnZone entries (`Data/EnemySpawnZone.cs`)
- `EnemySpawnEntry.template` (`EnemyTemplateSO`) becomes `public EnemyAIController prefab;`. Typing it as a component means the object picker only accepts valid enemy prefabs.
- `PickEntry`: weight is non-zero only when `prefab != null`.
- Scene migration: the type changes, so `FormerlySerializedAs` can't carry the value. `StartZone.unity` has exactly one entry (line ~700421). Rewrite it as `- prefab: {fileID: 943026238583717204, guid: be2d76008845246e6aa027550cf0f2d4, type: 3}`, which is the `EnemyAIController` on `SimpleEnemy`, the prefab the old predator template pointed to. The other 5 zones have `entries: []`.

### 4. EnemySpawner (`Managers/EnemySpawner.cs`)
- `Instantiate(entry.prefab, position, Quaternion.identity)` returns `EnemyAIController`. Get the combatant through `ai.Combatant`.
- The "needs OrganismCombatant and EnemyAIController" check shrinks to a null check on `Combatant`. Keep the `Destroy` and warning for that case.
- Remove the duplicated level-cap clamp. `OrganismProgression.InitializeRuntime` already clamps, so only `Mathf.Max(1, …)` stays.
- Update the warning text to `entry.prefab.name`.

### 5. Level cap in one place
- Add `public int LevelCap => (evolutionStage + 1) * 5;` to `EvolutionSO`.
- Use it in `OrganismProgression.LevelCap` and in `InitializeRuntime`, and in `EnemySpawnZoneEditor` (the max-level info box).

### 6. BossController (`Controllers/AI/BossController.cs`)
- Remove the `template` field and call `combatant.ConfigureEnemy(level, evolution, factionGroupId)`. Organs come from the boss prefab's `OrganismBody`.

### 7. EnemySpawnZoneEditor (`Editor/EnemySpawnZoneEditor.cs`)
- "Template not set" becomes "Prefab not set". Drop the "template has no prefab" warning.
- Add a warning when the prefab has no `OrganismBody` (informational only: the enemy will have no organs).

### 8. Delete
- `Assets/Scripts/Data/EnemyTemplateSO.cs` and `.meta`, `Assets/Data/Enemies/predator.asset` and `.meta`, `scavender.asset` and `.meta`. Leave the `Assets/Data/Enemies/` folder; the user can decide about it.

### 9. Manual editor steps (user, in the Unity Editor)
- Add `OrganismBody` to `SimpleEnemy` and `Scavanger` and set `partDefinitions`/levels (they currently have no organs, so nothing is lost).
- Optional: make `Scavanger` a Prefab Variant of `SimpleEnemy` (or extract an `Enemy_Base`), so new species override only the sprites, `behaviourType`, AI tuning and organs.
- Re-save both prefabs to drop the stale serialized fields (`isPlayer`, `enemyTemplate`, `attackWindup`/`attackActiveTime`/`attackCooldown`).

### 10. Docs
- `.claude/rules/organism-combat.md`: rename to `OrganismBody`, and say that enemies carry `OrganismBody` with no `isPlayer` branch.
- `.claude/rules/evolution-ui.md`: rename `PlayerBody.ApplyMutations`.
- `.claude/rules/enemy-ai.md`: the new `ConfigureEnemy` signature, entries referencing an `EnemyAIController` prefab, and a short section on creating an enemy:
  1. Make a Prefab Variant of an existing enemy.
  2. Set `behaviourType`/AI tuning on `EnemyAIController`, and organs on `OrganismBody`.
  3. Add it to a zone entry with its evolutions, or put a `BossController` on it for a hand-placed boss.

## Verification
- After the renames, `Grep` for `PlayerBody|EnemyTemplateSO|enemyTemplate|isPlayer` under `Assets/Scripts` should return nothing.
- `git diff --stat` should touch only the listed files. Check that the `OrganismBody.cs.meta` GUID is unchanged.
- In the Unity Editor: no compile errors; the player's mutation/evolution UI still works (the body reference is kept through the GUID); the `StartZone` lair zone entry shows `SimpleEnemy`; enemies spawn, fight, die and drop corpses; the kill counter sets `lair_00_cleared`.
- After adding `OrganismBody` with an organ to an enemy prefab, its stats change (for example, a higher max HP in the status bar).

## Status
- Code steps 1–8 and 10 are done. The empty `Assets/Data/Enemies/` folder and its `.meta` were also removed. Stale `isPlayer`/`enemyTemplate` lines were stripped from both enemy prefabs.
- Remaining: step 9 (manual editor work) and checking in the Unity Editor.
