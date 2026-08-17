namespace RogueBit.Core;

/// <summary>
/// The numbers that decide how long a run is and what ending it can have.
///
/// These live apart from the spawn tables because they are the shape of the
/// game rather than the difficulty of one floor.
/// </summary>
public static class GameRules
{
    /// <summary>
    /// The last floor. Its warden is what a run is trying to reach, and there
    /// are no stairs past it.
    /// </summary>
    public const int FinalDepth = 10;

    /// <summary>What reaching the end is worth, on top of the coins and the depth.</summary>
    public const int VictoryBonus = 250;

    /// <summary>True when this floor is the bottom of the dungeon.</summary>
    public static bool IsFinalDepth(int depth) => depth >= FinalDepth;
}
