namespace HourLatch.Core.Power;

public interface IPowerActionExecutor
{
    PowerActionResult Execute(RestrictionAction action);
}
