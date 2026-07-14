using WiseAutoShutdown.Core.Configuration;
using WiseAutoShutdown.Core.Security;
using Xunit;

namespace WiseAutoShutdown.Core.Tests.Configuration;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void Enabled_settings_require_a_password()
    {
        var settings = AppSettings.CreateDefaults() with { Enabled = true, Password = null };

        var result = new SettingsValidator().Validate(settings);

        Assert.Contains(result.Errors, error => error.Code == "password_required");
    }

    [Fact]
    public void Equal_start_and_end_is_invalid()
    {
        var settings = ValidSettings() with { EndTime = new TimeOnly(23, 0) };

        var result = new SettingsValidator().Validate(settings);

        Assert.Contains(result.Errors, error => error.Code == "schedule_invalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void Warning_seconds_must_be_in_supported_range(int seconds)
    {
        var result = new SettingsValidator().Validate(ValidSettings() with { WarningSeconds = seconds });

        Assert.Contains(result.Errors, error => error.Code == "warning_seconds_invalid");
    }

    [Fact]
    public void Valid_cross_midnight_settings_pass_validation()
    {
        var result = new SettingsValidator().Validate(ValidSettings());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static AppSettings ValidSettings() => AppSettings.CreateDefaults() with
    {
        Enabled = true,
        Password = new PasswordHashRecord("PBKDF2-SHA256", 210_000, "c2FsdA==", "aGFzaA==")
    };
}
