namespace WiseAutoShutdown.Core.Scheduling;

public sealed class DailyRestrictionSchedule(TimeOnly start, TimeOnly end)
{
    public TimeOnly Start { get; } = start;

    public TimeOnly End { get; } = end;

    public bool IsValid => Start != End;

    public RestrictionWindow? GetContainingWindow(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (!IsValid)
        {
            return null;
        }

        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var startDate = GetCandidateStartDate(localDate, TimeOnly.FromDateTime(localNow.DateTime));
        var window = CreateWindow(startDate, timeZone);

        return window.Contains(now) ? window : null;
    }

    public RestrictionWindow GetNextWindow(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (!IsValid)
        {
            throw new InvalidOperationException("The restriction schedule is invalid.");
        }

        var containingWindow = GetContainingWindow(now, timeZone);
        if (containingWindow is not null)
        {
            return containingWindow;
        }

        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        var nextStartDate = localTime < Start ? localDate : localDate.AddDays(1);

        return CreateWindow(nextStartDate, timeZone);
    }

    private DateOnly GetCandidateStartDate(DateOnly localDate, TimeOnly localTime)
    {
        if (Start < End || localTime >= Start)
        {
            return localDate;
        }

        return localTime < End ? localDate.AddDays(-1) : localDate;
    }

    private RestrictionWindow CreateWindow(DateOnly startDate, TimeZoneInfo timeZone)
    {
        var endDate = Start < End ? startDate : startDate.AddDays(1);
        return new RestrictionWindow(
            AtLocalTime(startDate, Start, timeZone),
            AtLocalTime(endDate, End, timeZone));
    }

    private static DateTimeOffset AtLocalTime(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var localDateTime = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
    }
}
