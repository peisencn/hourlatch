using HourLatch.Core.Power;

namespace HourLatch.Diagnostics;

public sealed class LoggingPowerActionExecutor : IPowerActionExecutor
{
    private readonly IPowerActionExecutor _inner;
    private readonly LocalLog _log;

    public LoggingPowerActionExecutor(IPowerActionExecutor inner, LocalLog log)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public PowerActionResult Execute(RestrictionAction action)
    {
        try
        {
            var result = _inner.Execute(action);
            _log.Action(action.ToString(), result.Succeeded, result.ErrorCode);
            return result;
        }
        catch (Exception exception)
        {
            _log.Exception($"action_{action}", exception);
            return PowerActionResult.Failure("The Windows action failed unexpectedly.");
        }
    }
}
