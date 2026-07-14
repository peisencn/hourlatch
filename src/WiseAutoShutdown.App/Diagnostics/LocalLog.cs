using WiseAutoShutdown.Core.Runtime;

namespace WiseAutoShutdown.Diagnostics;

public sealed class LocalLog
{
    private const long MaximumLogBytes = 1024 * 1024;
    private readonly object _sync = new();
    private readonly string _currentPath;
    private readonly string _previousPath;

    public LocalLog(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        _currentPath = Path.Combine(directory, "app.log");
        _previousPath = Path.Combine(directory, "app.previous.log");
    }

    public static LocalLog CreateDefault() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WiseAutoShutdown",
        "logs"));

    public void Event(
        string eventType,
        RestrictionState? previousState = null,
        RestrictionState? currentState = null)
    {
        var state = previousState is null
            ? string.Empty
            : $" state={previousState}->{currentState}";
        TryWrite($"event={eventType}{state}");
    }

    public void Action(string action, bool succeeded, int? errorCode) =>
        TryWrite($"action={action} succeeded={succeeded} errorCode={errorCode?.ToString() ?? "none"}");

    public void Exception(string eventType, Exception exception) =>
        TryWrite($"event={eventType} exception={exception.GetType().FullName}");

    private void TryWrite(string entry)
    {
        try
        {
            lock (_sync)
            {
                RotateIfNeeded();
                File.AppendAllText(
                    _currentPath,
                    $"{DateTimeOffset.Now:O} {entry}{Environment.NewLine}");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Logging must never interrupt the restriction workflow.
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_currentPath) || new FileInfo(_currentPath).Length < MaximumLogBytes)
        {
            return;
        }

        File.Move(_currentPath, _previousPath, overwrite: true);
    }
}
