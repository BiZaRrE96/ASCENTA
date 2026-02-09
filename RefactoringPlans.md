# Refactoring and Optimization Plan for Assets/Scripts

Date: 2026-02-09

## Scope
This plan is based on a review of scripts in `Assets/Scripts`, including core gameplay systems (movement, time/undo, save/load, bootstrap/config, events) and non-core additions (UI, audio, animations, effectors, debug).

## Current Condition Summary

### Strengths
- Clear separation by folders (`Movement`, `SaveLoad`, `Time`, `Events`, `UI`, `Audio`, etc.).
- Lightweight event bus reduces direct dependencies between features.
- Many systems already use `SerializeField` references + defensive null checks.
- Movement code is reasonably modular (controller vs. state vs. ground check).

### Coupling and Flow Issues
- **Implicit singleton/service discovery**: Multiple systems use `FindFirstObjectByType` or `FindObjectsByType` on enable/start, which creates hidden dependencies and runtime cost. Examples: `ConfigWorker`, `SaveLoadUI`, `MovementSettingsUpdater`, `NewgameCutscene`, `LastJumpTracker`.
- **Runtime object scanning** in hot paths or frequent events: `ConfigWorker.ApplyMouseSensitivity`, `MovementSettingsUpdater.ApplySensitivity`, and some UI state updates scan the scene each time.
- **EventBus is global with weak typing boundaries**: Event usage is consistent, but no scoping or ownership; any system can publish/subscribe, making flow harder to trace.
- **Bootstrap responsibilities are broad**: It performs config init and scene loading but also assumes presence of bootstrap-only objects without a formal contract (see `Assets/Scripts/Bootstrap/AGENTS.md`).
- **Save/load lifecycle is spread** across `DataPersistenceManager`, UI helpers, scene loader, and event requests. Some parts are synchronous, others async, and there’s no unified state machine for transitions.
- **Cutscene uses data persistence directly** (`NewgameCutscene` reads and writes intro flag), which couples gameplay flow to persistence internals.
- **Time/Undo coupling**: `TimeController` listens to undo events and drives reversal timing; undo logic manipulates movement + physics directly. This is functional but tightly connected.
- **UI components use `BroadcastMessage`** for hover/click propagation, which is flexible but opaque and slow for large hierarchies.

### Readability Opportunities
- Several scripts combine orchestration with device logic (e.g., `SettingsWorker` mixes UI interactions, config manipulation, and event emission).
- Some scripts have inline calculations that would benefit from utility extraction (e.g., JumpPad targeting math).
- Mixed casing / method naming (`rotateLook`, `showTutorial`) reduces consistency.

## Refactoring Goals
1. Cleaner logic flow with explicit ownership and ordering.
2. Decouple core systems (movement/time/save/config) from non-core presentation (UI/audio/visuals).
3. Reduce hidden dependencies and runtime object lookups.
4. Improve testability and traceability of game state transitions.

## Refactoring Plan

### Phase 1: Establish Core Service Boundaries
- Create a lightweight **GameServices** or **ServiceRegistry** that is initialized by Bootstrap.
- Register explicit services: `SceneLoad`, `ConfigWorker`, `TimeController`, `DataPersistenceManager`.
- Replace `FindFirstObjectByType` in core systems with injected or registered services.
- Define interfaces for core services where needed (e.g., `ISceneLoader`, `IConfigService`, `ISaveService`, `ITimeService`).

Benefit: Core systems can depend on interfaces rather than scene scanning; startup ordering becomes explicit.

### Phase 2: Centralize Game Flow Orchestration
- Create a **GameFlowController** (or similar) that owns transitions: boot -> menu -> load/new -> gameplay -> cutscene.
- Move logic currently spread across `SaveLoadUI`, `SaveLoadHelper`, `NewgameCutscene`, and `DataPersistenceManager` into a clear flow with state transitions.
- Convert `GameSceneReadyEmitter` into a flow signal owned by the flow controller.

