namespace RogueBit.Console;

using RogueBit.Core;

/// <summary>
/// What the panel at the end of a run says.
///
/// The words are worked out here rather than inside the drawing, because a
/// string built between two calls to Print cannot be read by anything except a
/// person looking at a window. The panel reported the wrong floor for as long
/// as there was a way back up, and the whole suite passed the entire time.
/// </summary>
public static class EndOfRun
{
    /// <summary>The lines of the panel, the headline first.</summary>
    /// <param name="run">The run that has just ended.</param>
    /// <param name="best">The best score on this machine, on any seed.</param>
    /// <param name="bestOnThisSeed">
    /// The best score on this seed before this run. Zero when the seed has not
    /// been finished before.
    /// </param>
    public static string[] Lines(Run run, int best, int bestOnThisSeed = 0)
    {
        ArgumentNullException.ThrowIfNull(run);

        return
        [
            run.HasWon ? "You reached the bottom and lived." : "You died.",

            // The deepest floor reached, not the floor the body is lying on.
            // The score is paid on the deepest, so a panel that named the
            // other one was telling the player their points were wrong.
            $"Floor {run.DeepestDepth}, {run.Turns} turns, {run.Score} points.",

            OnThisSeed(run, bestOnThisSeed),
            $"Best on any seed {best}.",
            "R to play the same seed again, Escape to leave.",
        ];
    }

    /// <summary>
    /// How this run stands against the same dungeon played before.
    ///
    /// This is the line the record was built for. R plays the same seed again,
    /// so the number worth putting next to that offer is the one from this
    /// seed rather than the best from some other dungeon on a lucky floor.
    /// </summary>
    private static string OnThisSeed(Run run, int bestOnThisSeed) => bestOnThisSeed switch
    {
        0 => $"Seed {run.Seed}, played through for the first time.",
        _ when run.Score > bestOnThisSeed => $"Your best on seed {run.Seed}, beating {bestOnThisSeed}.",
        _ => $"Your best on seed {run.Seed} is {bestOnThisSeed}.",
    };
}
