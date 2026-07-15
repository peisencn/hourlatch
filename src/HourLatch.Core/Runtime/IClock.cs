namespace HourLatch.Core.Runtime;

public interface IClock
{
    DateTimeOffset Now { get; }
}
