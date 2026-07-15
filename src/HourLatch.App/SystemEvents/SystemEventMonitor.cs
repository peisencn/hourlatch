using Microsoft.Win32;
using HourLatch.Core.Runtime;
using WindowsSystemEvents = Microsoft.Win32.SystemEvents;

namespace HourLatch.SystemEvents;

public sealed class SystemEventMonitor : IDisposable
{
    private bool _disposed;
    private bool _resumePending;

    public SystemEventMonitor()
    {
        WindowsSystemEvents.SessionSwitch += OnSessionSwitch;
        WindowsSystemEvents.PowerModeChanged += OnPowerModeChanged;
        WindowsSystemEvents.TimeChanged += OnTimeChanged;
    }

    public event Action<RestrictionTrigger>? EvaluationRequested;

    public bool IsSessionLocked { get; private set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        WindowsSystemEvents.SessionSwitch -= OnSessionSwitch;
        WindowsSystemEvents.PowerModeChanged -= OnPowerModeChanged;
        WindowsSystemEvents.TimeChanged -= OnTimeChanged;
        _disposed = true;
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs args)
    {
        if (args.Reason == SessionSwitchReason.SessionLock)
        {
            IsSessionLocked = true;
            return;
        }

        if (args.Reason != SessionSwitchReason.SessionUnlock)
        {
            return;
        }

        IsSessionLocked = false;
        var trigger = _resumePending ? RestrictionTrigger.Resume : RestrictionTrigger.SessionUnlock;
        _resumePending = false;
        EvaluationRequested?.Invoke(trigger);
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
    {
        if (args.Mode != PowerModes.Resume)
        {
            return;
        }

        if (IsSessionLocked)
        {
            _resumePending = true;
            return;
        }

        EvaluationRequested?.Invoke(RestrictionTrigger.Resume);
    }

    private void OnTimeChanged(object? sender, EventArgs args) =>
        EvaluationRequested?.Invoke(RestrictionTrigger.SystemTimeChanged);
}
