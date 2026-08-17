# SpaceRace

SpaceRace is a 3D endless-runner built in Unity. You pilot a ship through a
procedurally-assembled starfield, dodge obstacles and enemy fighters, fire
lasers at threats, and rack up score for as long as you survive.

## Gameplay

- **Movement** — the ship moves sideways and vertically within a bounded
  play area, banking into turns, while scrolling forward automatically.
- **Combat** — a limited supply of lasers can be fired at enemies; ammo is
  tracked and shown in the HUD.
- **Procedural world** — map chunks, obstacles, and enemy ships are spawned
  and randomized as the run progresses.
- **Scoring** — passing through score triggers increases your score, shown
  live during the run and again on the game-over screen.
- **Game over / restart** — colliding with an obstacle ends the run and
  shows the final score; pressing any key reloads the game.

## Project structure

```
Assets/
  Scripts/        Game code (see below)
  Scenes/          GamePlayScene.unity — the main gameplay scene
  Prefabs/         Player, enemies, obstacles, UI, and effect prefabs
  Materials/       Materials used across the project
  Sounds/          Sound effects and audio clips
  Archanor/        Third-party "Sci-Fi Arsenal" VFX asset pack
  StarSparrow/     Third-party ship model asset pack
  Unity UI Samples/ Third-party UI sample scripts/scenes
```

### Core scripts (`Assets/Scripts`)

| Script | Responsibility |
|---|---|
| `GameManager` | Tracks score/laser count, toggles gameplay vs. game-over UI |
| `PlayerMovement` | Reads input and drives the player ship via physics |
| `PlayerDeath` | Detects obstacle collisions and ends the run |
| `GyroMovement` | Optional device-gyroscope input for the player ship |
| `LaserSpawner` | Fires laser projectiles and tracks ammo |
| `Enemy` | Enemy ship movement pattern (wander + advance) |
| `Randomizer` | Spawns a random obstacle chunk and enemy ships on a timer |
| `SpaceChunksGenerator` | Continuously spawns world segments ahead of the player |
| `ScoreUpdater` | Awards score and plays a sound when the player crosses a trigger |
| `EnemyDestroyer` / `DeathByCollision` / `SelfDestruct` / `EnemyForwardMove` / `DelimitersMovement` | Supporting collision, cleanup, and movement behaviors |
| `TouchInputManager` | Mobile touch input handling |
| `SceneLoader` / `FinalScoreUI` | Scene transitions and the game-over screen |

## Requirements

- Unity **2022.2.12f1** (or a compatible 2022.2.x LTS-adjacent version) —
  see `ProjectSettings/ProjectVersion.txt`.

## Getting started

1. Clone the repository.
2. Open the project folder in Unity Hub (it will offer to install a
   matching editor version if you don't already have one).
3. Open `Assets/Scenes/GamePlayScene.unity`.
4. Press Play.

## Controls

| Input | Action |
|---|---|
| Horizontal / Vertical axes (arrow keys / WASD / gamepad) | Move the ship |
| Left mouse button | Fire laser |
| Any key | Restart after game over |
| Touch (mobile) | Tap to interact |

## Third-party assets

This project includes third-party asset packs for visual effects and ship
models, under `Assets/Archanor`, `Assets/StarSparrow`, and
`Assets/Unity UI Samples`. These retain their own original licenses from
their respective authors/Unity Asset Store listings and are not covered by
this repository's license.

## License

Original game code in this repository is licensed under the MIT License —
see [LICENSE](LICENSE). Third-party assets are excluded, per above.
