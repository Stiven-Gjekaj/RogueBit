using RogueBit.Console;
using RogueBit.Core;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// Moving a window over the log. Every off-by-one here shows the reader a
/// blank page or hides the line they opened the panel to read.
/// </summary>
public class LogScrollTests
{
    private static IReadOnlyList<LogMessage> Lines(int count)
    {
        MessageLog log = new(capacity: 1000);

        for (int i = 0; i < count; i++) log.Add($"line {i}");

        return log.Messages;
    }

    [Fact]
    public void StartsOverTheNewestLines()
    {
        LogScroll scroll = new(pageSize: 3);
        IReadOnlyList<LogMessage> all = Lines(10);

        Assert.True(scroll.AtTheNewest);
        Assert.Equal(["line 7", "line 8", "line 9"], scroll.Page(all).Select(m => m.Text));
    }

    [Fact]
    public void AShortLogFillsLessThanAPageAndCannotScroll()
    {
        LogScroll scroll = new(pageSize: 6);
        IReadOnlyList<LogMessage> all = Lines(2);

        Assert.Equal(["line 0", "line 1"], scroll.Page(all).Select(m => m.Text));

        scroll.Older(5, all.Count);

        Assert.Equal(0, scroll.Offset);
        Assert.Equal(["line 0", "line 1"], scroll.Page(all).Select(m => m.Text));
    }

    [Fact]
    public void AnEmptyLogShowsNothingAndDoesNotThrow()
    {
        LogScroll scroll = new(pageSize: 6);

        Assert.Empty(scroll.Page(Lines(0)));
    }

    [Fact]
    public void GoingBackOneLineMovesTheWindowOneLine()
    {
        LogScroll scroll = new(pageSize: 3);
        IReadOnlyList<LogMessage> all = Lines(10);

        scroll.Older(1, all.Count);

        Assert.Equal(["line 6", "line 7", "line 8"], scroll.Page(all).Select(m => m.Text));
    }

    [Fact]
    public void GoingBackAPageMovesTheWindowAPage()
    {
        LogScroll scroll = new(pageSize: 3);
        IReadOnlyList<LogMessage> all = Lines(10);

        scroll.PageOlder(all.Count);

        Assert.Equal(["line 4", "line 5", "line 6"], scroll.Page(all).Select(m => m.Text));
    }

    [Fact]
    public void ItStopsAtTheOldestLineTheLogStillHolds()
    {
        LogScroll scroll = new(pageSize: 3);
        IReadOnlyList<LogMessage> all = Lines(10);

        scroll.Older(999, all.Count);

        Assert.True(scroll.AtTheOldest(all.Count));
        Assert.Equal(["line 0", "line 1", "line 2"], scroll.Page(all).Select(m => m.Text));
    }

    [Fact]
    public void ItStopsAtTheNewestLine()
    {
        LogScroll scroll = new(pageSize: 3);
        IReadOnlyList<LogMessage> all = Lines(10);

        scroll.ToTheOldest(all.Count);
        scroll.Newer(999, all.Count);

        Assert.True(scroll.AtTheNewest);
        Assert.Equal(["line 7", "line 8", "line 9"], scroll.Page(all).Select(m => m.Text));
    }

    [Fact]
    public void EveryPageIsFullExceptOnALogShorterThanOne()
    {
        LogScroll scroll = new(pageSize: 4);
        IReadOnlyList<LogMessage> all = Lines(10);

        for (int step = 0; step <= 10; step++)
        {
            Assert.Equal(4, scroll.Page(all).Count);
            scroll.Older(1, all.Count);
        }
    }

    [Fact]
    public void ItOpensOnTheNewestLinesWhateverTheLogHolds()
    {
        // Nought is the newest end, so the panel opens where the log is being
        // written without being told how long the log is.
        foreach (int count in new[] { 1, 3, 4, 40, 100 })
        {
            LogScroll scroll = new(pageSize: 4);
            IReadOnlyList<LogMessage> all = Lines(count);

            Assert.Equal($"line {count - 1}", scroll.Page(all)[^1].Text);
        }
    }

    [Fact]
    public void ThePageSizeHasToBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogScroll(0));
    }
}
