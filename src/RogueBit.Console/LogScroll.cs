namespace RogueBit.Console;

using RogueBit.Core;

/// <summary>
/// Where the window onto the message log is sitting.
///
/// The arithmetic lives here rather than inside the drawing, because it is the
/// part that can be wrong. An off-by-one at either end shows a blank page or
/// hides the newest line, and neither of those is obvious in a screenshot,
/// while both are plain in a test.
///
/// The offset is counted back from the newest line, so nought is where the log
/// is still being written. That is where a reader wants to start, and it makes
/// the panel open on the newest lines without knowing how many there are.
///
/// Nothing happens in the game while the panel is open, so the log does not
/// grow underneath a reader. A window counted from this end would slide if it
/// did.
/// </summary>
public sealed class LogScroll(int pageSize)
{
    private readonly int pageSize = pageSize > 0
        ? pageSize
        : throw new ArgumentOutOfRangeException(nameof(pageSize));

    /// <summary>How many lines above the newest the bottom of the window is.</summary>
    public int Offset { get; private set; }

    /// <summary>True when the window is over the newest line.</summary>
    public bool AtTheNewest => Offset == 0;

    /// <summary>True when the window cannot go back any further.</summary>
    public bool AtTheOldest(int total) => Offset >= Furthest(total);

    /// <summary>Slides the window back towards the older lines.</summary>
    public void Older(int lines, int total) => Offset = Math.Clamp(Offset + lines, 0, Furthest(total));

    /// <summary>Slides the window towards the newest lines.</summary>
    public void Newer(int lines, int total) => Offset = Math.Clamp(Offset - lines, 0, Furthest(total));

    /// <summary>A whole page back.</summary>
    public void PageOlder(int total) => Older(pageSize, total);

    /// <summary>A whole page forward.</summary>
    public void PageNewer(int total) => Newer(pageSize, total);

    /// <summary>All the way back to the first line the log still holds.</summary>
    public void ToTheOldest(int total) => Offset = Furthest(total);

    /// <summary>Back to where the log is still being written.</summary>
    public void ToTheNewest() => Offset = 0;

    /// <summary>The lines the window is over, oldest first.</summary>
    public IReadOnlyList<LogMessage> Page(IReadOnlyList<LogMessage> all)
    {
        ArgumentNullException.ThrowIfNull(all);

        int end = all.Count - Math.Clamp(Offset, 0, Furthest(all.Count));
        int start = Math.Max(0, end - pageSize);

        LogMessage[] page = new LogMessage[end - start];
        for (int i = 0; i < page.Length; i++) page[i] = all[start + i];

        return page;
    }

    /// <summary>
    /// The furthest back the window can go. A log shorter than one page cannot
    /// scroll at all, which is why this is never negative.
    /// </summary>
    private int Furthest(int total) => Math.Max(0, total - pageSize);
}
