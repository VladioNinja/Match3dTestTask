# Match3D Test Task

A small Unity prototype inspired by Match 3D collection gameplay. The player drags physics-based items around the scene and drops them into the collector shaft. Collected items trigger feedback, update the counter, and the session ends when all spawned items are collected.

## Unity Version

- Unity 2022.3.62f3

## How to Run

1. Open the project in Unity Hub with Unity 2022.3.62f3 or a compatible Unity 2022.3 LTS version.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play.

## Controls

- Left mouse button: pick up and drag an item.
- Release left mouse button: drop the item.
- Restart button: reload the current session after game over.

## Implemented Features

- Random item spawning inside a configurable spawn area.
- Mouse-driven drag controller with camera raycasts and drag bounds.
- Rigidbody-based item movement with tuned mass, drag, collision materials, and velocity limits.
- Collector shaft that accepts dropped items only after release.
- Remaining-item counter and elapsed-time display.
- Game-over panel with final completion time.
- Collection feedback with flying star animation and audio.
- Collision audio with impulse threshold and cooldown to avoid excessive sound spam.

## Notes

- Item physics and collision sound settings are stored in `CollectableItemSettings` ScriptableObjects.
- The project keeps gameplay scripts under the `Match3d` namespace.
- Collected items are disabled by default after feedback so they no longer affect physics.
