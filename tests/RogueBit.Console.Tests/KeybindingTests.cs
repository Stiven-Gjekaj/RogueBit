using RogueBit.Console;
using RogueBit.Core;
using SadConsole.Input;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// What the keyboard is allowed to look like.
///
/// The game reads the movement table first and stops at the first key that
/// matches. A key in both tables therefore does the moving and never the other
/// thing, and it does so in silence: nothing throws, nothing is logged, and the
/// only way to find out is to press the key and notice that nothing happened.
/// That is how S came to be documented as the key that saves for four months
/// while it walked south instead.
/// </summary>
public class KeybindingTests
{
    [Fact]
    public void NoKeyBothMovesAndDoesSomethingElse()
    {
        HashSet<Keys> moves = [.. Keybindings.Movement.Select(binding => binding.Key)];

        Keys[] both = [.. Keybindings.Actions.Select(binding => binding.Key).Where(moves.Contains)];

        Assert.Empty(both);
    }

    [Fact]
    public void NoKeyAsksForTwoDifferentActions()
    {
        Keys[] confused =
        [
            .. Keybindings.Actions
                .GroupBy(binding => binding.Key, binding => binding.Action)
                .Where(group => group.Distinct().Count() > 1)
                .Select(group => group.Key),
        ];

        Assert.Empty(confused);
    }

    [Fact]
    public void EveryDirectionCanBeWalked()
    {
        HashSet<Position> steps = [.. Keybindings.Movement.Select(binding => binding.Step)];

        Assert.Equal(Directions.All.Count, steps.Count);
        Assert.All(Directions.All, step => Assert.Contains(step, steps));
    }

    [Fact]
    public void EveryActionHasAKey()
    {
        HashSet<PlayAction> bound = [.. Keybindings.Actions.Select(binding => binding.Action)];

        Assert.All(Enum.GetValues<PlayAction>(), action => Assert.Contains(action, bound));
    }
}
