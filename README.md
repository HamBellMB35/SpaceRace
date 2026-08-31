# SpaceRace

SpaceRace is a 3D endless-runner built in Unity. You pilot a ship through a
procedurally-assembled starfield, dodge obstacles and enemy fighters, fire
lasers at threats, and rack up score for as long as you survive - on PC
(keyboard, mouse, or a gamepad) or on Android, with on-screen touch controls.

## Gameplay

- **Movement** - the ship moves sideways and vertically within a bounded
  play area, banking into turns, while scrolling forward automatically.
  Controls read from keyboard, gamepad, or an on-screen touch joystick via
  Unity's Input System, so the same code path drives every platform.
- **Combat** - a limited supply of lasers can be fired at enemies; ammo is
  tracked and shown in the HUD, and flying through certain barriers grants
  bonus ammo.
- **Lives & respawn** - the player has 3 lives. Getting caught outside the
  track boundary for too long (rather than an instant obstacle collision)
  costs a life instead of ending the run outright: the ship resets to the
  track's center, a "Press Any Key to Continue" screen and countdown play,
  and a brief invulnerability window covers the moment control is handed
  back. The very start of a run also grants a short invulnerability window,
  and nothing is allowed to spawn within a radius of the ship at that
  moment, so you're never blindsided in the first second of play.
- **Procedural world** - map chunks, obstacles, and enemy ships are pooled
  and spawned as the run progresses, with spawns skipped if they'd land too
  close to the player.
- **Feedback** - camera shake (via Cinemachine Impulse) and layered
  explosion sounds play when something is destroyed, obstacles play a
  random "woosh" as they pass close to the ship, and soft screen-edge
  glows plus a precise on-screen meter show how centered you are in the
  track.
- **Scoring** - passing through score triggers increases your score, shown
  live during the run and again on the game-over screen.
- **Game over / restart** - running out of lives shows the final score
  screen; pressing any key (or tapping, on mobile) returns to the main
  menu, where "New Game" starts a fresh run.

## Platforms

