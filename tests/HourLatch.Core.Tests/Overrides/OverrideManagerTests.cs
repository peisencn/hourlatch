using HourLatch.Core.Overrides;
using HourLatch.Core.Scheduling;
using Xunit;

namespace HourLatch.Core.Tests.Overrides;

public sealed class OverrideManagerTests
{
    private static readonly RestrictionWindow Window = new(
        DateTimeOffset.Parse("2026-07-13T23:00:00+00:00"),
        DateTimeOffset.Parse("2026-07-14T07:00:00+00:00"));

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public void Fixed_duration_grants_requested_minutes(int minutes)
    {
        var now = DateTimeOffset.Parse("2026-07-13T23:10:00+00:00");

        var grant = new OverrideManager().Create(Window, now, OverrideRequest.ForMinutes(minutes));

        Assert.Equal(now.AddMinutes(minutes), grant.ExpiresAt);
        Assert.True(grant.IsActive(Window, now));
    }

    [Fact]
    public void Fixed_duration_is_clipped_to_window_end()
    {
        var now = DateTimeOffset.Parse("2026-07-14T06:50:00+00:00");

        var grant = new OverrideManager().Create(Window, now, OverrideRequest.ForMinutes(30));

        Assert.Equal(Window.End, grant.ExpiresAt);
    }

    [Fact]
    public void Until_window_end_uses_window_end()
    {
        var now = DateTimeOffset.Parse("2026-07-14T01:00:00+00:00");

        var grant = new OverrideManager().Create(Window, now, OverrideRequest.UntilWindowEnd());

        Assert.Equal(Window.End, grant.ExpiresAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void Invalid_duration_is_rejected(int minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OverrideRequest.ForMinutes(minutes));
    }

    [Fact]
    public void Creation_outside_the_window_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new OverrideManager().Create(
            Window,
            Window.End,
            OverrideRequest.ForMinutes(15)));
    }

    [Fact]
    public void Grant_rejects_wrong_window_and_expiry()
    {
        var grant = new OverrideManager().Create(Window, Window.Start, OverrideRequest.ForMinutes(15));
        var anotherWindow = new RestrictionWindow(Window.Start.AddDays(1), Window.End.AddDays(1));

        Assert.False(grant.IsActive(anotherWindow, Window.Start));
        Assert.False(grant.IsActive(Window, grant.ExpiresAt));
    }

    [Fact]
    public void Repeated_grant_can_replace_previous_grant()
    {
        var manager = new OverrideManager();
        var first = manager.Create(Window, Window.Start, OverrideRequest.ForMinutes(15));
        var second = manager.Create(Window, Window.Start.AddMinutes(5), OverrideRequest.ForMinutes(60));

        Assert.NotEqual(first, second);
        Assert.True(second.ExpiresAt > first.ExpiresAt);
    }
}
