namespace RogueBit.Core.Map;

/// <summary>Carves one floor of the dungeon.</summary>
public interface IDungeonGenerator
{
    /// <summary>The name shown in the seed line, so a run says how it was built.</summary>
    string Name { get; }

    /// <summary>
    /// Carves a floor. The result is always one connected region, with an
    /// entrance and stairs down that the player can reach from it.
    /// </summary>
    DungeonMap Generate(int width, int height, SeededRandom random);
}