- **PC** - keyboard/mouse or a gamepad. Runs at a fixed 60fps target.
- **Android** - on-screen joystick and fire button (only shown on an actual
  mobile build or in Unity's Device Simulator - hidden everywhere else),
  screen locked to landscape, and an input response curve that makes small
  joystick nudges easier to control precisely without sacrificing full
  speed at a full push.

## Project structure

```
Assets/
  Scripts/
    Player/      Ship input, movement, and death handling
    Weapons/     Laser firing and barrier pickups
    Enemies/     Enemy movement and cleanup
    World/       Procedural world generation and track bounds/feedback
    Combat/      Collision-driven destruction, explosions, and impact audio
    UI/          HUD, lives display, and the game-over screen
    Managers/    Central game state, object pooling, and the respawn flow
    Platform/    Mobile vs. PC control visibility
  Scenes/        UI_v2.unity (main menu, loads first) and GamePlayScene.unity
  Prefabs/       Player, enemies, obstacles, map chunks, and UI prefabs
  Materials/     Materials used across the project
  Sounds/        Sound effects, music, and voice/beep clips
  Tests/         EditMode + PlayMode tests (Unity Test Framework)
  Archanor/      Third-party "Sci-Fi Arsenal" VFX asset pack
  StarSparrow/   Third-party ship model asset pack
  Unity UI Samples/ Third-party UI sample scripts/scenes
```

### Core scripts (`Assets/Scripts`)

| Script | Responsibility |
|---|---|
| **Player** | |
| `PlayerMovement` | Reads keyboard/gamepad/touch input and drives the ship via physics; applies an Android-only response curve to touch input |
| `PlayerDeath` | Spends a life when the player collides with an obstacle |
| `GyroMovement` | Optional device-gyroscope input for steering |
| `CrosshairFollow` | Keeps the aiming crosshair UI over the ship's current aim point |
| **Weapons** | |
| `LaserSpawner` | Fires laser projectiles, tracks ammo, plays fire/no-ammo sounds |
| `LaserBarrier` | Grants bonus laser ammo when the player flies through it |
| **Enemies** | |
| `Enemy` | Moves an enemy to random points within a bounding box while advancing forward |
| `EnemyForwardMove` | Moves an enemy forward at a constant speed |
| `EnemyDestroyer` | Cleans up (returns to pool) enemies/obstacles that scroll past the player unhit |
| **World** | |
| `SpaceChunksGenerator` | Continuously spawns pooled world segments ahead of the player |
| `Randomizer` | Spawns a random obstacle chunk and enemy ships on a timer, skipping spawns too close to the player |
| `DelimitersMovement` | Scrolls a lane-delimiter object toward the camera at a constant speed |
| `TrackBounds` | Single source of truth for the track's playable path boundaries |
| `TrackBoundsPenalty` | Tracks how long the player has been outside the track and spends a life after a grace period |
| `TrackEdgeVignette` | Soft screen-edge glows that fade in as the ship nears a track wall |
| `TrackPositionMeter` | Precise on-screen bars showing exact position within the track bounds |
| **Combat** | |
| `DeathByCollision` | Decides which trigger contacts count as a real "kill" (obstacles only die to the player/laser; lasers explode on almost anything else), then spawns an explosion and pools the object |
| `ExplosionImpulse` | Triggers camera shake and plays a random explosion sound whenever an explosion effect spawns |
| `ProximityWoosh` | Plays a random "woosh" sound as obstacles pass close to the player |
| `SelfDestruct` | Releases a pooled object back to its pool a fixed delay after it becomes active |
| **UI** | |
| `LivesDisplay` | Updates the three ship-life icons and flashes/plays a sound when a life is lost |
| `FinalScoreUI` | Game-over screen; waits for any input, then returns to the main menu |
| `ScoreUpdater` | Plays a sound and awards score when the player passes through a trigger |
| `SceneLoader` | UI hook for ending the current run from a button |
| **Managers** | |
| `GameManager` | Central coordinator for score, laser ammo, and lives; switches between gameplay and game-over UI; sets the 60fps target. The actual counting logic lives in the three plain classes below - GameManager just reacts to what they report and keeps the UI in sync |
| `ScoreTracker` | Plain C# class owning the running score total and the points-per-trigger rule |
| `LaserAmmoTracker` | Plain C# class owning the ammo count, spending one shot at a time (never below zero) and granting bonus ammo |
| `LivesTracker` | Plain C# class owning the lives count and reporting whether losing one just ended the run |
| `ObjectPoolManager` | Central object pool (built on Unity's own `ObjectPool<T>`) used for map chunks, obstacles, enemies, and explosions to avoid GC spikes |
| `RespawnSequence` | Drives the game-start and post-respawn "Press Any Key to Continue" + countdown flow, including temporary invulnerability |
| **Platform** | |
| `PlatformControlsVisibility` | Shows on-screen touch controls only on mobile (or while previewing one in Unity's Device Simulator) |

`TouchInputManager.cs` is a leftover tech-demo script (tap to recolor whatever's under your finger) that isn't wired into core gameplay - it's a candidate for deletion rather than something actively used.

## Requirements

- Unity **2022.2.12f1** (or a compatible 2022.2.x LTS-adjacent version) -
  see `ProjectSettings/ProjectVersion.txt`.

## Getting started

1. Clone the repository.
2. Open the project folder in Unity Hub (it will offer to install a
   matching editor version if you don't already have one).
3. Press Play from `Assets/Scenes/UI_v2.unity` (the main menu - this is
   also the scene Unity will boot into on a real build, per the Build
   Settings scene order).

## Controls

| Input | Action |
|---|---|
| WASD / arrow keys / gamepad left stick / on-screen joystick (Android) | Move the ship |
| Left mouse button / gamepad South button / on-screen fire button (Android) | Fire laser |
| Any key / tap | Continue past "Press Any Key to Continue," or return to the main menu after game over |

## Tests

`Assets/Tests` has two Unity Test Framework suites, both runnable from
**Window > General > Test Runner** in Unity:

- **EditMode** (`Assets/Tests/EditMode`) - fast, scene-free tests against
  the plain C# `ScoreTracker`, `LaserAmmoTracker`, and `LivesTracker`
  classes (score accumulation, ammo spending/clamping/bonuses, and the
  "did losing that life end the run" rule). These run instantly with no
  Play Mode required, since none of those classes touch a Unity scene.
- **PlayMode** (`Assets/Tests/PlayMode`) - a smaller set of tests that spin
  up a real `GameManager` component to confirm its public behavior (for
  example, that losing the last life correctly ends the run) still holds
  end-to-end, on top of the trackers above.

## Third-party assets

This project includes third-party asset packs for visual effects and ship
models, under `Assets/Archanor`, `Assets/StarSparrow`, and
`Assets/Unity UI Samples`. These retain their own original licenses from
their respective authors/Unity Asset Store listings and are not covered by
this repository's license.

## License

Original game code in this repository is licensed under the MIT License -
see [LICENSE](LICENSE). Third-party assets are excluded, per above.
