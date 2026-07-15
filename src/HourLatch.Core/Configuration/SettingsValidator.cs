namespace HourLatch.Core.Configuration;

public sealed record SettingsValidationError(string Code, string Message);

public sealed record SettingsValidationResult(IReadOnlyList<SettingsValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed class SettingsValidator
{
    private const int MaximumWarningSeconds = 300;
    private const int MaximumOverrideMinutes = 24 * 60;

    public SettingsValidationResult Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var errors = new List<SettingsValidationError>();
        AddScheduleError(settings, errors);
        AddPasswordError(settings, errors);
        AddRangeErrors(settings, errors);
        return new SettingsValidationResult(errors);
    }

    private static void AddScheduleError(AppSettings settings, List<SettingsValidationError> errors)
    {
        if (settings.StartTime == settings.EndTime)
        {
            errors.Add(new("schedule_invalid", "Start and end times must differ."));
        }
    }

    private static void AddPasswordError(AppSettings settings, List<SettingsValidationError> errors)
    {
        if (settings.Enabled && settings.Password is null)
        {
            errors.Add(new("password_required", "Enabled restrictions require a password."));
        }
    }

    private static void AddRangeErrors(AppSettings settings, List<SettingsValidationError> errors)
    {
        if (settings.WarningSeconds is < 1 or > MaximumWarningSeconds)
        {
            errors.Add(new("warning_seconds_invalid", "Warning seconds must be between 1 and 300."));
        }

        if (settings.DefaultOverrideMinutes is < 1 or > MaximumOverrideMinutes)
        {
            errors.Add(new("override_minutes_invalid", "Override minutes must be between 1 and 1440."));
        }
    }
}
