using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseAutoShutdown.Core.Configuration;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly string _path;
    private readonly SettingsValidator _validator;

    public JsonSettingsStore(string path, SettingsValidator validator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public SettingsLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return ValidResult(AppSettings.CreateDefaults());
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), JsonOptions);
            return settings is null ? CorruptedResult() : ValidateLoaded(settings);
        }
        catch (JsonException)
        {
            return CorruptedResult();
        }
        catch (NotSupportedException)
        {
            return CorruptedResult();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var validation = _validator.Validate(settings);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Cannot save invalid settings.");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
        Directory.CreateDirectory(directory!);
        var tempPath = _path + ".tmp";

        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, settings, JsonOptions);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, _path, overwrite: true);
    }

    private SettingsLoadResult ValidateLoaded(AppSettings settings)
    {
        var validation = _validator.Validate(settings);
        return validation.IsValid
            ? ValidResult(settings)
            : new SettingsLoadResult(false, settings with { Enabled = false }, validation.Errors);
    }

    private static SettingsLoadResult ValidResult(AppSettings settings) =>
        new(true, settings, Array.Empty<SettingsValidationError>());

    private static SettingsLoadResult CorruptedResult() => new(
        false,
        AppSettings.CreateDefaults(),
        new[] { new SettingsValidationError("settings_corrupted", "Settings JSON is invalid.") });

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
