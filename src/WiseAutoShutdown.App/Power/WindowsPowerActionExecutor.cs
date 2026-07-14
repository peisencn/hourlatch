using System.ComponentModel;
using System.Runtime.InteropServices;
using WiseAutoShutdown.Core.Power;

namespace WiseAutoShutdown.Power;

public sealed class WindowsPowerActionExecutor : IPowerActionExecutor
{
    public PowerActionResult Execute(RestrictionAction action) => action switch
    {
        RestrictionAction.Lock => LockWorkstation(),
        RestrictionAction.Sleep => SuspendComputer(),
        _ => PowerActionResult.Failure($"Unsupported restriction action: {action}.")
    };

    private static PowerActionResult LockWorkstation()
    {
        if (NativeMethods.LockWorkStation())
        {
            return PowerActionResult.Success();
        }

        var errorCode = Marshal.GetLastWin32Error();
        return PowerActionResult.Failure(new Win32Exception(errorCode).Message, errorCode);
    }

    private static PowerActionResult SuspendComputer()
    {
        return Application.SetSuspendState(PowerState.Suspend, force: false, disableWakeEvent: false)
            ? PowerActionResult.Success()
            : PowerActionResult.Failure("Windows rejected the suspend request.");
    }

    private static class NativeMethods
    {
        // LockWorkStation interop adapted from ShutdownTimerClassic (MIT), commit 37c955e.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LockWorkStation();
    }
}
