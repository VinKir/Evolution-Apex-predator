# Generated enemies: evolution path, rolled organs and mutations, zone danger

## Context
Right now, spawned enemies take their stats from the picked evolution only (the TODO in `OrganismProgression.InitializeRuntime`). Their organs come from the prefab's `OrganismBody`: `SimpleEnemy` has none, and `Scavanger` has no `OrganismBody` component at all.

Goal: each zone-spawned enemy is built from a random path through the evolution tree.
- Stats are the sum of every stage's bonuses on the path.
- Organs are the union of every stage's granted organs, plus a number of extra organs controlled by the zone's `dangerModificator`.
- Each part gets a random level and its free mutations.

Base prefabs stay empty: no body parts and zero characteristics.

## Decisions (agreed)
- Extra organs: for each stage on the path, roll `Random.Range(0, dangerModificator + 1)` organs from that stage's `purchasableOrgans`, skipping organs the enemy already has.
- Free mutations: every `EvolutionSO.mutations` entry on the path, plus one random unlocked variant for each milestone level (multiple of 5) that the part's rolled level reaches.
- Remove `OrganismProgression.startingEvolution`. The player and bosses are configured by hand in the Editor: `currentEvolution`, `level` and the four characteristics on `OrganismProgression`, plus `factionGroupId` on `OrganismCombatant`. Only zone-spawned enemies are initialized at runtime.
- `BossController` no longer configures the organism. Its `evolution`, `level` and `factionGroupId` fields and its `ConfigureEnemy` call are removed.
- `nextEvolutions` ↔ `previousEvolutions` sync both ways in the editor: adding a link on either side adds the mirror link, and removing one removes it.

## Changes

### 1. `EvolutionSO` (`Data/Evolution/EvolutionSO.cs`)
- Add `[Header("Previous evolutions")] public List<EvolutionSO> previousEvolutions = new();`. This is a new field, so existing assets keep their data.
- Add `#if UNITY_EDITOR OnValidate`, following the pattern in `BodyPartDefinitionSO.OnValidate`:
  - `[NonSerialized]` snapshots of both lists.
  - First call (snapshot null, i.e. on load or first selection): add-only backfill of the mirror links, then take the snapshot.
  - Later calls: diff against the snapshot.
    - Added to `nextEvolutions` → add `this` to `next.previousEvolutions`. Removed → remove it.
    - Added to `previousEvolutions` → add `this` to `previous.nextEvolutions`. Removed → remove it.
  - Ignore nulls and self. `EditorUtility.SetDirty` on every asset it changes, then update the snapshot.
  - Stale snapshots on the other asset stay safe: its next diff only re-adds or re-removes links that are already correct (no-op).

### 2. `EnemyGenerator` (new static class, `Controllers/AI/EnemyGenerator.cs`)
`Generate(EvolutionSO evolution, int dangerModificator)` returns a plain `EnemyBuild`: the path, the four summed characteristics, and a `List<OrganismBody.BodyPartRuntimeState>`. It has no component access.

1. **Path**: start at `evolution`. While `stage > 0`, pick a random entry from `previousEvolutions` with a lower `evolutionStage`. Guard against cycles with a visited `HashSet`. If no candidate is found, log a warning and stop, using the partial path.
2. **Stats**: sum `strengthExt/Int` and `enduranceExt/Int` bonuses over the whole path, including stage 0.
3. **Organs**: the distinct union of `organs` over the path. Then, for each stage, add up to `Random.Range(0, dangerModificator + 1)` organs shuffled from its `purchasableOrgans`, skipping duplicates by `partId`.
4. **Levels**: `max = evolution.LevelCap`, the same value as `MutationCap` after spawn, and `band = max - 5`.
   - If `band <= 0`, roll `Random.Range(0, max + 1)`.
   - Otherwise, a 50% chance of `Random.Range(band, max + 1)`, else `Random.Range(0, band)`.
5. **Granted mutations**: for each `mutations` entry on the path whose `bodyPart.partId` the enemy has, add an `AppliedVariant { milestoneLevel = unlockLevel }`. Skip it if that milestone is already filled. This mirrors `EvolutionUIController.ApplyEvolutionMutations`.
6. **Milestone mutations**: for each `milestone = 5, 10, … <= level` that is not yet filled, pick a random entry from `organ.GetVariantsForLevel(milestone)` that isn't already applied. This mirrors `MutationUIController.BuildMilestoneRequests`.

