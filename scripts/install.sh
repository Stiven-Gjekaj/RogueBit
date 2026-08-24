#!/bin/sh
#
# Installs RogueBit from a release, on Linux and macOS.
#
#     curl -fsSL https://raw.githubusercontent.com/Stiven-Gjekaj/RogueBit/main/scripts/install.sh | sh
#
# The releases hold self contained builds, so nothing has to be installed
# first. No .NET, no SDK, no clone, no build.
#
# Options, as arguments or as environment variables:
#
#   --version <tag>   ROGUEBIT_VERSION  Which release. The newest by default.
#   --to <dir>        ROGUEBIT_HOME     Where the game goes.
#   --bin <dir>       ROGUEBIT_BIN      Where the launcher goes.
#   --uninstall                         Take both away again and stop.
#
# Written for POSIX sh, so it runs under dash as well as bash.

set -eu

REPO="Stiven-Gjekaj/RogueBit"

version="${ROGUEBIT_VERSION:-}"
home="${ROGUEBIT_HOME:-${XDG_DATA_HOME:-$HOME/.local/share}/roguebit}"
bin="${ROGUEBIT_BIN:-$HOME/.local/bin}"
uninstall=no

say() { printf '%s\n' "$*"; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }

while [ $# -gt 0 ]; do
    case "$1" in
        --version) [ $# -ge 2 ] || die "--version needs a tag after it."; version="$2"; shift 2 ;;
        --to)      [ $# -ge 2 ] || die "--to needs a directory after it.";  home="$2";    shift 2 ;;
        --bin)     [ $# -ge 2 ] || die "--bin needs a directory after it."; bin="$2";     shift 2 ;;
        --uninstall) uninstall=yes; shift ;;
        # Every comment line under the shebang, stopping at the first line
        # that is not one. It counted to a fixed line number before, and the
        # count was one line long, so --help ended by printing "set -eu".
        -h|--help) awk 'NR > 1 { if ($0 !~ /^#/) exit; sub(/^# ?/, ""); print }' "$0"; exit 0 ;;
        *) die "'$1' is not an option this script knows. Try --help." ;;
    esac
done

if [ "$uninstall" = yes ]; then
    rm -rf "$home"
    rm -f "$bin/roguebit"
    say "Removed $home and $bin/roguebit."
    say "Saved runs and scores are left alone. They live in"
    say "  ${XDG_DATA_HOME:-$HOME/.local/share}/RogueBit"
    exit 0
fi

# ------------------------------------------------------------- downloading ---

if command -v curl >/dev/null 2>&1; then
    fetch() { curl -fsSL "$1" -o "$2"; }
    read_url() { curl -fsSL "$1"; }
elif command -v wget >/dev/null 2>&1; then
    fetch() { wget -qO "$2" "$1"; }
    read_url() { wget -qO- "$1"; }
else
    die "This needs curl or wget, and has neither."
fi

# Asks up to three times, waiting twice as long after each refusal.
#
# A 504 is weather rather than an answer. Giving up on the first one sends the
# reader off to look up a version number by hand, which is a poor reply from a
# script whose whole promise is that it is one line.
read_url_retrying() {
    _wait=1
    _try=1

    while :; do
        if _body="$(read_url "$1" 2>/dev/null)"; then
            printf '%s' "$_body"
            return 0
        fi

        [ "$_try" -ge 3 ] && return 1

        sleep "$_wait"
        _wait=$((_wait * 2))
        _try=$((_try + 1))
    done
}

# --------------------------------------------------------------- the build ---

kernel="$(uname -s)"
machine="$(uname -m)"

case "$kernel" in
    Linux)  os=linux ;;
    Darwin) os=osx ;;
    *) die "There is no build for $kernel. The releases hold Linux, macOS and Windows." ;;
esac

case "$machine" in
    x86_64|amd64)  arch=x64 ;;
    arm64|aarch64) arch=arm64 ;;
    *) die "There is no build for $machine." ;;
esac

case "$os-$arch" in
    linux-x64) asset="RogueBit-linux-x64.tar.gz" ;;
    osx-arm64) asset="RogueBit-osx-arm64.tar.gz" ;;
    *)
        die "There is no $os-$arch build. The releases hold linux-x64, osx-arm64
       and win-x64. Building from source is in the README, and needs the
       .NET 10 SDK."
        ;;
esac

# ------------------------------------------------------------- the release ---

# The newest release, out of an api.github.com answer.
#
# Not /releases/latest. That endpoint skips pre-releases, and every RogueBit
# release so far is one, so it answers 404 here. This asks for the newest
# release of any kind instead.
#
# Split on the commas first. Without that, the greedy match runs to the last
# tag_name on the line rather than the first.
tag_from_api() {
    tr ',' '\n' \
        | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' \
        | head -1
}

