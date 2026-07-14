using WiseAutoShutdown.Core.Configuration;
using WiseAutoShutdown.Core.Power;
using WiseAutoShutdown.Core.Security;
using Xunit;

namespace WiseAutoShutdown.Core.Tests.Configuration;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"wise-auto-shutdown-{Guid.NewGuid():N}");
    private readonly string _path;

    public JsonSettingsStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "settings.json");
    }

    [Fact]
    public void Missing_file_loads_valid_disabled_defaults()
    {
        var result = CreateStore().Load();

        Assert.True(result.IsValid);
        Assert.False(result.Settings.Enabled);
        Assert.Equal(new TimeOnly(23, 0), result.Settings.StartTime);
        Assert.Equal(new TimeOnly(7, 0), result.Settings.EndTime);
    }

    [Fact]
    public void Settings_round_trip_through_json()
    {
        var settings = AppSettings.CreateDefaults() with
        {
            Enabled = true,
            Action = RestrictionAction.Sleep,
            Password = new PasswordHashRecord("PBKDF2-SHA256", 210_000, "c2FsdA==", "aGFzaA==")
        };
        var store = CreateStore();

        store.Save(settings);
        var result = store.Load();

        Assert.True(result.IsValid);
        Assert.Equal(settings, result.Settings);
        Assert.False(File.Exists(_path + ".tmp"));
    }

    [Fact]
    public void Corrupted_json_disables_actions_without_overwriting_source()
    {
        const string brokenJson = "{ definitely not json";
        File.WriteAllText(_path, brokenJson);

        var result = CreateStore().Load();

        Assert.False(result.IsValid);
        Assert.False(result.Settings.Enabled);
        Assert.Contains(result.Errors, error => error.Code == "settings_corrupted");
        Assert.Equal(brokenJson, File.ReadAllText(_path));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private JsonSettingsStore CreateStore() => new(_path, new SettingsValidator());
}
