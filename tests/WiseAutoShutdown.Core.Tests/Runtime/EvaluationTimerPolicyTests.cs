using WiseAutoShutdown.Core.Runtime;
using Xunit;

namespace WiseAutoShutdown.Core.Tests.Runtime;

public sealed class EvaluationTimerPolicyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-15T15:00:00+00:00");
    private static readonly TimeSpan Fallback = TimeSpan.FromSeconds(30);

    [Fact]
    public void Uses_exact_target_when_it_is_before_fallback()
    {
        var delay = EvaluationTimerPolicy.GetDelay(Now, Now.AddSeconds(7), Fallback);

        Assert.Equal(TimeSpan.FromSeconds(7), delay);
    }

    [Fact]
    public void Caps_long_targets_at_periodic_fallback()
    {
        var delay = EvaluationTimerPolicy.GetDelay(Now, Now.AddMinutes(5), Fallback);

        Assert.Equal(Fallback, delay);
    }

    [Fact]
    public void Missing_target_uses_periodic_fallback()
    {
        Assert.Equal(Fallback, EvaluationTimerPolicy.GetDelay(Now, null, Fallback));
    }

    [Fact]
    public void Past_target_uses_minimum_non_busy_delay()
    {
        var delay = EvaluationTimerPolicy.GetDelay(Now, Now.AddSeconds(-1), Fallback);

        Assert.Equal(TimeSpan.FromMilliseconds(250), delay);
    }
}
