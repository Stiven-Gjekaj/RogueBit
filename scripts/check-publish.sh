#!/usr/bin/env bash
#
# Checks that a published build actually contains a program that can start.
#
# This exists because of a bug that shipped. SadConsole.Host.MonoGame declares a
# reference to MonoGame.Framework but does not carry it, so the compiler was
# satisfied, the build was green, and the published output had no such assembly.
# The game threw FileNotFoundException before a window ever appeared.
#
# Nothing caught it, because continuous integration compiles the frontend and
# cannot run it on a machine with no display. This check needs no display: it
# asks whether the files are there.
#
#     scripts/check-publish.sh <published directory>

set -euo pipefail

directory="${1:?Usage: check-publish.sh <published directory>}"

if [ ! -d "$directory" ]; then
  echo "No such directory: $directory" >&2
  exit 1
fi

failed=0

# Named exactly, because these are the same on every platform.
for assembly in \
  RogueBit.dll \
  RogueBit.Core.dll \
  SadConsole.dll \
  SadConsole.Host.MonoGame.dll \
  MonoGame.Framework.dll \
  TheSadRogue.Primitives.dll
do
  if [ -f "$directory/$assembly" ]; then
    echo "ok      $assembly"
  else
    echo "MISSING $assembly" >&2
    failed=1
  fi
done

# Matched by pattern, because the native names differ per platform and a check
# that guessed them would fail for the wrong reason. Several patterns per
# library, since one name does not cover every platform: OpenAL ships as
# libopenal.so, libopenal.1.dylib and soft_oal.dll.
check_native() {
  local label="$1"
  shift

  local found
  for pattern in "$@"; do
    found=$(find "$directory" -maxdepth 2 -iname "$pattern" -exec basename {} \; 2>/dev/null | head -1)
    if [ -n "$found" ]; then
      echo "ok      $label ($found)"
      return
    fi
  done

  echo "MISSING $label, nothing matched: $*" >&2
  failed=1
}

check_native "SDL" "*SDL2*"
check_native "OpenAL" "*openal*" "*oal*"

if [ "$failed" -ne 0 ]; then
  echo >&2
  echo "The published build is missing something it needs to start." >&2
  echo "A reference that only the compiler sees is the usual cause." >&2
  exit 1
fi

echo
echo "The published build has everything it needs to start."
