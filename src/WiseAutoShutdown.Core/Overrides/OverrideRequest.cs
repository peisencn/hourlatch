namespace WiseAutoShutdown.Core.Overrides;

public sealed record OverrideRequest
{
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(24);

    private OverrideRequest(TimeSpan? duration)
    {
        Duration = duration;
    }

    public TimeSpan? Duration { get; }

    public bool IsUntilWindowEnd => Duration is null;

    public static OverrideRequest ForMinutes(int minutes)
    {
        var duration = TimeSpan.FromMinutes(minutes);
        if (duration <= TimeSpan.Zero || duration > MaximumDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes));
        }

        return new OverrideRequest(duration);
    }

    public static OverrideRequest UntilWindowEnd() => new((TimeSpan?)null);
}

