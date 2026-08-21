namespace RogueBit.Console;

using RogueBit.Core;
using RogueBit.Core.Saves;

/// <summary>
/// What the player is told at the top of a run about the last one on the same
/// seed.
///
/// This is the half of the record that faces the player before they play
/// rather than after. A seed that replays the same run is only worth replaying
/// if the game can say what happened the last time, and until the record
/// existed there was nothing to say it with.
/// </summary>
public static class StartOfRun
{
    /// <summary>How the last run on this seed went, in one line.</summary>
    public static string LastTime(FinishedRun last)
    {
        ArgumentNullException.ThrowIfNull(last);

        // The deepest floor reached rather than the one it ended on, which is
        // what the score was paid for and what the panel at the end says.
        return last.Won
            ? $"Last time on this seed you reached the bottom, with {last.Score} points."
            : $"Last time on this seed you got to floor {last.DeepestDepth}, with {last.Score} points.";
    }

    /// <summary>
    /// Puts that line into the log, if there is one to put. A seed nobody has
    /// finished says nothing at all rather than saying it is new, because the
    /// log is what happened and not a greeting.
    /// </summary>
    public static void Announce(Run run, RunHistory history)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(history);

        if (history.LastOn(run.Seed) is not { } last) return;

        run.Log.Add(LastTime(last), MessageKind.Normal);
    }
}
