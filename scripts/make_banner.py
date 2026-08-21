#!/usr/bin/env python3
"""Builds assets/banner.svg from a captured frame of a real run.

    python3 scripts/make_banner.py

The dungeon in the banner is not drawn by hand. assets/banner-frame.json is a
viewport captured from a game the code actually played: seed 21, turn 233, on
the first floor. Recapture it with

    dotnet run --project tools/RogueBit.BannerFrame -- --search 0 40

which plays every seed in the range and keeps the best frame any of them
produced. Most seeds give nothing, because the bot dies before the floor is
worth looking at. A single --seed is quicker and usually worse.

Every colour here is copied from src/RogueBit.Console/Theme.cs, so the banner
and the running game cannot disagree about what a goblin looks like.

Two things keep it safe to put in a README. The font is subsetted and embedded
as a data URI, because an SVG inside an img tag cannot fetch anything. And every
run of glyphs carries a textLength, so the grid holds its alignment even if the
embedded font somehow fails to apply and a fallback is used instead.

The subsetted font is committed as assets/banner-font.woff2 and read from there.
It is not rebuilt on every run, because the woff2 encoder does not produce the
same bytes twice for the same input, which made every rebuild of the banner a
spurious diff. Rebuild the font only when the text in the banner needs a
character the subset does not have:

    python3 scripts/make_banner.py --refresh-font    # needs fonttools and brotli
"""

import base64
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
STATE = json.loads((ROOT / "assets" / "banner-frame.json").read_text())


def version() -> str:
    """The version the project stamps into what it builds.

    Read rather than written down here. It used to be written down twice, and
    both copies still said 0.1.0-alpha two releases later.
    """
    props = (ROOT / "Directory.Build.props").read_text()
    found = re.search(r"<Version>(.*?)</Version>", props)

    if found is None:
        raise SystemExit("Directory.Build.props has no <Version>.")

    return found.group(1)


# How many tests the suite holds. Nothing in the repository states this, so it
# is the one number here that has to be moved by hand.
TESTS = 389


def _trim(state: dict) -> dict:
    """Drops fully empty edge rows and columns from the captured frame."""
    rows, kinds = state["rows"], state["kinds"]

    top = next(i for i, r in enumerate(rows) if r.strip())
    bottom = len(rows) - next(i for i, r in enumerate(reversed(rows)) if r.strip())
    rows, kinds = rows[top:bottom], kinds[top:bottom]

    left = min(len(r) - len(r.lstrip()) for r in rows if r.strip())
    right = max(len(r.rstrip()) for r in rows)
    state["rows"] = [r[left:right] for r in rows]
    state["kinds"] = [k[left:right] for k in kinds]
    return state


STATE = _trim(STATE)

# ---------------------------------------------------------------- palette ---
# Straight from src/RogueBit.Console/Theme.cs, so the banner and the running
# game cannot disagree about what the player or a goblin looks like.
BG = "#0A0C0E"
PANEL = "#10151A"
EDGE = "#1E262C"
WALL_LIT = "#7A828A"
WALL_DIM = "#34393E"
FLOOR_LIT = "#B0B8BE"
FLOOR_DIM = "#464C52"
STAIRS = "#F0C85A"
DOOR = "#BA8C60"
TRAP = "#CE5C78"
PLAYER = "#5AD6E2"
MONSTER = "#7EC46C"
BOSS = "#E2604E"
COIN = "#EEBE44"
POTION = "#E26E96"
GEAR = "#96AAF0"
TEXT = "#DEE8EC"
DIM = "#7E8E94"
GOOD = "#7ED096"
BAD = "#EE7A68"
WARN = "#EEBE44"

KIND_COLOUR = {
    "P": PLAYER, "M": MONSTER, "B": BOSS, "C": COIN, "O": POTION, "E": GEAR,
    "S": STAIRS, "D": DOOR, "T": TRAP, "f": FLOOR_LIT, "r": FLOOR_DIM, "w": WALL_LIT, "d": WALL_DIM,
    " ": BG,
}
LOG_COLOUR = {"Good": GOOD, "Bad": BAD, "Warning": WARN, "Normal": TEXT}

