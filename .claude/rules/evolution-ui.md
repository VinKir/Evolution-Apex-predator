---
paths:
  - "Assets/Scripts/Data/Evolution/**"
  - "Assets/Scripts/Controllers/Visual/**"
  - "Assets/Scripts/Controllers/Logic/**"
  - "Assets/Scripts/UI/**"
  - "Assets/Scripts/Managers/OrganismStatusBarManager.cs"
---

# Evolution, mutations and UI

- `EvolutionSO` nodes form a tree through `nextEvolutions`. Each node has stage stat bonuses, granted organs and mutations, `purchasableOrgans` bought with evolution points, and `EvolutionRequirementSO` conditions. To add a requirement type, subclass the abstract SO (`IsMet` / `GetDescription`).
- `EvolutionUIController` handles picking an evolution, buying stats and organs, and the timed evolve, which ends in `OrganismProgression.ApplyEvolution`. That call resets the level to 1 and raises `OnEvolve`.
- `MutationUIController` handles leveling body parts and the milestone-variant choice queue, which ends in `PlayerBody.ApplyMutations`.
- Both controllers lock the player through `PlayerActionLock`. Pausing goes through the static `TimeController`.
- UI scripts in `UI/` are thin row and popup components (`*RowUI`, `*PopupUI`) that the controllers populate.
- World-space status bars: `OrganismStatusBarManager` subscribes to the `OrganismCombatantRegistry.Instance` singleton and takes bars from `OrganismStatusBarPool`.
