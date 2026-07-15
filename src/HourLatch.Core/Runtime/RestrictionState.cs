namespace HourLatch.Core.Runtime;

public enum RestrictionState
{
    Disabled,
    OutsideWindow,
    Warning,
    Restricted,
    OverrideActive,
    ActionFailed
}
