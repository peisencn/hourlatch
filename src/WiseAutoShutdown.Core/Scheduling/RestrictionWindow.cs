namespace WiseAutoShutdown.Core.Scheduling;

public sealed record RestrictionWindow(DateTimeOffset Start, DateTimeOffset End)
{
    public string Id => $"{Start.UtcDateTime.Ticks}:{End.UtcDateTime.Ticks}";

    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;
}
