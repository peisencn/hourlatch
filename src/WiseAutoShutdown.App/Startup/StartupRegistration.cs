using System.Security;
using Microsoft.Win32;

namespace WiseAutoShutdown.Startup;

public sealed record StartupRegistrationResult(bool Succeeded, string? ErrorMessage = null)
{
    public static StartupRegistrationResult Success() => new(true);

    public static StartupRegistrationResult Failure(Exception exception) =>
        new(false, exception.Message);
}

public sealed record StartupQueryResult(bool Succeeded, bool Enabled, string? ErrorMessage = null);

public sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WiseAutoShutdown";
    private readonly string _command;

    public StartupRegistration(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _command = $"\"{Path.GetFullPath(executablePath)}\"";
    }

    public StartupQueryResult IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(ValueName) as string;
            return new StartupQueryResult(
                true,
                string.Equals(value, _command, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (IsExpectedRegistryFailure(exception))
        {
            return new StartupQueryResult(false, false, exception.Message);
        }
    }

    public StartupRegistrationResult Enable() => ChangeRegistration(enable: true);

    public StartupRegistrationResult Disable() => ChangeRegistration(enable: false);

    private StartupRegistrationResult ChangeRegistration(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enable)
            {
                key.SetValue(ValueName, _command, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return StartupRegistrationResult.Success();
        }
        catch (Exception exception) when (IsExpectedRegistryFailure(exception))
        {
            return StartupRegistrationResult.Failure(exception);
        }
    }

    private static bool IsExpectedRegistryFailure(Exception exception) =>
        exception is UnauthorizedAccessException or SecurityException or IOException;
}
