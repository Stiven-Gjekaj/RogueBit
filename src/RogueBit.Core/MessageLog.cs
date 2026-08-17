namespace RogueBit.Core;

/// <summary>How loudly a message should be shown.</summary>
public enum MessageKind
{
    Normal,
    Good,
    Bad,
    Warning,
}

/// <summary>One line of the log, and how many times it has just repeated.</summary>
public sealed record LogMessage(string Text, MessageKind Kind)
{
    public int Count { get; internal set; } = 1;

    /// <summary>The text as it is shown, with a run length when it repeats.</summary>
    public string Display => Count > 1 ? $"{Text} (x{Count})" : Text;
}

/// <summary>
/// What has just happened, in the player's words.
///
/// A repeated line is counted rather than added again, so twenty misses in a
/// row do not push everything else off the panel.
/// </summary>
public sealed class MessageLog
{
    private readonly List<LogMessage> messages = [];
    private readonly int capacity;

    public MessageLog(int capacity = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        this.capacity = capacity;
    }

    public IReadOnlyList<LogMessage> Messages => messages;

    public int Count => messages.Count;

    public void Add(string text, MessageKind kind = MessageKind.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (messages.Count > 0)
        {
            LogMessage last = messages[^1];
            if (last.Text == text && last.Kind == kind)
            {
                last.Count++;
                return;
            }
        }

        messages.Add(new LogMessage(text, kind));

        if (messages.Count > capacity) messages.RemoveAt(0);
    }

    /// <summary>The last <paramref name="count"/> lines, oldest first.</summary>
    public IReadOnlyList<LogMessage> Latest(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return messages.Count <= count ? messages : messages[^count..];
    }

    public void Clear() => messages.Clear();
}
