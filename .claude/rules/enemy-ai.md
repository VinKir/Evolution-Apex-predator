---
paths:
  - "Assets/Scripts/Controllers/AI/**"
  - "Assets/Scripts/Managers/EnemySpawner.cs"
  - "Assets/Scripts/Data/EnemySpawnPoint.cs"
  - "Assets/Scripts/Data/EnemyTemplateSO.cs"
---

# Enemy AI and spawning

- `EnemySpawner` spawns roaming scavengers and predators around the player, and guardians at `EnemySpawnPoint`s with `isBase`. It calls `OrganismCombatant.ConfigureEnemy(template, level, evolution, groupId)` and then `EnemyAIController.Initialize(player, basePoint)`, and culls enemies beyond `despawnDistance`.
- Every `thinkInterval`, `EnemyAIController` runs `AIContext.UpdateFrom` (perception), then `OrganismBrain.DecideState` (a decision tree per `OrganismBehaviourType` using `AINeeds`), then `AIStateMachine.ChangeState`.
- All states are in `AIStates.cs`; to add one, also add an `AIStateType` value and a case in `AIStateMachine.CreateState`. `AIMemory` tracks the last attacker.
- Attack and eat ranges scale with `Stats.sizeMultiplier`.
