# Architecture and algorithms

This file says how RogueBit is put together and why each algorithm was chosen.
It is written for somebody who wants to change the game and needs to know what
will break.

## The one rule

`RogueBit.Core` takes no rendering dependency. Not SadConsole, not MonoGame, not
a colour type.

Three things follow from that, and all three are the reason for the rule.

1. **A run is reproducible.** Every dice roll comes from one `SeededRandom`, and
   that object is built from the seed. Nothing else in the core calls `Random`.
2. **The game is testable with no display.** The whole of a run can be played out
   in a test. The screenshot in the README was captured that way.
3. **The frontend cannot drift.** It holds no rules, so there is no second copy
   of a rule to fall out of step with the first.

If a change needs the core to know something about drawing, the change belongs
in the frontend.

## Determinism

`SeededRandom` is PCG-XSH-RR, written out in this repository rather than taken
from `System.Random`. Two reasons, both of which bite this particular game.

1. **`System.Random` does not promise the same sequence from the same seed
   across runtime versions**, and its algorithm has already changed once. A
   seed that replays a different dungeon next year is not a seed, and every
   screenshot and shared seed would quietly come to mean something else.
2. **Its state cannot be read out**, so a run could not be saved and resumed
   without changing what the dice would do next.

`SeededRandomTests.TheSequenceForASeedIsPinned` holds the output for seed 12345
to fixed numbers. If that test fails, the change is breaking and needs a new
major version, not a new set of expected values.

`Restart()` returns a fresh source on the same seed. It exists because of a
specific defect: the version before this one kept a single generator alive
across restarts, so pressing `R` carried on drawing from a generator that had
already produced a dungeon.

`Snapshot()` and `Restore()` hand over the two state words, which is what lets a
saved run carry on rather than start again.

## Moving in eight directions

A diagonal step costs the same as a straight one, so Chebyshev distance is both
the true distance and the estimate A* uses. Manhattan was right while the
walker moved four ways and overstates the cost at once on eight, and an
estimate that overstates gives a short walk rather than a shortest one. That is
not theoretical: left on Manhattan, roughly one generated floor in six gets a
longer walk than breadth first finds, by as much as seven steps.
`MatchesBreadthFirstOnEveryFloorItIsGiven` compares the two over eighty floors.

A diagonal between two walls that meet at a corner is refused. Brushing one
wall is allowed, because forbidding that makes diagonals useless indoors. The
rule sits on the map rather than in the player or in the pathfinder, because
those two have to agree: a monster that could cut a corner the player could not
would be a monster that catches somebody who did everything right.

## Floors that stay put

A floor is built once and then kept. `Run` holds every floor it has been on,
by depth, and a `Floor` is the ground, the monsters and the items together
rather than three lists that have to be kept in step by hand.

This is what makes the stairs up worth having. Building the floor above again
would put its monsters back, move the potion that was walked past, and make
retreating free, because the ground behind would be new ground. Taking one out
of store also draws nothing from the generator, which is what keeps a run that
walks up and down equal to a run that walked straight down.
`GoingBackToAFloorDrawsNothingFromTheDice` compares the state of the dice
across a round trip and says so.

Every floor below the first has stairs up on the cell the player arrives on.
The generator is not told which depth it is building: it reports where the
entrance is, and the run writes the tile, the same way it fills in the stairs
down on the bottom floor. Coming up puts the player on the stairs down of the
floor above, so climbing twice means crossing a floor in between.

The depth bonus is paid on the deepest floor reached rather than the floor
being stood on. The two were the same number while the dungeon only went down.

## Saving

A save stores every floor as tiles, not as the seed it grew from. Regenerating
would be smaller, but it would tie every save to the exact behaviour of the
generator, and a save that stops loading because somebody tuned a generator was
never really written down. Three kilobytes a floor buys independence from that.

All of the floors are written, not only the one being stood on, because the
player can climb back into any of them. Each carries where the player came in,
which is where its stairs up are.

The file is written beside the target and then moved over it, so a crash midway
leaves the previous save whole rather than half of a new one. A load refuses a
version it does not know, a damaged file, a floor whose size does not match its
own dimensions, and a save that names a depth it holds no floor for, because a
save that cannot be trusted should not be played.

A run that ends deletes its save. Otherwise loading is a way to undo dying.

## Effects

The core reports `TurnEvent` values: what happened, on which cell, how big it
was, and whether the player was on the receiving end. It has no idea what any of
that looks like.

Everything visual lives in the frontend. A particle fades by moving toward the
background colour rather than by alpha, because a console cell has nothing to be
transparent against. The screen shakes only for a blow the player took, because
shaking for every hit anywhere makes the whole game feel loose. `--no-effects`
turns all of it off and changes nothing about how the game plays.