# ----------------------------------------------------------------- canvas ---
W, H = 1280, 480
LEFT_X = 52          # identity column
SPLIT = 560          # where the game panel starts
CELL_W, CELL_H = 12.0, 21.5
FONT_SIZE = 19.0
ADVANCE = FONT_SIZE * 1233 / 2048      # DejaVu Sans Mono advance, in em units
TRACKING = CELL_W - ADVANCE

# -------------------------------------------------------------------- font ---
SOURCE_FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf"

# What the committed subset covers. Rebuilding the font rewrites this list.
# Every glyph MapText can draw a tile with. Naming them here rather than
# letting the captured frame decide means a frame that happens to hold a tile
# the last one did not is drawn rather than refused, and a new tile costs one
# font rebuild rather than a failed banner build to find out about.
TILE_GLYPHS = "#.><+^"

FONT_COVERS = (
    " !\"#$'()*+,-./0123456789:<>@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_"
    "abcdefghijklmnopqrstuvwxyz\u00b7\u2026"
)


def characters_used() -> set[str]:
    """Every character the banner draws."""
    used = set(" ")
    for row in STATE["rows"]:
        used |= set(row)
    meta = STATE["meta"]
    for line in meta["log"]:
        used |= set(line["text"])
    used |= set("HP SCORE FLOOR TURN SEED abcdefghijklmnopqrstuvwxyz")
    used |= set("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789./-_:$#@!>,()+*'\"[]\u2026\u00b7")
    used |= set(TILE_GLYPHS)
    return used


def refresh_font(path: pathlib.Path) -> None:
    """Rebuilds the committed subset. Needs fonttools and brotli installed."""
    from fontTools.subset import Options, Subsetter
    from fontTools.ttLib import TTFont

    font = TTFont(SOURCE_FONT)
    options = Options()
    options.layout_features = []
    options.desubroutinize = True
    options.drop_tables += ["DSIG"]
    subsetter = Subsetter(options=options)
    subsetter.populate(text="".join(sorted(characters_used())))
    subsetter.subset(font)

    # Pinning the timestamps removes one source of drift. It does not remove
    # them all, which is why the result is committed rather than rebuilt.
    font["head"].created = 0
    font["head"].modified = 0

    font.flavor = "woff2"
    font.save(str(path))
    print(f"rebuilt {path} ({path.stat().st_size / 1024:.1f} KB)")


def embedded_font() -> str:
    """Reads the committed subset and returns it base64 encoded."""
    path = ROOT / "assets" / "banner-font.woff2"

    if not path.exists():
        raise SystemExit(
            f"{path} is missing. Rebuild it with:\n"
            f"    python3 scripts/make_banner.py --refresh-font"
        )

    missing = {c for c in characters_used() if c.strip()} - set(FONT_COVERS)
    if missing:
        raise SystemExit(
            "The committed font subset has no glyph for: "
            + " ".join(sorted(missing))
            + "\nRebuild it with: python3 scripts/make_banner.py --refresh-font"
        )

    return base64.b64encode(path.read_bytes()).decode("ascii")


# ---------------------------------------------------------------- wordmark ---
# A five by seven block face. The game is made of cells, so its name is too.
GLYPHS = {
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "G": [".####", "#....", "#....", "#..##", "#...#", "#...#", ".###."],
    "U": ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "B": ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
    "I": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####"],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
}


