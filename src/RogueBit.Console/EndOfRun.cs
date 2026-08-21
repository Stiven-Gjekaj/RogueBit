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
    public static string[] Lines(Run run, int best)
    {
        ArgumentNullException.ThrowIfNull(run);

        return
        [
            run.HasWon ? "You reached the bottom and lived." : "You died.",

            // The deepest floor reached, not the floor the body is lying on.
            // The score is paid on the deepest, so a panel that named the
            // other one was telling the player their points were wrong.
            $"Floor {run.DeepestDepth}, {run.Turns} turns, {run.Score} points.",

            $"Best so far {best}.",
            "R to play the same seed again, Escape to leave.",
        ];
    }
}
