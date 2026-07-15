namespace WiseAutoShutdown.Core.Runtime;

public static class EvaluationTimerPolicy
{
    public static readonly TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(250);

    public static TimeSpan GetDelay(
        DateTimeOffset now,
        DateTimeOffset? target,
        TimeSpan fallback)
    {
        if (fallback <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(fallback));
        }

        if (target is null)
        {
            return fallback;
        }

        var remaining = target.Value - now;
        if (remaining <= MinimumDelay)
        {
            return MinimumDelay;
        }

        return remaining < fallback ? remaining : fallback;
    }
}
