namespace WiseAutoShutdown.Core.Configuration;

public sealed record SettingsLoadResult(
    bool IsValid,
    AppSettings Settings,
    IReadOnlyList<SettingsValidationError> Errors);
