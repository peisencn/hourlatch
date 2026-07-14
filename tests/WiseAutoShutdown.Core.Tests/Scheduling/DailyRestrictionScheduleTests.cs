using Xunit;
using WiseAutoShutdown.Core.Scheduling;

namespace WiseAutoShutdown.Core.Tests.Scheduling;

public sealed class DailyRestrictionScheduleTests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.Utc;

    [Theory]
    [InlineData("2026-07-13T23:00:00+00:00", true)]
    [InlineData("2026-07-13T23:30:00+00:00", true)]
    [InlineData("2026-07-14T06:59:59+00:00", true)]
    [InlineData("2026-07-14T07:00:00+00:00", false)]
    [InlineData("2026-07-13T22:59:59+00:00", false)]
    public void Cross_midnight_window_is_half_open(string instant, bool expected)
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(23, 0), new TimeOnly(7, 0));

        var window = schedule.GetContainingWindow(DateTimeOffset.Parse(instant), Zone);

        Assert.Equal(expected, window is not null);
    }

    [Theory]
    [InlineData("2026-07-13T09:00:00+00:00", true)]
    [InlineData("2026-07-13T12:00:00+00:00", true)]
    [InlineData("2026-07-13T16:59:59+00:00", true)]
    [InlineData("2026-07-13T17:00:00+00:00", false)]
    [InlineData("2026-07-13T08:59:59+00:00", false)]
    public void Same_day_window_is_half_open(string instant, bool expected)
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(9, 0), new TimeOnly(17, 0));

        var window = schedule.GetContainingWindow(DateTimeOffset.Parse(instant), Zone);

        Assert.Equal(expected, window is not null);
    }

    [Fact]
    public void Cross_midnight_window_uses_previous_day_before_end_time()
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(23, 0), new TimeOnly(7, 0));
        var now = DateTimeOffset.Parse("2026-07-14T01:00:00+00:00");

        var window = schedule.GetContainingWindow(now, Zone);

        Assert.NotNull(window);
        Assert.Equal(DateTimeOffset.Parse("2026-07-13T23:00:00+00:00"), window.Start);
        Assert.Equal(DateTimeOffset.Parse("2026-07-14T07:00:00+00:00"), window.End);
    }

    [Fact]
    public void Equal_start_and_end_is_invalid()
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(8, 0), new TimeOnly(8, 0));

        Assert.False(schedule.IsValid);
        Assert.Null(schedule.GetContainingWindow(DateTimeOffset.Parse("2026-07-13T08:00:00+00:00"), Zone));
    }

    [Fact]
    public void Next_window_is_today_when_start_is_still_ahead()
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(23, 0), new TimeOnly(7, 0));
        var now = DateTimeOffset.Parse("2026-07-13T22:00:00+00:00");

        var window = schedule.GetNextWindow(now, Zone);

        Assert.Equal(DateTimeOffset.Parse("2026-07-13T23:00:00+00:00"), window.Start);
        Assert.Equal(DateTimeOffset.Parse("2026-07-14T07:00:00+00:00"), window.End);
    }

    [Fact]
    public void Next_window_is_tomorrow_after_same_day_window_ends()
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var now = DateTimeOffset.Parse("2026-07-13T18:00:00+00:00");

        var window = schedule.GetNextWindow(now, Zone);

        Assert.Equal(DateTimeOffset.Parse("2026-07-14T09:00:00+00:00"), window.Start);
        Assert.Equal(DateTimeOffset.Parse("2026-07-14T17:00:00+00:00"), window.End);
    }
}

