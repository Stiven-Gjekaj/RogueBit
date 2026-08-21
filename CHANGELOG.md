# Changelog

All notable changes to RogueBit are recorded here. The format is based on
Keep a Changelog (https://keepachangelog.com), and this project follows
Semantic Versioning.

## 0.3.0-alpha

### Added

- **A monster that runs when it is hurt.** A scavenger comes at you while it
  is whole and breaks the moment it is below half, taking whichever of the
  eight steps puts the most ground between the two of you. Cornered it fights
  whatever is in reach, and cornered out of reach it holds still rather than
  walking at you. Running is real movement, so a scavenger driven back over a
  trap sets it off, which is how you finish one that will not stand and be
  hit. Nothing about the choice draws from the run's chance, so two runs of
  one seed still match to the turn.
- **A monster that calls the others to it.** A howler notices the player from
  further off than anything else and spends that turn calling. Everything
  within twelve cells comes for the player whatever it can see from where it
  stands. Calling costs the howler its turn, so killing one first is the right
  answer rather than merely an obvious one, and a howler roused by another
  howler does not call, so one sighting cannot travel a floor by relay. The
  call is written to the log whether or not the player can see it, because a
  call is a sound.
- **A record of every finished run**, in `runs.json` beside the save. It keeps
  the seed, the score, the deepest floor, the turns and the ending. Starting a
  seed played before opens with the floor it reached and what it scored, and
  the panel at the end says whether this run is the best on that seed and what
  there is left to beat. It is written beside the target and moved over it,
  and it carries a version it refuses to read past, both the way the save has
  always been written.
- **A tool that works out the recording route**, in `tools/RogueBit.Route`. It
  plays a seed headlessly until the floor is worth looking at, writes that run,
  and prints the keys that finish it, and it will not offer a walk it has not
  played out and seen go through a doorway, onto a trap and round a corner.
  The recording on the front page could not be made again from the repository
  before this.

### Changed

- **What lives on a floor is decided by bands that do not overlap.** They used
  to be thresholds that ran from zero and swallowed each other, so what a line
  was worth depended on every line above it. Adding one kind at the top halved
  another at the bottom and nothing said so. Counted over four hundred seeds a
  floor, it now reads: goblins 56 and jackals 26 on floor two, scavengers from
  floor two, howlers from floor three, and goblins 30 with four other kinds
  between 10 and 26 from floor four down. Floor one is still goblins and only
  goblins.
- **The recording on the front page** was made before doors, traps or diagonal
  movement existed, twenty three commits ago, directly under a banner that had
  been retaken and does show doorways. It is now seed 2, floor one, walking a
  corridor a corner at a time, through a doorway, onto a trap, and finishing
  beside a goblin with the status row reporting what the goblin has left.
- **The sample floor** under it is the output of `--print-floor` on the same
  seed, so both exhibits show the same game and anybody can print the second
  one for themselves. The old block had no doorway anywhere in it.
- **`scores.json` is no longer read or written.** It held one integer, the best
  score across every seed ever played, with no seed attached to it. Nothing
  migrates it, because carrying that number across would mean inventing a run
  that was never played. The file is left where it is rather than deleted.

### Fixed

- **The key that says it saves the run saves it.** Pressing `S` walked south
  and had done since WASD was offered alongside hjkl: `S` is in the movement
  table, the movement table is read first, and the branch that saved sat below
  it and was never once reached. Saving is now `F5`. The two tables live in one
  file and a test reads them both, because a key in both of them fails in
  silence.
- **The panel at the end of a run names the deepest floor reached.** It named
  the floor the body was lying on while the score was paid for the deepest, so
  climbing from six to three and dying there put two numbers that disagreed on
  one panel. The words came out of the drawing to make this readable by
  anything other than a person looking at a window.

## 0.2.0-alpha

### Added

- **Doors.** The one tile that can be walked through and cannot be seen
  through. They stand in the throat of a corridor where it meets a room, so a
  corridor no longer shows what is waiting at the far end of it.
- **Traps.** Drawn as ordinary ground until something stands on one, and then
  spent and visible. They do not care who stood on them, so leading a monster
  across one is a real move, and a monster killed that way pays nothing,
  because the player did not strike the blow. More of them, hitting harder,
  the deeper the floor.
- **Movement in eight directions**, for the player and for the monsters
  together, on the keypad and on yubn. A diagonal between two walls that meet
  at a corner is refused for both.
- **The whole message log**, on `m`. The panel shows six lines and the log
  keeps a hundred, so ninety four of them could never be read.
- **What the monster beside you has left**, at the end of the status line. The
  weakest of the ones beside you, because that is the one worth hitting.
- **A command that prints a floor without playing it**, so a bad map can be
  reported by seed and depth rather than described.
- **An install script**, for Linux and macOS in `sh` and for Windows in
  PowerShell. One line, no .NET, no clone, no build: it works out which build
  this machine wants, checks the download against the checksums published with
  the release, refuses anything that does not match, and starts the game once
  to prove the build runs before it reports success. Both are attached to every
  release as well as living on the branch, and `--uninstall` takes it away
  again.
- **A way back up.** Every floor below the first has stairs on the cell the
  player arrives on, and the comma takes whichever staircase is underfoot
  rather than only the one going down.
- **Floors that stay put.** A floor is built once and kept. Going back up
  reaches the floor that was left, with the same ground, the same monsters
  standing where they were, whatever was walked past still lying there, and
  what the player had explored still remembered. Taking a floor out of store
  draws nothing from the generator, so a run that walks up and down is the
  same run as one that walked straight down.

### Changed

- **The pack is grouped by kind with potions first**, and keeps arrival order
  inside each group, so the letter for a potion stops moving every time
  anything else is picked up.
- **The A\* estimate moved from Manhattan to Chebyshev.** Manhattan overstates
  the cost as soon as diagonals are allowed, and an estimate that overstates
  gives a short walk rather than a shortest one.
- **The depth bonus is paid on the deepest floor reached** rather than the
  floor being stood on. The two were the same number while the dungeon only
  went down. They are not now, and a score that fell as the player climbed
  would make retreating a decision about points rather than about surviving.
- **The save holds every floor the run has been on**, each with where the
  player came in, so a resumed run can climb into a floor it saw rather than
  into one built on the spot. The file version goes to two, and a version one
  save is refused: it does not record where the player came in, so its floor
  has nowhere to put the stairs back up.

- **The banner is retaken** on a floor this game builds, with the doorways on
  it and the version read from the project rather than written down. The old
  one predated doors and traps and advertised 0.1.0-alpha.

### Fixed

- **Escape closes the pack instead of quitting the game.** It was read before
  anything asked whether an overlay was open, so closing the pack saved the run
  and shut the game down. The branch meant to close it could never run.
- **A swift monster stops when it dies.** It took both of its steps whether or
  not it survived the first, which nothing could reach until a trap gave a
  monster a way to die on its own turn.
- **A floor with doors survives a save.** The save format wrote every tile it
  did not know as a wall.

## 0.1.2-alpha

Housekeeping. No rule changed, so a seed plays the run it played in
0.1.1-alpha.

### Changed

- **MonoGame moves to 3.8.5.1.** It carries the native libraries the window
  opens with, so of everything raised here it is the only one that reaches the
  archives. It was checked by publishing the game, running the output through
  the publish check, and then starting it under a software OpenGL context and
  capturing a frame. A green build is not evidence that a window opens, which
  is what 0.1.0-alpha taught.
- **The test tooling moves to Microsoft.NET.Test.Sdk 18.9.0, xunit 2.9.3 and
  xunit.runner.visualstudio 4.0.0.** None of it ships. All 222 tests pass on
  it.
- **The workflow actions move up**, five of them, four crossing more than one
  major version. The artifact actions only run during a release, so release.yml
  was dispatched with no tag to build everything and publish nothing. That is
  what the tag switch exists for. The release action only runs when there is a
  tag, so this version is the first thing that exercises it.

### Added

- **A recording of the window running**, at the top of the README, with the
  tools that captured it. The route through the floor was worked out by playing
  the same seed with no display attached, because the game is deterministic and
  the same keys replay the same run.
- **A roadmap**, which says what 0.2 is meant to hold and which says what is not
  planned.
- **Dependabot**, watching the packages and the actions once a week. The routine
  bumps are grouped into one pull request each. SadConsole and MonoGame are left
  out of the groups, because a package the window draws through is not a routine
  bump.

## 0.1.1-alpha

The first version that runs. 0.1.0-alpha builds and then throws before it draws
anything, so its archives are not usable.

### Fixed

- **The game starts.** `SadConsole.Host.MonoGame` declares a reference to
  `MonoGame.Framework` but does not carry it. The compiler was satisfied, the
  build was green, and the published output had no such assembly, so starting
  the game threw `FileNotFoundException` before a window appeared.

### Added

- **An ending.** Floor ten has no stairs down and holds a warden twice the size
  of the one halfway. Killing it wins the run and is worth 250 points. The
  monsters do not take their turn after the winning blow, so one of them cannot
  kill a player who has already finished.
- **A check that a published build can start.** Continuous integration
  publishes and then asks whether the assemblies and the native libraries are
  actually in the output. Compiling proves a reference resolves; it does not
  prove the file reaches the archive. The release refuses to attach an archive
  that fails this.
- **Tests for the effects the frontend draws**, in their own project, so the
  core suite keeps its promise of touching no rendering dependency.
- **A banner** built from a dungeon the game really played, with the tool that
  captured it and the script that draws it.

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
- **An ending.** Floor ten has no stairs down and holds a warden twice the size
  of the one halfway. Killing it wins the run. The monsters do not take their
  turn after the winning blow, so one of them cannot kill a player who has
  already finished.
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
- **The game starts at all.** `SadConsole.Host.MonoGame` references
  `MonoGame.Framework` but does not carry it. The reference satisfied the
  compiler, so the build was green while the published binaries had no such
  assembly, and starting the game threw before a window appeared. Continuous
  integration could not catch it, because it compiles the frontend and cannot
  run it.
