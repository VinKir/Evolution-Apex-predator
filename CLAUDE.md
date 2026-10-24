# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Unity **6000.5.8f1** 2D game (URP 2D, new Input System, Cinemachine 3, TMP). Gameplay scene: `Assets/Scenes/StartZone.unity`.

- All code is in `Assets/Scripts/` in the default `Assembly-CSharp`. There are no asmdefs, namespaces or tests.
- There's no CLI build, lint or test pipeline; work is checked in the Unity Editor. `Assets/PlayerControls.cs` is generated from `PlayerControls.inputactions`, so don't edit it by hand.
- Use the Unity 6 APIs already in the code (`FindObjectsByType<T>()` without a sort mode, `EntityId`/`GetEntityId()`).
- Renaming a `[SerializeField]` field, an SO class or an enum value breaks scenes, prefabs and assets. Use `[FormerlySerializedAs]` and append new enum values at the end. Keep `.meta` files with their assets.

Per-area architecture notes are in `.claude/rules/` and load automatically when matching files are touched.
