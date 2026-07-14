using WiseAutoShutdown.Core.Scheduling;

namespace WiseAutoShutdown.Core.Overrides;

public sealed class OverrideManager
{
    public OverrideGrant Create(
        RestrictionWindow window,
        DateTimeOffset now,
        OverrideRequest request)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(request);

        if (!window.Contains(now))
        {
            throw new InvalidOperationException("Overrides can only be granted inside the active window.");
        }

        var requestedExpiry = request.IsUntilWindowEnd
            ? window.End
            : now.Add(request.Duration!.Value);
        var expiresAt = requestedExpiry < window.End ? requestedExpiry : window.End;

        return new OverrideGrant(window.Id, now, expiresAt);
    }
}
