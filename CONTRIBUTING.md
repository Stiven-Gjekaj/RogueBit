# Contributing to RogueBit

Thank you for looking. This file says how to set the project up and what is
expected of a change.

## Set up

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Nothing else.

```sh
git clone https://github.com/Stiven-Gjekaj/RogueBit
cd RogueBit
dotnet test
```

`dotnet run --project src/RogueBit.Console` opens the game.

## The gate

Run all of these before every commit. Not a selection.

```sh
dotnet format --verify-no-changes
dotnet build --configuration Release
dotnet test
```

Continuous integration runs the same three. A change that fails any of them
fails there as well.

## What a change looks like

- **One logical change per commit.** Code and its tests go in together.
  Documentation goes in its own commit. A wide rename goes in its own commit
  with no change of behaviour inside it.
- **Write the subject line in the present tense**, saying what the change does.
  No version numbers in a subject line.
- **No em-dashes, and no emoji**, in code, comments, documentation or commit
  messages.
- **Do not open a pull request unless somebody asks for one.**

## What earns trust here

- **Run it.** Do not conclude that it will work.
- **When you add a test, break the thing it covers on purpose and watch it
  fail.** A test that cannot fail is worse than none, because it makes something
  look guarded while it drifts. This project has already caught one such case
  that way.
- **Build the state a test needs inside the test.** `MapBuilder` exists so a
  test can state the map it wants in ASCII. A test that asks a generator for a
  shape fails the day somebody tunes the generator, for no real reason.
- **Say plainly when a measurement does not support the conclusion.**

## Where the rules live

`RogueBit.Core` must never take a rendering dependency. That is what keeps a run
reproducible and testable with no display attached. If a change needs the core
to know something about drawing, the change is in the wrong project.

The frontend holds no rules. It asks the run what is true and draws that.
