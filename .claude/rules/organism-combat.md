---
paths:
  - "Assets/Scripts/Controllers/Organism/**"
  - "Assets/Scripts/Controllers/Player/**"
  - "Assets/Scripts/Core/**"
  - "Assets/Scripts/Data/OrganismTypes.cs"
  - "Assets/Scripts/Data/Organs/**"
---

# Organisms, combat and stats

## Organism components
The player and enemies share the same components on one GameObject:
- **`OrganismProgression`**: level/XP, the current `EvolutionSO` (`LevelCap = (stage+1)*5`), the four characteristics (`StrengthExt/Int`, `EnduranceExt/Int`), evolution points, biomass and achievement flags. It raises `OnEvolve`.
- **`OrganismCombatant`** (the largest file): HP pools for chitin, body, jaws and left/right legs, plus stamina. It also handles attack windup, active and cooldown timing, damage through `AttackPacket`, bleed, lifesteal, reflect, regen, part disable/regrow, death and corpse drop, and `IsFriendlyTo`. `RecalculateStats()` rebuilds `Stats`, keeps each HP pool at the same ratio of its new max, and sets `transform.localScale` from `sizeMultiplier`.
- **`OrganismMovementMotor`**: `Rigidbody2D` movement. The player drives it through `PlayerMovementController` and enemies through the AI states.
- **`AttackHitbox`** (attack trigger) and **`BodyHitbox`** (hurtboxes per `BodyHitboxSlot` on child colliders). The slot that gets hit decides which HP pool takes the damage.
- `PlayerActionLock` blocks movement and menus while the player is eating or mutating.

## Stat pipeline
`OrganismStatsCalculator.Calculate(progression, bodyParts, evolutionStage)` is the only place `OrganismRuntimeStats` is derived:
1. Base formulas use the four characteristics and the `CombatSettings` constants (`Data/OrganismTypes.cs`).
2. Bonuses are summed from each body part's `BodyPartDefinitionSO.modifiers` and its applied `BodyPartVariantSO.modifiers`. Each modifier adds `value + perLevel*partLevel + perEvolutionStage*stage`.

**To add a stat**, change four places: the `BodyStatType` enum (append at the end), a `CombatBonusAccumulator` field, a `case` in `AddModifier`, and an `OrganismRuntimeStats` field plus its formula in `Calculate`. If combat uses the stat, also update `AttackPacket` and `BuildAttackPacket`.

## Body parts
- `OrganismBody` holds `BodyPartRuntimeState`s (organ, level, applied variants keyed by milestone level) and raises `OnBodyChanged`, which triggers a stat recalculation.
- Every organism, player or enemy, takes its organs and mutations only from its own `OrganismBody` component. `OrganismCombatant.body` falls back to `GetComponent`. An organism without one has no organs.
- `partId` identifies an organ. `BodyPartDefinitionSO.OnValidate` sets the back-reference on its variants automatically.
