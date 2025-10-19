# RogueBit

A retro ASCII roguelike dungeon crawler in your terminal.

RogueBit is a small, turn-based roguelike built with .NET 8, SadConsole for terminal-style rendering, and GoRogue for roguelike utilities like pathfinding. Explore a procedurally generated dungeon, fight goblins, collect coins and hearts, and chase a new high score.

## Installation

- Prerequisites: .NET SDK 8.0+

Build:

```
dotnet build
```

Run (with deterministic seed):

```
dotnet run --project src/RogueBit -- --seed 12345
```

Run from project directory:

```
cd src/RogueBit
dotnet run -- --seed 12345
```

## Controls

- Movement: Arrow Keys or WASD
- R: Restart
- Q: Quit

## Features

- Procedural dungeon generation (drunkard walk) with good coverage and full connectivity.
- Shadowcasting-style field-of-view with fog-of-war (remembered tiles).
- Turn-based: every player move advances all enemies.
- Enemies: goblins (g) chase with A* if within aggro radius, otherwise wander.
- Pickups: hearts (♥) restore health; coins ($) increase score.
- Bump combat: walk into enemies to damage them; enemies damage you if they bump you.
- HUD: bottom row shows HP, Coins, Depth, and Seed.
- Deterministic runs: `--seed <int>` reproduces maps and placements.
- High score is persisted to a JSON file.

## Example Screenshot

```
################################################################################
#..............g.............#............#...................$...............#
#.....####.....#######.......#.......g....#.....#######.......#....#######....#
#.....#..#...........#.......#............#.....#.....#.......#....#.....#....#
#..@..#..#....$......#....♥..#....$.......#.....#..g..#....$..#....#..$..#....#
#.....####...........#.......#............#.....#######.......#....#######....#
#............................#..................$.............#...............#
################################################################################
 HP: 10/10   Coins: 3   Depth: 1   Seed: 12345
```

## Troubleshooting

- Unicode heart fallback: If the `♥` glyph doesn’t render in your terminal or appears as a missing character, set the environment variable `USE_ASCII=1` before running to use an ASCII fallback:

  - Windows (PowerShell): `setx USE_ASCII 1`
  - macOS/Linux (bash/zsh): `export USE_ASCII=1`

- Terminal colors/font size: If colors look off or text is too small, try resizing the window or adjusting zoom settings. SadConsole uses a pixel font under the hood; window scaling may vary by platform.

## High Score File Location

- Windows: `%AppData%/RogueBit/highscore.json`
- Linux/macOS: `$XDG_DATA_HOME/RogueBit/highscore.json` or `~/.local/share/RogueBit/highscore.json` if `XDG_DATA_HOME` is not set.
