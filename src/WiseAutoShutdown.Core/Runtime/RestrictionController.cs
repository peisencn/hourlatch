using WiseAutoShutdown.Core.Configuration;
using WiseAutoShutdown.Core.Scheduling;

namespace WiseAutoShutdown.Core.Runtime;

public sealed class RestrictionController
{
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromMinutes(5);
    private readonly RestrictionControllerDependencies _dependencies;
    private readonly TimeZoneInfo _timeZone;
    private bool _evaluationInProgress;
    private int _failureCount;

    public RestrictionController(
        RestrictionControllerDependencies dependencies,
        TimeZoneInfo timeZone)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        _timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
    }

    public RestrictionState State { get; private set; } = RestrictionState.Disabled;

    public DateTimeOffset? NextRetryAt { get; private set; }

    public void Evaluate(RestrictionTrigger trigger, bool sessionLocked)
    {
        if (_evaluationInProgress || ShouldWaitForRetry(trigger))
        {
            return;
        }

        _evaluationInProgress = true;
        try
        {
            EvaluateCore(trigger, sessionLocked);
        }
        finally
        {
            _evaluationInProgress = false;
        }
    }

    private void EvaluateCore(RestrictionTrigger trigger, bool sessionLocked)
    {
        var loadResult = _dependencies.SettingsStore.Load();
        var settings = loadResult.Settings;
        if (!loadResult.IsValid || !_dependencies.Validator.Validate(settings).IsValid || !settings.Enabled)
        {
            State = RestrictionState.Disabled;
            return;
        }

        var now = _dependencies.Clock.Now;
        var schedule = new DailyRestrictionSchedule(settings.StartTime, settings.EndTime);
        var window = schedule.GetContainingWindow(now, _timeZone);
        settings = ClearStaleGrant(settings, window, now);

        if (window is null)
        {
            ResetFailure(RestrictionState.OutsideWindow);
            return;
        }

        if (settings.ActiveOverride?.IsActive(window, now) == true)
        {
            ResetFailure(RestrictionState.OverrideActive);
            return;
        }

        if (trigger == RestrictionTrigger.Resume && sessionLocked)
        {
            return;
        }

        ShowPrompt(settings, window, now);
    }

    private AppSettings ClearStaleGrant(
        AppSettings settings,
        RestrictionWindow? window,
        DateTimeOffset now)
    {
        if (settings.ActiveOverride is null ||
            (window is not null && settings.ActiveOverride.IsActive(window, now)))
        {
            return settings;
        }

        var updated = settings with { ActiveOverride = null };
        _dependencies.SettingsStore.Save(updated);
        return updated;
    }

    private void ShowPrompt(AppSettings settings, RestrictionWindow window, DateTimeOffset now)
    {
        State = RestrictionState.Warning;
        var request = new PromptRequest(window, settings.Action, settings.WarningSeconds)
        {
            DefaultOverrideMinutes = settings.DefaultOverrideMinutes,
            AllowUntilWindowEnd = settings.AllowUntilWindowEnd,
            Password = settings.Password!
        };
        var result = _dependencies.Prompt.Show(request);

        if (result.Outcome == PromptOutcome.OverrideApproved)
        {
            PersistOverride(settings, window, now, result.OverrideRequest!);
            return;
        }

        ExecuteAction(settings);
    }

    private void PersistOverride(
        AppSettings settings,
        RestrictionWindow window,
        DateTimeOffset now,
        Overrides.OverrideRequest request)
    {
        var grant = _dependencies.OverrideManager.Create(window, now, request);
        _dependencies.SettingsStore.Save(settings with { ActiveOverride = grant });
        ResetFailure(RestrictionState.OverrideActive);
    }

    private void ExecuteAction(AppSettings settings)
    {
        var result = _dependencies.PowerExecutor.Execute(settings.Action);
        if (result.Succeeded)
        {
            ResetFailure(RestrictionState.Restricted);
            return;
        }

        _failureCount++;
        var delaySeconds = Math.Min(30 * Math.Pow(2, _failureCount - 1), MaximumRetryDelay.TotalSeconds);
        NextRetryAt = _dependencies.Clock.Now.AddSeconds(delaySeconds);
        State = RestrictionState.ActionFailed;
    }

    private bool ShouldWaitForRetry(RestrictionTrigger trigger) =>
        State == RestrictionState.ActionFailed &&
        trigger == RestrictionTrigger.Periodic &&
        NextRetryAt > _dependencies.Clock.Now;

    private void ResetFailure(RestrictionState state)
    {
        _failureCount = 0;
        NextRetryAt = null;
        State = state;
    }
}
