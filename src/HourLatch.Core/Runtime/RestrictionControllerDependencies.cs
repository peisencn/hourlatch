using HourLatch.Core.Configuration;
using HourLatch.Core.Overrides;
using HourLatch.Core.Power;

namespace HourLatch.Core.Runtime;

public sealed class RestrictionControllerDependencies
{
    public required IClock Clock { get; init; }
    public required ISettingsStore SettingsStore { get; init; }
    public required SettingsValidator Validator { get; init; }
    public required IRestrictionPrompt Prompt { get; init; }
    public required IPowerActionExecutor PowerExecutor { get; init; }
    public required OverrideManager OverrideManager { get; init; }
}