The event list is cleared when an action is committed, not when one is
requested. A refused move leaves the previous turn's events alone, because
nothing has happened to replace them.

`RunTests.TheSameSeedPlaysOutIdenticallyForAWholeRun` plays four hundred turns
twice and compares the floor, the score, the health and the turn count.

## Dungeon generation

Both generators satisfy one contract, checked over forty seeds in
`DungeonGeneratorContractTests`:

- exactly one walkable region, so no floor can strand the player
- a wall all the way round the edge
- an entrance and stairs, both on walkable ground
- the entrance and the stairs never on the same cell, or the stairs up written
  where the player arrives would take the stairs down away
- the same floor from the same seed, and a different floor from a different one

Doors go in afterwards, on floors of rooms. A doorway is a walkable cell that
belongs to no room, has exactly one walkable neighbour that does, and exactly
one that does not: the throat of a corridor where it reaches a wall. That is
the only place narrow enough for a door to be worth anything and the only place
one can stand without cutting an open space off from itself.

Traps go in last, on plain ground that nothing else took, so one never hides
under the stairs or under the cell the player arrives on.

### Binary space partition, odd floors

The floor is cut in two over and over until each block is too small to cut
again. Each leaf gets a room, and **each split joins the rooms of its two halves
before it returns**.

That last point is the whole design. Joining at every split makes the result
connected by construction. The common alternative, carving all the rooms and
then joining them in a list, leaves the connectivity to a second pass that has
to be right on its own.

### Drunkard walk, even floors

A digger walks at random and carves whatever it stands on, until it has carved
the coverage asked for.

When the digger reaches the edge it **jumps back to a cell it has already
carved** rather than being clamped against the wall. Clamping was what the old
code did, and it presses the cave flat along the edges, because a digger that
keeps trying to leave spends all its time on the boundary.

A cave has no rooms, so the stairs go at the walkable cell furthest from the
entrance, found by breadth-first search. That is what makes the floor worth
crossing.

### Connectivity

`MapRegions.Find` groups walkable cells into regions by cardinal steps.
`ConnectAll` repeatedly joins the two largest regions with an L shaped corridor
until one is left.

Cardinal and not diagonal is deliberate: the player moves in four directions, so
two floors touching only at a corner are genuinely two regions, and
`DoesNotJoinTwoFloorsThatOnlyTouchAtACorner` says so.

## Field of view

Recursive shadowcasting across eight octants. Each octant is scanned row by row
outwards, carrying the slopes of its left and right edge. A wall narrows that
wedge: the scan recurses into the part still lit and abandons the part behind
the wall.

**This is not what the previous version did.** That code cast a Bresenham ray
from the player at every cell within the radius and called it "shadowcasting
style". The two differ in ways that matter:

- Ray casting visits the cells near the viewer once per ray. Shadowcasting
  touches each cell at most once per octant.
- Ray casting leaves holes in a diagonal wall, because no ray happens to land on
  some of its cells.

One detail is worth knowing before you touch `Scan`. The scan runs outwards
along the **negative** axis, so `deltaY` is `-distance`. Getting that sign wrong
mirrors every octant and puts the shadows on the wrong side of their walls. It
was wrong in the first draft, and five tests caught it.

## Pathfinding

A\* with a Manhattan heuristic over four directions. Manhattan never overstates
the true cost on that grid, which is what keeps the result a shortest path
rather than merely a short one.

`Find` takes an optional blocking test as well as the map, because a monster has
to path round the other monsters and not only round the walls. **The goal is
exempt from that test.** Without the exemption a monster could never path onto
the player, because the player blocks its own cell, and nothing would ever reach
you.

## Turn order

One player action advances the whole world:

1. The player moves, attacks, waits, picks something up, uses something or
   descends.
2. Every living monster acts, a swift one twice.
3. Field of view is recomputed.
4. If the player is dead, the run ends.

A move into a wall is refused and **costs no turn**. A misread key should not
kill anybody.

## Combat

Damage is the attacker's power less the defender's defence, floored at nothing,
so armour can never turn an attack into healing.

Equipment bonuses are added by the actor, in `Player.Power` and
`Player.Defence`, rather than being held beside it. That is a correction of a
defect: the pack used to sit next to the player, so combat asked the actor for
its defence and never saw the armour, and every piece of armour in the game did
nothing. `EquipmentReachesCombatTests` pins the bonus to the number combat
actually reads.

## Testing

`MapBuilder` builds a map from ASCII inside a test, and renders one back so a
failure prints something readable.

Tests state the map they need rather than asking a generator for one. A
generator is a set of choices its author changes; a test that leans on one fails
the day a choice moves, for no reason connected to the thing under test.

The habit that matters: **when you add a test, break the thing it covers on
purpose and watch it fail.** That catches a test that asserts nothing and a test
that asserts the wrong thing, in one step.
