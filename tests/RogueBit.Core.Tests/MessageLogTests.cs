using RogueBit.Core;
using Xunit;

namespace RogueBit.Core.Tests;

public class MessageLogTests
{
    [Fact]
    public void KeepsWhatItIsGivenInOrder()
    {
        MessageLog log = new();

        log.Add("you hit the goblin");
        log.Add("the goblin dies");

        Assert.Equal(["you hit the goblin", "the goblin dies"], log.Messages.Select(m => m.Text));
    }

    [Fact]
    public void CountsARepeatedLineRatherThanAddingItAgain()
    {
        MessageLog log = new();

        log.Add("you miss");
        log.Add("you miss");
        log.Add("you miss");

        Assert.Single(log.Messages);
        Assert.Equal(3, log.Messages[0].Count);
        Assert.Equal("you miss (x3)", log.Messages[0].Display);
    }

    [Fact]
    public void ShowsNoRunLengthForALineSaidOnce()
    {
        MessageLog log = new();

        log.Add("you miss");

        Assert.Equal("you miss", log.Messages[0].Display);
    }

    [Fact]
    public void TheSameWordsAtADifferentPitchAreADifferentLine()
    {
        MessageLog log = new();

        log.Add("the door opens", MessageKind.Normal);
        log.Add("the door opens", MessageKind.Bad);

        Assert.Equal(2, log.Count);
    }

    [Fact]
    public void OnlyTheLineJustSaidIsCounted()
    {
        MessageLog log = new();

        log.Add("you miss");
        log.Add("the goblin hits you");
        log.Add("you miss");

        Assert.Equal(3, log.Count);
    }

    [Fact]
    public void DropsTheOldestLineOnceItIsFull()
    {
        MessageLog log = new(capacity: 3);

        for (int i = 0; i < 5; i++) log.Add($"line {i}");

        Assert.Equal(3, log.Count);
        Assert.Equal(["line 2", "line 3", "line 4"], log.Messages.Select(m => m.Text));
    }

    [Fact]
    public void LatestGivesTheEndOfTheLogOldestFirst()
    {
        MessageLog log = new();
        for (int i = 0; i < 10; i++) log.Add($"line {i}");

        Assert.Equal(["line 7", "line 8", "line 9"], log.Latest(3).Select(m => m.Text));
    }

    [Fact]
    public void LatestGivesEverythingWhenItIsAskedForMoreThanItHas()
    {
        MessageLog log = new();
        log.Add("only line");

        Assert.Single(log.Latest(50));
    }

    [Fact]
    public void RefusesAnEmptyLine()
    {
        MessageLog log = new();

        Assert.Throws<ArgumentException>(() => log.Add("   "));
    }
}
