using RogueBit.Core;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// The one table of glyphs the window, the banner tool and the print command
/// all read. A tile missing from it is drawn as a wall in all three at once,
/// which is what happened to the door and the stairs up before it existed.
/// </summary>
public class MapTextTests
{
    [Theory]
    [InlineData(TileKind.Wall, '#')]
    [InlineData(TileKind.Floor, '.')]
    [InlineData(TileKind.StairsDown, '>')]
    [InlineData(TileKind.StairsUp, '<')]
    [InlineData(TileKind.Door, '+')]
    [InlineData(TileKind.TrapSprung, '^')]
    public void DrawsEachTileAsItsOwnCharacter(TileKind kind, char expected)
        => Assert.Equal(expected, MapText.Glyph(kind));

    [Fact]
    public void DrawsAnArmedTrapAsTheGroundItHidesUnder()
    {
        // A trap anybody can see is not a trap. This is the one tile that is
        // deliberately drawn as something else.
        Assert.Equal(MapText.Glyph(TileKind.Floor), MapText.Glyph(TileKind.TrapArmed));
    }

    [Fact]
    public void EveryTileButTheArmedTrapIsToldApartFromEveryOther()
    {
        List<TileKind> kinds = [.. Enum.GetValues<TileKind>().Where(k => k != TileKind.TrapArmed)];
        List<char> glyphs = [.. kinds.Select(MapText.Glyph)];

        Assert.Equal(kinds.Count, glyphs.Distinct().Count());
    }

    [Fact]
    public void RendersAFloorRowByRow()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#.+>#",
            "#<^.#",
            "#####");

        Assert.Equal(["#####", "#.+>#", "#<^.#", "#####"], MapText.Render(map));
    }
}
