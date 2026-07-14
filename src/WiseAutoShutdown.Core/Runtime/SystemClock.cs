namespace WiseAutoShutdown.Core.Runtime;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