### 3. `OrganismBody` (`Core/OrganismBody.cs`)
- Add `ReplaceParts(List<BodyPartRuntimeState> parts)`. It rebuilds `partDefinitions` from the parts' organs, sets `states`, and raises `OnBodyChanged` once.

### 4. `OrganismProgression` (`Controllers/Organism/OrganismProgression.cs`)
- Remove the `startingEvolution` field and `Awake` (which only copied its bonuses). Serialized characteristics are now used as they are.
- Replace `InitializeRuntime(level, evolution)` with a single `InitializeRuntime(int initialLevel, EvolutionSO evolution, int strengthExt, int strengthInt, int enduranceExt, int enduranceInt)`. It is used only by generated enemies; the level is still clamped to `LevelCap`. Remove the TODO.

### 5. `OrganismCombatant` (`Controllers/Organism/OrganismCombatant.cs`)
- Change `ConfigureEnemy` to `ConfigureEnemy(int level, EnemyBuild build, int groupId)`. The build carries the evolution. It initializes progression with the summed stats, calls `body.ReplaceParts(build.parts)` (with a warning if `body` is null), then calls `RecalculateStats()`.
- Drop the `?? gameObject.AddComponent<OrganismProgression>()` fallback. Progression without an evolution is now meaningless, so log a warning and return instead.

### 5a. `BossController` (`Controllers/AI/BossController.cs`)
- Remove `evolution`, `level`, `factionGroupId`, the `ConfigureEnemy` call and the "no evolution" warning. Everything else (dormant/leash/reward) stays.
- The header comment says the organism is configured on its own components in the Editor.

### 6. `EnemySpawnZone` / `EnemySpawner` / editor
- `EnemySpawnZone`: under a `[Header("Danger")]`, add `[Tooltip(...)] [Min(0)] public int dangerModificator = 0;`. Its meaning: 0..N extra organs per evolution stage, from that stage's `purchasableOrgans`.
- `EnemySpawner.SpawnEnemy`: call `EnemyGenerator.Generate(evolution, zone.dangerModificator)` and pass the result to the new `ConfigureEnemy` overload.
- `EnemySpawnZoneEditor.DrawWarnings`: change the missing-`OrganismBody` message from Info to Warning, since generated organs need it.

### 7. Prefabs and docs
- `Scavanger.prefab`: add an empty `OrganismBody` component in the Editor, not by hand-editing the YAML. `SimpleEnemy` already has an empty one.
- Both prefabs already have all four characteristics set to 0. The removed `startingEvolution` key disappears from the prefab and scene YAML on their next save.
- Player in `StartZone`: set the four characteristics to 10 (the BaseEvolution bonuses that `Awake` used to apply). `currentEvolution` is already BaseEvolution. Do this in the Inspector, or as a targeted edit of those 4 lines in the player's `OrganismProgression` block.
- Update `.claude/rules/enemy-ai.md` (generation flow, `dangerModificator`, the boss is configured by hand) and `evolution-ui.md` (`previousEvolutions` and its auto-sync). Update `organism-combat.md` if it mentions starting stats.

## Verification (in the Unity Editor)
1. Select `BaseEvolution` once. `Larva` and `Regenerat Larva 1` should now list it under `previousEvolutions`. Remove `Larva` from `BaseEvolution.nextEvolutions`: `Larva.previousEvolutions` loses it. Add it back: the link is restored.
2. Open `StartZone`, set a zone to stage-1 evolutions with `dangerModificator = 1`, and enter Play mode. On a spawned enemy, check:
   - Its characteristics equal the BaseEvolution bonuses plus its stage-1 bonuses (e.g. Larva → 15 each).
   - Its `OrganismBody` has the base organs, the stage-1 organs, and sometimes the purchasable regen gland.
   - Part levels are in 0..10, and parts at 5 or above carry milestone variants.
3. Set `dangerModificator = 0`: no purchasable organs appear.
4. In Play mode, the player has 10 in each characteristic and the same HP and stamina as before the change.
5. A boss placed by hand (enemy prefab + `BossController`, with its stats set in the Inspector) keeps those stats, wakes up, leashes and grants its flag.

## Status
- Done: sections 1–6. Player characteristics set to 10 in `StartZone.unity`. Rules docs updated (`organism-combat.md` needed no change).
- Remaining (in the Unity Editor): add an empty `OrganismBody` to `Scavanger.prefab`, then run the verification steps.