# The newest release, out of the releases feed. Each entry carries a link to
# its own tag page, and the newest entry is first:
#
#     <link rel="alternate" href="https://github.com/OWNER/REPO/releases/tag/v0.3.0-alpha"/>
#
# Split on the angle brackets first, one tag to a line. The feed arrives
# pretty printed today, and on a single line the greedy match would run to the
# last entry and quietly name the oldest release instead of the newest. That
# was watched happening before this line was written.
tag_from_feed() {
    tr '<' '\n' \
        | sed -n 's|.*href="https://github.com/[^"]*/releases/tag/\([^"]*\)".*|\1|p' \
        | head -1
}

if [ -z "$version" ]; then
    say "Looking for the newest release..."

    version="$(read_url_retrying "https://api.github.com/repos/$REPO/releases?per_page=1" | tag_from_api)"

    # The API allows sixty unauthenticated requests an hour to each address.
    # GitHub's own macOS runners share a small pool of outbound addresses, so
    # that budget is usually spent by somebody else before this script asks,
    # and the answer is 403. Anybody behind a company or a campus NAT is in
    # the same position.
    #
    # The releases feed is served by github.com rather than by the API, so it
    # is not on that budget at all. It is second only because the API answer
    # is smaller and says plainly what it is.
    if [ -z "$version" ]; then
        say "The API would not say. Asking the releases feed instead..."
        version="$(read_url_retrying "https://github.com/$REPO/releases.atom" | tag_from_feed)"
    fi

    [ -n "$version" ] || die "Could not work out which release is the newest. Neither the
       API nor the releases feed would answer. Pass one with --version, for
       example --version v0.3.0-alpha. They are listed at
       https://github.com/$REPO/releases"
fi

base="https://github.com/$REPO/releases/download/$version"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT INT TERM

say "Fetching RogueBit $version for $os-$arch..."
fetch "$base/$asset" "$work/$asset" || die "Could not download $base/$asset"
fetch "$base/SHA256SUMS" "$work/SHA256SUMS" || die "Could not download the checksums."

# ------------------------------------------------------------- the check ---

if command -v sha256sum >/dev/null 2>&1; then
    got="$(sha256sum "$work/$asset" | cut -d' ' -f1)"
elif command -v shasum >/dev/null 2>&1; then
    got="$(shasum -a 256 "$work/$asset" | cut -d' ' -f1)"
else
    die "This needs sha256sum or shasum to check the download, and has neither."
fi

want="$(sed -n "s/^\([0-9a-f]\{64\}\)  *$asset\$/\1/p" "$work/SHA256SUMS" | head -1)"

[ -n "$want" ] || die "SHA256SUMS has no line for $asset."

if [ "$got" != "$want" ]; then
    die "The download does not match its checksum.
       wanted $want
       got    $got
       Nothing has been installed."
fi

say "Checksum matches."

# ----------------------------------------------------------- unpacking ---

say "Unpacking into $home..."
rm -rf "$home"
mkdir -p "$home"
tar -xzf "$work/$asset" -C "$home"

[ -f "$home/RogueBit" ] || die "The archive holds no RogueBit program. Nothing has been installed."
chmod +x "$home/RogueBit"

# macOS marks anything downloaded, and Gatekeeper refuses to start it. The
# build is not signed, so without this the first run is a dialogue rather than
# a game. Best effort: an older macOS without xattr is not a reason to stop.
if [ "$os" = osx ] && command -v xattr >/dev/null 2>&1; then
    xattr -dr com.apple.quarantine "$home" 2>/dev/null || true
fi

# ------------------------------------------------------------ the launcher ---

mkdir -p "$bin"

# A wrapper rather than a symlink. The program looks beside itself for the
# hundred files it ships with, and a symlink can leave it looking in the wrong
# place.
cat > "$bin/roguebit" <<LAUNCHER
#!/bin/sh
exec "$home/RogueBit" "\$@"
LAUNCHER

chmod +x "$bin/roguebit"

# --------------------------------------------------------------- proving it ---

# Starting it is the only proof that the download was the right build for this
# machine. An archive can arrive whole, match its checksum, and still be for
# the wrong libc.
if ! help="$("$bin/roguebit" --help 2>&1)"; then
    die "RogueBit is installed at $home but will not start.
       Try running $home/RogueBit yourself to see what it says."
fi

say ""
say "RogueBit $version is in $home."
say "The launcher is $bin/roguebit."

case ":${PATH}:" in
    *":$bin:"*)
        say ""
        say "Start it with:  roguebit"
        ;;
    *)
        say ""
        say "$bin is not on your PATH. Either run it by its full path:"
        say "  $bin/roguebit"
        say ""
        say "or add the directory to your shell profile:"
        say "  export PATH=\"\$PATH:$bin\""
        ;;
esac

say ""
say "The game opens a window, so it needs a display."

# Asked of the build that was installed rather than assumed. Older releases
# have no such option, and pointing somebody at one is worse than saying
# nothing.
case "$help" in
    *--print-floor*)
        say "To see it work without one, print a floor as text:"
        say "  $bin/roguebit --print-floor --seed 4242 --depth 3"
        ;;
esac

say ""
say "To remove it again, run this script with --uninstall."
