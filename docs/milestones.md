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

The floors are readable but plain. This milestone is about what is on them.

- **Doors** that block sight but not movement, which is the first tile to
  separate the two. See issue 1.
- **Traps**, so walking into unexplored ground is a decision. See issue 2.
- **Diagonal movement**, for the player and for the monsters together. See
  issue 3.
- **A better view of what is happening**: reading back through the message log
  (issue 4), and seeing what a monster has left when it is next to you
  (issue 7).
- **A pack that keeps its order**, so the key for a potion does not move every
  time something is picked up. See issue 6.
- **A way to look at a floor without playing it**, which makes a bad map easy
  to report. See issue 5.

## v0.3: a run worth repeating

- **More than one way to build a character.** Right now every run plays the
  same, because the only choice is which weapon to carry.
- **Monsters that do something other than walk at you.** A monster that flees
  when hurt, or one that calls others, changes how a room is fought.
- **A reason to go back up.** The dungeon only goes down, so there is never a
  decision about whether to press on or retreat.
- **A record of past runs**, so a seed can be compared against how it went last
  time.

## Not planned

- **Multiplayer.** Nothing about the turn model would survive it.
- **Graphical tiles.** The game is ASCII on purpose. The palette and the glyphs
  are the art.
- **A launcher or settings window.** The command line covers what there is to
  configure, and a window to configure a window is not worth writing.
