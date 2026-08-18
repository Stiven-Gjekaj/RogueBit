namespace RogueBit.Console;

using RogueBit.Core;
using RogueBit.Core.Map;

/// <summary>
/// Prints a floor as text, without opening a window.
///
/// The floor is reached by playing down to it rather than by asking a
/// generator for floor three directly. Floors are drawn from one source of
/// chance in order, so floor three of a seed is only floor three if floors one
/// and two were built first. Anything that skipped them would print a floor no
/// run ever walks on, which is worse than useless in a bug report.
/// </summary>
public static class FloorPrinter
{
    /// <summary>Every row of the floor a run reaches at this depth.</summary>
    public static string[] Render(int seed, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);

        Run run = new(seed);

        while (run.Depth < depth)
        {
            run.Player.Position = run.Map.StairsDown;

            if (run.TakeStairs() != ActionResult.Took)
            {
                throw new InvalidOperationException($"Floor {run.Depth} of seed {seed} has no way down.");
            }
        }

        return MapText.Render(run.Map);
    }
}
