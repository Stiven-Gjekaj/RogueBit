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
        -h|--help) sed -n '2,19p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
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

if [ -z "$version" ]; then
    say "Looking for the newest release..."

    # Not /releases/latest. That endpoint skips pre-releases, and every RogueBit
    # release so far is one, so it answers 404 here. This asks for the newest
    # release of any kind instead.
    version="$(
        read_url "https://api.github.com/repos/$REPO/releases?per_page=1" \
            | tr ',' '\n' \
            | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' \
            | head -1
    )"

    [ -n "$version" ] || die "Could not work out the newest release. Pass one with --version."
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
