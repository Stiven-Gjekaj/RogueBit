# Milestones and roadmap

This is where RogueBit is today and where it is headed. It is kept out of the
README on purpose, so the front page stays short.

## v0.1: a game that runs

Everything before this point did not compile. The whole of it was rewritten.

- **A core that holds every rule and draws nothing.** A whole run can be played
  out in a test with no display attached.
- **Chance from a generator written out in the repository.** PCG-XSH-RR, with
  the sequence for one seed pinned by a test, because `System.Random` does not
  promise the same numbers across runtime versions.
- **Two floor generators**, rooms and corridors from a binary space partition
  and caves from a drunkard walk, held to one contract over forty seeds.
- **Recursive shadowcasting** across eight octants, with ground once seen
  staying on the map.
- **A\* that paths round the other monsters** and still reaches a player who is
  standing in the way.
- **Four monster behaviours**, depth that costs something, equipment, a pack, a
  message log, and an ending on floor ten.
- **Saving and resuming**, including the state of the dice, so a resumed run
  plays on rather than replaying the seed.
- **Hit flashes, sparks and screen shake**, which the core knows nothing about.
- **222 tests**, continuous integration on three platforms, and a check that a
  published build contains a program that can actually start.

## v0.2: a dungeon worth reading

Shipped. The floors were readable but plain, and this milestone was about what
is on them.

- **Doors** that block sight but not movement, the first tile to separate the
  two. They stand in the throat of a corridor where it meets a room, so walking
  down one no longer shows what is waiting at the far end.
- **Traps**, drawn as ordinary ground until something stands on one. They do
  not care who that was, so leading a monster across one is a real move, and
  there are more of them hitting harder the deeper the floor.
- **Diagonal movement**, for the player and for the monsters together. A
  diagonal between two walls that meet at a corner is refused for both.
- **A better view of what is happening**: the whole log rather than the last
  six lines, and what the monster beside you has left.
- **A pack that keeps its order**, grouped by kind with potions first.
- **A way to look at a floor without playing it**, with `--print-floor`.

## v0.3: a run worth repeating

- **More than one way to build a character.** Right now every run plays the
  same, because the only choice is which weapon to carry.
- **Monsters that do something other than walk at you.** A monster that flees
  when hurt, or one that calls others, changes how a room is fought.
- ~~**A reason to go back up.**~~ Done. Every floor below the first has stairs
  where the player came in, and a floor that is left is the floor that is found
  on the way back: the same ground, the same monsters, and whatever was walked
  past still lying there. The depth bonus is paid on the deepest floor reached,
  so retreating costs ground rather than points.
- **A record of past runs**, so a seed can be compared against how it went last
  time.

## Not planned

- **Multiplayer.** Nothing about the turn model would survive it.
- **Graphical tiles.** The game is ASCII on purpose. The palette and the glyphs
  are the art.
- **A launcher or settings window.** The command line covers what there is to
  configure, and a window to configure a window is not worth writing.
