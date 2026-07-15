using HourLatch.Core.Scheduling;

namespace HourLatch.Core.Overrides;

public sealed record OverrideGrant(
    string WindowId,
    DateTimeOffset GrantedAt,
    DateTimeOffset ExpiresAt)
{
    public bool IsActive(RestrictionWindow window, DateTimeOffset now) =>
        WindowId == window.Id && now >= GrantedAt && now < ExpiresAt;
}