Benefit: Single source of truth for scene loads and save/load lifecycle.

### Phase 3: Decouple Persistence from Gameplay Features
- Move `introPlayed` handling into a **Progress/SaveState** service so cutscenes don’t reach into `DataPersistenceManager.CurrentData` directly.
- Provide a small API like `IProgressService.HasSeenIntro` and `IProgressService.SetSeenIntro()`.
- Keep `DataPersistenceManager` focused on save/load mechanics rather than gameplay flags.

Benefit: Cutscene logic stays focused on sequencing; persistence stays focused on data.

### Phase 4: Reduce Runtime Scans and Implicit Dependencies
- Replace `FindObjectsByType` for settings propagation with either:
  - a `MovementRegistry` that registers active controllers, or
  - event-driven updates where `MovementController` subscribes to a `SettingsChangedEvent` directly.
- Cache references in systems that update frequently (`ConfigWorker.ApplyMouseSensitivity`, `MovementSettingsUpdater`).
- Avoid `BroadcastMessage` in UI hover; replace with explicit interfaces or UnityEvents on the master.

Benefit: Predictable updates and lower per-frame or per-event cost.

### Phase 5: Clarify Event Ownership and Scope
- Group events by domain (`MovementEvents`, `UIEvents`, `SaveEvents`) and limit which systems should publish them.
- Introduce a naming convention for bus usage (e.g., only core systems publish state events, UI only publishes request events).
- Consider replacing global `EventBus` usage in certain features with direct references when ownership is clear (e.g., `ButtonHoverMaster` -> slaves).

Benefit: Easier reasoning about what triggers what.

## Optimization Opportunities (Non-Behavioral)
- **Reduce allocations** in `EventBus.Publish` by reusing handler lists or using a copy-on-write strategy.
- **Avoid `FindObjectsByType` in gameplay loops**; cache once or maintain registries.
- **Avoid repeated FMOD warning logs** by consolidating checks in a base audio component.
- **MovingPlatform**: consider precomputing valid point indices to avoid repeated list scans on every move state calculation.
- **UndoProcess**: the ground check could be cached or performed with a precomputed `RaycastHit` if used repeatedly.

## Recommended Re-architecture Map
- **Core systems**: Bootstrap, Config, Time/Undo, Movement, Save/Load, Scene loading.
- **Non-core systems**: UI, Audio, Animations, Visual effectors, Debug.
- **Data**: `ConfigData`, `GameData`, `PlayerData`, `MovingPlatformData`.

Move toward:
- `Core/Services/` for service interfaces + registry.
- `Core/Flow/` for game state transitions.
- `Gameplay/` for movement, platforms, jump, dash, undo.
- `Presentation/` for UI/audio/visual effects.

## Concrete Next Steps
1. Define a `GameServices` registry and register `SceneLoad`, `ConfigWorker`, `TimeController`, `DataPersistenceManager` in Bootstrap.
1. Update `SaveLoadUI`, `NewgameCutscene`, `MovementSettingsUpdater`, `LastJumpTracker` to use services rather than scene searches.
1. Implement `IProgressService` to manage intro flags and other progression state.
1. Replace `BroadcastMessage` usage with explicit `UnityEvent` or `IButtonHoverListener` on `ButtonHoverMaster`.
1. Establish a `MovementControllerRegistry` or event subscription model for settings propagation.

## Risks and Considerations
- Service registration introduces ordering constraints; ensure Bootstrap loads first and survives scene changes.
- Refactoring event flows may change timing; add transitional logs or debug visualizers.
- Keep save format stable (`GameData.version`) and plan for migration if data fields move.

## Notes from `Assets/Scripts/Bootstrap/AGENTS.md`
- Bootstrap is expected to load first in a special `bootstrap` scene and ensure Config is applied before loading the next scene. This design can be formalized by using a dedicated flow controller and explicit service registration.
