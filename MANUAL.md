# RogueBit Developer Manual

This manual describes the structure, algorithms, and extension points of RogueBit.

## Folder Structure

- `src/RogueBit/`
  - `Program.cs` – App entry, SadConsole bootstrap, seed parsing.
  - `Game.cs` – Core game state, main loop coordination, turn order.
  - `Map/`
    - `DungeonGenerator.cs` – Drunkard-walk dungeon generation with connectivity pass.
    - `MapManager.cs` – Map data (walkability/transparency), FOV, A*, helpers.
  - `Entities/`
    - `Entity.cs` – Base entity with common properties.
    - `Player.cs` – Player-specific state (Coins).
    - `Enemy.cs` – Goblin implementation.
    - `Item.cs` – Items and factories for Coins/Hearts (ASCII fallback for ♥).
  - `Systems/`
    - `InputSystem.cs` – Key handling (WASD/arrows, R, Q).
    - `RenderSystem.cs` – Draws map, entities, fog-of-war, HUD, death screen.
    - `CombatSystem.cs` – Simple bump-combat resolution.
    - `SaveSystem.cs` – JSON high score with cross-platform data path.

## Build & Run

- Build: `dotnet build`
- Run: `dotnet run --project src/RogueBit -- --seed 12345`
- Or from `src/RogueBit`: `dotnet run -- --seed 12345`

## Algorithms

- Dungeon Generation: Drunkard walk starting from the map center, targeting ~50% floor coverage. A post-pass ensures connectivity via BFS from start; any isolated regions are joined by carving straight corridors.
- Field of View: Ray-based visibility that behaves like shadowcasting for small radii. It traces Bresenham rays from the player to tiles within radius, marking visible until the first opaque tile.
- Pathfinding: GoRogue `AStar` with a cost map (1 for floors, `double.MaxValue` for walls) and Manhattan distance for 4-directional movement.

## Game Loop

1. Render current state.
2. Input: one player move (or action) at a time.
3. Update: enemies act once per player action (chase or wander).
4. Recompute FOV, redraw.
5. On death: show death screen and wait for `R`/`Q`.

## Adding New Enemies

1. Create a new class in `Entities/` deriving from `Entity` or `Enemy` and set `Glyph`, `Color`, `HP`, `MaxHP`, and behavior parameters.
2. In `Game.PlaceEntities`, spawn instances at random walkable positions.
3. Extend `EnemiesAct` in `Game.cs` with your AI behavior:
   - To chase: use `Map.GetNextStepToward(enemy.Pos, Player.Pos)` and move if unblocked.
   - To perform ranged attacks: check line-of-sight using `Map.IsTransparent` along a Bresenham line.
4. Ensure rendering respects visibility; enemies draw only when in FOV.

## Adding New Items

1. Extend `ItemType` in `Entities/Item.cs` and add a factory method similar to `Item.Coin`/`Item.Heart`.
2. Implement pickup effects in `Game.PickupItemsAt`.
3. Place items in `Game.PlaceEntities` with desired counts/densities.

## Tunable Constants

- Map size: `GameConstants.MapWidth`, `GameConstants.MapHeight` (map reserves the last screen row for HUD).
- FOV radius: `GameConstants.FovRadius`.
- Enemy aggro radius: `GameConstants.EnemyAggroRadius`.
- Densities for enemies/items: tweak counts in `Game.PlaceEntities`.

## RNG Seeding & Repro

Pass `--seed <int>` to fix the RNG seed for a run. The same seed reproduces dungeon topology, entity placements, and enemy wander randomness, making it easier to reproduce and debug issues.

## SadConsole Notes

- Rendering: We render directly to `SadConsole.Console` using `SetGlyph` and `Print`.
- Colors: Prefer `SadRogue.Primitives.Color` constructors for custom shades.
- Window sizing: Resize is disabled; the screen is fixed at `80x26`.
- Unicode glyphs: Some fonts may not render `♥`. An ASCII fallback (`h`) activates when `USE_ASCII=1`.

## Tips

- If you change map size or layout, keep the last screen row free for the HUD to avoid entities drawing over it.
- When adding new effects or systems, keep them stateless where possible and route state through `Game`.

