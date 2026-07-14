using WiseAutoShutdown.Core.Power;
using WiseAutoShutdown.Core.Security;

namespace WiseAutoShutdown.Core.Configuration;

public sealed record AppSettings
{
    public bool Enabled { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public RestrictionAction Action { get; init; }
    public int WarningSeconds { get; init; }
    public int DefaultOverrideMinutes { get; init; }
    public bool AllowUntilWindowEnd { get; init; }
    public bool AutoStart { get; init; }
    public PasswordHashRecord? Password { get; init; }

    public static AppSettings CreateDefaults() => new()
    {
        Enabled = false,
        StartTime = new TimeOnly(23, 0),
        EndTime = new TimeOnly(7, 0),
        Action = RestrictionAction.Lock,
        WarningSeconds = 60,
        DefaultOverrideMinutes = 30,
        AllowUntilWindowEnd = true,
        AutoStart = false
    };
}
