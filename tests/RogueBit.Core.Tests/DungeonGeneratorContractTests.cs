using RogueBit.Core;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// The promises every generator makes, checked against each one over many
/// seeds. A generator that cannot keep these is not usable by the game, whatever
/// its shape looks like.
/// </summary>
public class DungeonGeneratorContractTests
{
    public static TheoryData<string> GeneratorNames() => ["rooms"];

    private static IDungeonGenerator Create(string name) => name switch
    {
        "rooms" => new BspDungeonGenerator(),
        _ => throw new ArgumentException($"No generator called {name}.", nameof(name)),
    };

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void LeavesExactlyOneWalkableRegion(string name)
    {
        IDungeonGenerator generator = Create(name);

        for (int seed = 0; seed < 40; seed++)
        {
            DungeonMap map = generator.Generate(60, 30, new SeededRandom(seed));

            List<List<Position>> regions = MapRegions.Find(map);
            Assert.True(
                regions.Count == 1,
                $"seed {seed} left {regions.Count} regions:\n{string.Join('\n', MapBuilder.Render(map))}");
        }
    }

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void PutsTheEntranceAndTheStairsOnWalkableGround(string name)
    {
        IDungeonGenerator generator = Create(name);

        for (int seed = 0; seed < 40; seed++)
        {
            DungeonMap map = generator.Generate(60, 30, new SeededRandom(seed));

            Assert.True(map.IsWalkable(map.Entrance), $"seed {seed}: the entrance is not walkable");
            Assert.True(map.IsWalkable(map.StairsDown), $"seed {seed}: the stairs are not walkable");
            Assert.Equal(TileKind.StairsDown, map[map.StairsDown]);
        }
    }

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void KeepsAWallAllTheWayRound(string name)
    {
        IDungeonGenerator generator = Create(name);

        for (int seed = 0; seed < 20; seed++)
        {
            DungeonMap map = generator.Generate(60, 30, new SeededRandom(seed));

            for (int x = 0; x < map.Width; x++)
            {
                Assert.False(map.IsWalkable(new Position(x, 0)), $"seed {seed}: the top row leaks");
                Assert.False(map.IsWalkable(new Position(x, map.Height - 1)), $"seed {seed}: the bottom row leaks");
            }

            for (int y = 0; y < map.Height; y++)
            {
                Assert.False(map.IsWalkable(new Position(0, y)), $"seed {seed}: the left column leaks");
                Assert.False(map.IsWalkable(new Position(map.Width - 1, y)), $"seed {seed}: the right column leaks");
            }
        }
    }

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void ReplaysTheSameFloorFromTheSameSeed(string name)
    {
        IDungeonGenerator generator = Create(name);

        for (int seed = 0; seed < 10; seed++)
        {
            DungeonMap first = generator.Generate(60, 30, new SeededRandom(seed));
            DungeonMap second = generator.Generate(60, 30, new SeededRandom(seed));

            Assert.Equal(MapBuilder.Render(first), MapBuilder.Render(second));
            Assert.Equal(first.Entrance, second.Entrance);
            Assert.Equal(first.StairsDown, second.StairsDown);
        }
    }

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void BuildsADifferentFloorFromADifferentSeed(string name)
    {
        IDungeonGenerator generator = Create(name);

        DungeonMap first = generator.Generate(60, 30, new SeededRandom(1));
        DungeonMap second = generator.Generate(60, 30, new SeededRandom(2));

        Assert.NotEqual(MapBuilder.Render(first), MapBuilder.Render(second));
    }

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void LeavesEnoughRoomToPlayIn(string name)
    {
        IDungeonGenerator generator = Create(name);

        for (int seed = 0; seed < 20; seed++)
        {
            DungeonMap map = generator.Generate(60, 30, new SeededRandom(seed));

            int walkable = map.CountWalkable();
            Assert.True(walkable >= 150, $"seed {seed} carved only {walkable} walkable cells");
        }
    }

    [Theory]
    [MemberData(nameof(GeneratorNames))]
    public void StillProducesAPlayableFloorAtAwkwardSizes(string name)
    {
        IDungeonGenerator generator = Create(name);

        foreach ((int width, int height) in new[] { (12, 8), (21, 11), (80, 24), (31, 41) })
        {
            DungeonMap map = generator.Generate(width, height, new SeededRandom(5));

            Assert.True(map.CountWalkable() > 0, $"{width}x{height} carved nothing");
            Assert.Single(MapRegions.Find(map));
            Assert.True(map.IsWalkable(map.Entrance));
        }
    }
}
