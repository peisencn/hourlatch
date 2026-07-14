namespace WiseAutoShutdown.Core.Power;

public interface IPowerActionExecutor
{
    PowerActionResult Execute(RestrictionAction action);
}
