namespace HourLatch.Core.Configuration;

public interface ISettingsStore
{
    SettingsLoadResult Load();

    void Save(AppSettings settings);
}
