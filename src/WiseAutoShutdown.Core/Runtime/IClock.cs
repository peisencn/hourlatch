namespace WiseAutoShutdown.Core.Runtime;

public interface IClock
{
    DateTimeOffset Now { get; }
}
