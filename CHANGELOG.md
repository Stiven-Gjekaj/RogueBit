# Changelog

All notable changes to RogueBit are recorded here. The format is based on
Keep a Changelog (https://keepachangelog.com), and this project follows
Semantic Versioning.

## 0.1.0-alpha

The first version that builds. Everything before this point is kept on the
`archive/original-history` branch and does not compile.

### Added

- **A rendering-free core.** `RogueBit.Core` holds every rule and takes no
  drawing dependency, so a whole game can be played out in a test with no
  display attached.
- **Two dungeon generators.** Rooms and corridors from a binary space partition
  on odd floors, caves from a drunkard walk on even ones. Both are held to the
  same contract over forty seeds: one connected region, a wall all the way
  round, an entrance and stairs on walkable ground, and the same floor from the
  same seed.
- **Recursive shadowcasting.** Eight octants, each scanned outwards carrying the
  slopes of the wedge still in the light. Ground once seen stays on the map,
  dimmed.
- **A\* pathfinding.** The search takes a blocking test as well as the map, so a
  monster paths round the others. The goal is exempt from that test, which is
  what lets one walk onto the player to attack.
- **Four monster behaviours.** Chasers, swift monsters that move twice per turn,
  archers that shoot along a clear line, and a boss that hits twice as hard
  below half health.
- **Depth that costs something.** Monster counts and strength rise with depth
  while potions grow scarcer. The harder kinds unlock with depth, so the first
  floor is always goblins.
- **Items, a pack and two equipment slots.** Equipping over something puts the
  old item back rather than destroying it.
- **A message log** that counts a repeated line instead of letting a run of
  misses push everything else off the panel.
- **A colour-blind palette** and three movement layouts at once: arrows, WASD
  and hjkl.
- **Saving and resuming a run.** The floor, the pack, what has been explored and
  the exact state of the generator all come back, so a resumed run plays on
  rather than replaying the seed. The file is written beside the target and
  moved over it, so a crash midway cannot leave half a save. A run that ends is
  removed, so dying cannot be undone by loading.
- **A generator written out in the repository.** PCG-XSH-RR replaces
  `System.Random`, which does not promise the same sequence from one seed across
  runtime versions and has already changed once. A test pins the output for seed
  12345, so drift is a failing build rather than a silent change of meaning.
- **Hit flashes, sparks and screen shake.** The core reports what happened and
  on which cell; the frontend decides entirely what that looks like, and
  `--no-effects` switches all of it off.
- **Continuous integration** that tests the core on Linux and compiles the whole
  solution on Linux, Windows and macOS.
- **A release cut from a file.** Writing a version into `.github/release-version`
  tags the commit and builds a draft release, after checking the version against
  the project, the changelog and the commit's own checks.

### Fixed

These are defects carried over from the version that never compiled.

- **The same seed now really does replay the same run.** Restarting used to
  carry on with a generator that had already been drawn from, so `R` handed the
  player a different dungeon on the same seed.
- **Worn equipment now reaches the numbers combat reads.** Armour raised a total
  that nothing ever asked for, so it did nothing at all.
- **The combat log agrees with who is fighting.** It said "You hits a jackal",
  and it said a killing blow "kills it" when the player was the one who died.
- **A seed that is not a number is reported.** The old build dropped one in
  silence and handed the player a different dungeon with no explanation.
