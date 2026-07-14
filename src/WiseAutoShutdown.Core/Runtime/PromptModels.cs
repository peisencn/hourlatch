using WiseAutoShutdown.Core.Overrides;
using WiseAutoShutdown.Core.Power;
using WiseAutoShutdown.Core.Scheduling;
using WiseAutoShutdown.Core.Security;

namespace WiseAutoShutdown.Core.Runtime;

public sealed record PromptRequest(
    RestrictionWindow Window,
    RestrictionAction Action,
    int WarningSeconds)
{
    public int DefaultOverrideMinutes { get; init; }
    public bool AllowUntilWindowEnd { get; init; }
    public required PasswordHashRecord Password { get; init; }
}

public enum PromptOutcome
{
    TimedOut,
    ExecuteNow,
    OverrideApproved
}

public sealed record PromptResult(PromptOutcome Outcome, OverrideRequest? OverrideRequest = null)
{
    public static PromptResult TimedOut() => new(PromptOutcome.TimedOut);

    public static PromptResult ExecuteNow() => new(PromptOutcome.ExecuteNow);

    public static PromptResult OverrideApproved(OverrideRequest request) =>
        new(PromptOutcome.OverrideApproved, request ?? throw new ArgumentNullException(nameof(request)));
}
