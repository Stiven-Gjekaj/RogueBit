#!/bin/sh
#
# Checks how scripts/install.sh works out which release is the newest.
#
#     sh tests/install/run.sh
#
# That lookup is the one part of the installer nothing else can reach. The
# install workflow runs the whole script against a real release, so it only
# ever sees the path where the API answers. The fallback exists for the case
# where it does not, and without this it would first run on somebody's machine
# on the day the API was down.
#
# The functions are lifted out of the shipped script rather than copied here.
# A test that holds its own copy of the thing it tests passes whatever the
# shipped copy does.

set -eu

root="$(CDPATH='' cd -- "$(dirname -- "$0")/../.." && pwd)"
here="$root/tests/install"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

script="$root/scripts/install.sh"
functions="$work/functions.sh"

for name in read_url_retrying tag_from_api tag_from_feed; do
    awk -v want="^$name\\\\(\\\\) \\\\{" '$0 ~ want, /^\}/' "$script" >> "$functions"
done

# awk finding nothing is silence, and silence here would be a test that ran
# against three functions that no longer exist and passed.
found="$(grep -c '^}' "$functions" || true)"
[ "$found" = 3 ] || {
    printf 'error: found %s of the 3 functions in %s. Were they renamed?\n' "$found" "$script" >&2
    exit 1
}

# shellcheck source=/dev/null
. "$functions"

failures=0

check() {
    if [ "$2" = "$3" ]; then
        printf 'ok   %s\n' "$1"
    else
        printf 'FAIL %s: wanted [%s], got [%s]\n' "$1" "$3" "$2"
        failures=$((failures + 1))
    fi
}

# ------------------------------------------------------------ the parsing ---

tr -d '\n' < "$here/releases.atom" > "$work/one-line.atom"

check "the api answer names the newest release" \
    "$(tag_from_api < "$here/releases.json")" "v0.3.0-alpha"

check "the feed names the newest release" \
    "$(tag_from_feed < "$here/releases.atom")" "v0.3.0-alpha"

# The feed arrives pretty printed today. On one line a greedy match runs past
# every entry to the last one, and the script would quietly install the oldest
# release it could see rather than the newest.
check "the feed still does on a single line" \
    "$(tag_from_feed < "$work/one-line.atom")" "v0.3.0-alpha"

# The first link in the feed is the feed's own, and points at the release list
# rather than at any release.
check "the feed's own link is not mistaken for a release" \
    "$(printf '%s' '<link href="https://github.com/o/r/releases"/>' | tag_from_feed)" ""

check "nothing in means nothing out, from the api" "$(printf '' | tag_from_api)" ""
check "nothing in means nothing out, from the feed" "$(printf '' | tag_from_feed)" ""
check "neither reader is fooled by the other's answer" \
    "$(tag_from_feed < "$here/releases.json")" ""

# ------------------------------------------------------------- the asking ---

# read_url runs inside a command substitution, so the count has to live in a
# file to outlive the subshell. A variable was tried first and every call
# looked like the first one.
counter="$work/asked"
asked() { printf 'x' >> "$counter"; wc -c < "$counter" | tr -d ' '; }

sleep() { :; }

: > "$counter"
read_url() { asked >/dev/null; return 22; }

check "a refusal every time gives back nothing" \
    "$(read_url_retrying https://example.invalid | tag_from_api)" ""
check "and it asked three times before giving up" \
    "$(wc -c < "$counter" | tr -d ' ')" "3"

: > "$counter"
read_url() {
    [ "$(asked)" -lt 3 ] && return 22
    cat "$here/releases.json"
}

check "a refusal that clears up is waited out" \
    "$(read_url_retrying https://example.invalid | tag_from_api)" "v0.3.0-alpha"

# -----------------------------------------------------------------------------

[ "$failures" = 0 ] || { printf '\n%s check(s) failed\n' "$failures" >&2; exit 1; }
printf '\nall good\n'
