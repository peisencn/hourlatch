namespace HourLatch.Core.Runtime;

public enum RestrictionTrigger
{
    Startup,
    Periodic,
    WindowBoundary,
    SessionUnlock,
    Resume,
    SettingsChanged,
    SystemTimeChanged,
    OverrideExpired
}