def wordmark(x: float, y: float, block: float) -> str:
    """Draws ROGUEBIT, with BIT in the player's own cyan."""
    out = []
    cursor = x
    for index, letter in enumerate("ROGUEBIT"):
        colour = FLOOR_LIT if index < 5 else PLAYER
        rows = GLYPHS[letter]
        for row_index, row in enumerate(rows):
            run_start = None
            for column in range(len(row) + 1):
                filled = column < len(row) and row[column] == "#"
                if filled and run_start is None:
                    run_start = column
                elif not filled and run_start is not None:
                    out.append(
                        f'<rect x="{cursor + run_start * block:.1f}" '
                        f'y="{y + row_index * block:.1f}" '
                        f'width="{(column - run_start) * block:.1f}" height="{block:.1f}" '
                        f'fill="{colour}"/>'
                    )
                    run_start = None
        cursor += block * 6
    return "\n".join(out)


def escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


# --------------------------------------------------------------- map panel ---
HIGHLIGHT = {"P": (PLAYER, 0.20), "M": (MONSTER, 0.15), "B": (BOSS, 0.18),
             "C": (COIN, 0.12), "O": (POTION, 0.12), "E": (GEAR, 0.12), "S": (STAIRS, 0.14)}


def player_cell() -> tuple[int, int]:
    for row_index, kinds in enumerate(STATE["kinds"]):
        column = kinds.find("P")
        if column >= 0:
            return column, row_index
    return len(STATE["kinds"][0]) // 2, len(STATE["kinds"]) // 2


def map_rows(x: float, y: float) -> str:
    """One text element per row, one tspan per run of a single colour."""
    out = []

    # Backing tints first, so glyphs sit on top of them.
    for row_index, kinds in enumerate(STATE["kinds"]):
        for column, kind in enumerate(kinds):
            if kind not in HIGHLIGHT:
                continue
            colour, alpha = HIGHLIGHT[kind]
            out.append(
                f'<rect x="{x + column * CELL_W - 1.5:.1f}" '
                f'y="{y + row_index * CELL_H - FONT_SIZE * 0.82:.1f}" '
                f'width="{CELL_W + 3:.1f}" height="{CELL_H - 2:.1f}" rx="2.5" '
                f'fill="{colour}" fill-opacity="{alpha}"/>'
            )

    for row_index, (glyphs, kinds) in enumerate(zip(STATE["rows"], STATE["kinds"])):
        spans = []
        column = 0
        while column < len(glyphs):
            kind = kinds[column]
            end = column
            while end < len(glyphs) and kinds[end] == kind:
                end += 1
            chunk = glyphs[column:end]
            if chunk.strip():
                # textLength pins the run to exactly its cells. If the embedded
                # font ever fails to apply, the grid still cannot drift.
                spans.append(
                    f'<tspan x="{x + column * CELL_W:.1f}" '
                    f'textLength="{len(chunk) * CELL_W:.1f}" lengthAdjust="spacing" '
                    f'fill="{KIND_COLOUR.get(kind, FLOOR_DIM)}">{escape(chunk)}</tspan>'
                )
            column = end
        if spans:
            out.append(
                f'<text y="{y + row_index * CELL_H:.1f}" class="mono" '
                f'font-size="{FONT_SIZE}" xml:space="preserve">{"".join(spans)}</text>'
            )
    return "\n".join(out)


