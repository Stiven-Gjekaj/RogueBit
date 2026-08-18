using RogueBit.Console;
using RogueBit.Core;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// What the command line means. A mistyped option is reported rather than
/// dropped, because a player who is quietly given a different dungeon has no
/// way to work out why.
/// </summary>
public class OptionsTests
{
    private static Options Parse(params string[] arguments) => Options.Parse(arguments, out _);

    private static string? ErrorFrom(params string[] arguments)
    {
        Options.Parse(arguments, out string? error);
        return error;
    }

    [Fact]
    public void ReadsPrintFloorAndItsDepth()
    {
        Options options = Parse("--print-floor", "--seed", "4242", "--depth", "3");

        Assert.True(options.PrintFloor);
        Assert.Equal(4242, options.Seed);
        Assert.Equal(3, options.Depth);
        Assert.Null(ErrorFrom("--print-floor", "--seed", "4242", "--depth", "3"));
    }

    [Fact]
    public void PrintingWithoutADepthMeansTheFirstFloor()
    {
        Assert.Equal(1, Parse("--print-floor").Depth);
    }

    [Fact]
    public void ADepthOutsideTheDungeonIsReported()
    {
        Assert.NotNull(ErrorFrom("--print-floor", "--depth", "0"));
        Assert.NotNull(ErrorFrom("--print-floor", "--depth", $"{GameRules.FinalDepth + 1}"));
        Assert.Null(ErrorFrom("--print-floor", "--depth", $"{GameRules.FinalDepth}"));
    }

    [Fact]
    public void ADepthThatIsNotANumberIsReported()
    {
        Assert.NotNull(ErrorFrom("--print-floor", "--depth", "deep"));
    }

    [Fact]
    public void ADepthWithNothingAfterItIsReported()
    {
        Assert.NotNull(ErrorFrom("--print-floor", "--depth"));
    }

    [Fact]
    public void ADepthWithoutPrintFloorIsReported()
    {
        // Silently ignoring it would let somebody think they had asked to
        // start on floor five.
        Assert.NotNull(ErrorFrom("--depth", "5"));
    }

    [Fact]
    public void PrintingAndContinuingTogetherIsReported()
    {
        Assert.NotNull(ErrorFrom("--print-floor", "--continue"));
    }

    [Fact]
    public void AnOptionTheGameDoesNotKnowIsReported()
    {
        Assert.NotNull(ErrorFrom("--print-flor"));
    }

    [Fact]
    public void TheUsageTextMentionsEveryOptionItAccepts()
    {
        foreach (string option in new[] { "--seed", "--continue", "--print-floor", "--depth", "--colour-blind", "--no-effects", "--help" })
        {
            Assert.Contains(option, Options.Usage, StringComparison.Ordinal);
        }
    }
}
