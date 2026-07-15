using HourLatch.Core.Configuration;
using HourLatch.Core.Security;
using Xunit;

namespace HourLatch.Core.Tests.Configuration;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void Enabled_settings_require_a_password()
    {
        var settings = AppSettings.CreateDefaults() with { Enabled = true, Password = null };

        var result = new SettingsValidator().Validate(settings);

        Assert.Contains(result.Errors, error => error.Code == "password_required");
    }

    [Theory]
    [InlineData("PBKDF2-SHA1", 210_000, "AAAAAAAAAAAAAAAAAAAAAA==", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256", 9_999, "AAAAAAAAAAAAAAAAAAAAAA==", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256", 1_000_001, "AAAAAAAAAAAAAAAAAAAAAA==", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256", 210_000, "not-base64", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256", 210_000, "AAAAAAAAAAAAAAAAAAAAAA==", "not-base64")]
    [InlineData("PBKDF2-SHA256", 210_000, null, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256", 210_000, "AAAAAAAAAAAAAAAAAAAAAA==", null)]
    [InlineData("PBKDF2-SHA256", 210_000, "AAAAAAAAAAAAAAAAAAAA", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256", 210_000, "AAAAAAAAAAAAAAAAAAAAAA==", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")]
    public void Enabled_settings_reject_invalid_password_records(
        string algorithm,
        int iterations,
        string? saltBase64,
        string? hashBase64)
    {
        var password = new PasswordHashRecord(algorithm, iterations, saltBase64!, hashBase64!);
        var settings = ValidSettings() with { Password = password };

        var result = new SettingsValidator().Validate(settings);

        Assert.Contains(result.Errors, error => error.Code == "password_invalid");
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
        Password = new PasswordHashRecord(
            "PBKDF2-SHA256",
            210_000,
            "AAAAAAAAAAAAAAAAAAAAAA==",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")
    };
}