def build() -> str:
    meta = STATE["meta"]
    font64 = embedded_font()

    columns = len(STATE["rows"][0])
    rows_count = len(STATE["rows"])
    map_w = columns * CELL_W
    map_x = SPLIT + 26
    # Centre the map in the panel's vertical space rather than eyeballing it.
    panel_top, panel_h = 40, H - 80
    map_y = panel_top + (panel_h - rows_count * CELL_H) / 2 + FONT_SIZE * 0.78
    hud_x = map_x + map_w + 24
    hud_w = (W - 40) - hud_x - 12

    # Health colour follows the same thresholds the game's status bar uses.
    ratio = meta["hp"] / meta["maxHp"]
    hp_colour = BAD if ratio <= 1 / 3 else (WARN if ratio <= 1 / 2 else GOOD)

    stats = [
        ("HP", f'{meta["hp"]}/{meta["maxHp"]}', hp_colour),
        ("SCORE", str(meta["score"]), TEXT),
        ("FLOOR", str(meta["depth"]), TEXT),
        ("TURN", str(meta["turns"]), TEXT),
        ("SEED", str(meta["seed"]), PLAYER),
    ]

    if meta.get("weapon"):
        stats.append(("WIELDING", meta["weapon"].removeprefix("a ").removeprefix("an "), GEAR))

    chips = [
        ("PERMADEATH", BAD),
        ("SEEDED RUNS", PLAYER),
        ("SHADOWCASTING", WARN),
        (".NET 10", GOOD),
    ]

    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" '
        f'viewBox="0 0 {W} {H}" role="img" '
        f'aria-label="RogueBit, a turn-based ASCII roguelike. A dungeon floor with the '
        f'player, two goblins, a potion and a coin, beside a status panel.">',
        "<defs><style>",
        "@font-face{font-family:'RB';font-style:normal;font-weight:400;"
        f"src:url(data:font/woff2;base64,{font64}) format('woff2');}}",
        ".mono{font-family:'RB',ui-monospace,monospace;}",
        ".lbl{font-family:'RB',ui-monospace,monospace;font-size:11px;letter-spacing:1.6px;}",
        "</style></defs>",
        f'<rect width="{W}" height="{H}" fill="{BG}"/>',
    ]

    # A faint grid, because the whole game is cells.
    parts.append('<g opacity="0.055">')
    for gx in range(0, W, 20):
        parts.append(f'<line x1="{gx}" y1="0" x2="{gx}" y2="{H}" stroke="{PLAYER}" stroke-width="1"/>')
    for gy in range(0, H, 20):
        parts.append(f'<line x1="0" y1="{gy}" x2="{W}" y2="{gy}" stroke="{PLAYER}" stroke-width="1"/>')
    parts.append("</g>")

    # ---- left: identity ----
    parts.append(
        f'<text x="{LEFT_X}" y="60" class="mono" font-size="14" fill="{DIM}">'
        f'<tspan fill="{MONSTER}">$</tspan> ./RogueBit --seed {meta["seed"]}</text>'
    )
    parts.append(wordmark(LEFT_X, 88, 9))
    parts.append(
        f'<line x1="{LEFT_X}" y1="196" x2="{LEFT_X + 432}" y2="196" stroke="{EDGE}" stroke-width="2"/>'
    )
    parts.append(
        f'<text x="{LEFT_X}" y="230" class="mono" font-size="16" fill="{TEXT}">'
        f'A turn-based ASCII roguelike.</text>'
    )
    parts.append(
        f'<text x="{LEFT_X}" y="256" class="mono" font-size="16" fill="{DIM}">'
        f'The same seed always plays <tspan fill="{PLAYER}">the same run</tspan>.</text>'
    )

    # A key to the map on the right, so the banner teaches what it is showing.
    legend = [
        ("@", "you", PLAYER),
        ("g", "goblin", MONSTER),
        ("$", "coin", COIN),
        ("!", "potion", POTION),
        ("+", "door", DOOR),
        ("^", "trap", TRAP),
        (">", "stairs", STAIRS),
    ]
    legend_x = LEFT_X
    for glyph, label, colour in legend:
        parts.append(
            f'<text x="{legend_x:.1f}" y="304" class="mono" font-size="15" fill="{colour}">'
            f'{escape(glyph)}</text>'
        )
        parts.append(
            f'<text x="{legend_x + 15:.1f}" y="304" class="mono" font-size="12" fill="{DIM}">'
            f'{label}</text>'
        )
        legend_x += 15 + len(label) * 7.3 + 16

    chip_y = 348
    chip_x = LEFT_X
    for label, colour in chips:
        width = len(label) * 7.2 + 26
        parts.append(
            f'<rect x="{chip_x:.1f}" y="{chip_y}" width="{width:.1f}" height="26" rx="3" '
            f'fill="none" stroke="{colour}" stroke-opacity="0.42" stroke-width="1"/>'
        )
        parts.append(
            f'<text x="{chip_x + width / 2:.1f}" y="{chip_y + 17}" class="lbl" '
            f'fill="{colour}" text-anchor="middle">{label}</text>'
        )
        chip_x += width + 10

    parts.append(
        f'<text x="{LEFT_X}" y="437" class="mono" font-size="12" fill="{DIM}">'
        f'v{version()}  \u00b7  MIT  \u00b7  {TESTS} tests  \u00b7  the core does no drawing</text>'
    )

    # ---- right: the game ----
    panel_x = SPLIT
    parts.append(
        f'<rect x="{panel_x}" y="40" width="{W - panel_x - 40}" height="{H - 80}" rx="4" '
        f'fill="{PANEL}" stroke="{EDGE}" stroke-width="1"/>'
    )
    focus_col, focus_row = player_cell()
    focus_x = map_x + (focus_col + 0.5) * CELL_W
    focus_y = map_y + (focus_row - 0.3) * CELL_H
    reach = max(map_w, rows_count * CELL_H) * 0.72

    parts.append(
        f'<radialGradient id="focus" gradientUnits="userSpaceOnUse" '
        f'cx="{focus_x:.1f}" cy="{focus_y:.1f}" r="{reach:.1f}">'
        f'<stop offset="0.34" stop-color="#fff" stop-opacity="1"/>'
        f'<stop offset="0.72" stop-color="#fff" stop-opacity="0.62"/>'
        f'<stop offset="1" stop-color="#fff" stop-opacity="0.16"/>'
        f'</radialGradient>'
        f'<mask id="focusMask">'
        f'<rect x="{SPLIT}" y="{panel_top}" width="{W - SPLIT - 40}" height="{panel_h}" fill="url(#focus)"/>'
        f'</mask>'
    )
    parts.append(f'<g mask="url(#focusMask)">{map_rows(map_x, map_y)}</g>')

    # ---- HUD ----
    parts.append(
        f'<line x1="{hud_x - 16:.1f}" y1="60" x2="{hud_x - 16:.1f}" y2="{H - 60}" '
        f'stroke="{EDGE}" stroke-width="1"/>'
    )
    parts.append(
        f'<text x="{hud_x:.1f}" y="76" class="lbl" fill="{PLAYER}">ROGUEBIT v{version()}</text>'
    )

    row_y = 112
    for label, value, colour in stats:
        parts.append(f'<text x="{hud_x:.1f}" y="{row_y}" class="lbl" fill="{DIM}">{label}</text>')
        parts.append(
            f'<text x="{hud_x + 168:.1f}" y="{row_y}" class="mono" font-size="14" '
            f'fill="{colour}" text-anchor="end">{escape(value)}</text>'
        )
        row_y += 25

    parts.append(
        f'<line x1="{hud_x:.1f}" y1="{row_y - 8}" x2="{hud_x + 168:.1f}" y2="{row_y - 8}" '
        f'stroke="{EDGE}" stroke-width="1"/>'
    )

    log_y = row_y + 22
    log_size = 11.0
    log_chars = int((hud_w - log_size * 0.602 * 2) / (log_size * 0.602))
    for line in meta["log"][-6:]:
        text = line["text"]
        if len(text) > log_chars:
            text = text[: log_chars - 1].rstrip() + "\u2026"
        parts.append(
            f'<text x="{hud_x:.1f}" y="{log_y}" class="mono" font-size="{log_size}" '
            f'fill="{LOG_COLOUR.get(line["kind"], TEXT)}">'
            f'<tspan fill="{DIM}">&gt;</tspan> {escape(text)}</text>'
        )
        log_y += 18

    parts.append("</svg>")
    return "\n".join(parts)


if __name__ == "__main__":
    if "--refresh-font" in sys.argv:
        refresh_font(ROOT / "assets" / "banner-font.woff2")

    svg = build()
    out = ROOT / "assets" / "banner.svg"
    out.write_text(svg)
    print(f"wrote {out} ({len(svg) / 1024:.1f} KB)")
