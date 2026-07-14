using WiseAutoShutdown.Core.Configuration;
using WiseAutoShutdown.Core.Overrides;
using WiseAutoShutdown.Core.Power;
using WiseAutoShutdown.Core.Runtime;
using WiseAutoShutdown.Core.Scheduling;
using WiseAutoShutdown.Core.Security;
using Xunit;

namespace WiseAutoShutdown.Core.Tests.Runtime;

public sealed class RestrictionControllerTests
{
    [Fact]
    public void Disabled_settings_do_nothing()
    {
        var harness = ControllerHarness.Create(enabled: false);

        harness.Controller.Evaluate(RestrictionTrigger.Startup, sessionLocked: false);

        Assert.Equal(RestrictionState.Disabled, harness.Controller.State);
        Assert.Empty(harness.Prompt.Requests);
        Assert.Empty(harness.Power.Actions);
    }

    [Fact]
    public void Outside_window_does_nothing()
    {
        var harness = ControllerHarness.Create(now: "2026-07-13T12:00:00+00:00");

        harness.Controller.Evaluate(RestrictionTrigger.Periodic, sessionLocked: false);

        Assert.Equal(RestrictionState.OutsideWindow, harness.Controller.State);
        Assert.Empty(harness.Prompt.Requests);
    }

    [Fact]
    public void Active_override_suppresses_prompt_and_action()
    {
        var harness = ControllerHarness.Create();
        harness.Store.Settings = harness.Store.Settings with
        {
            ActiveOverride = ActiveGrant(harness.Clock.Now, harness.Clock.Now.AddMinutes(30))
        };

        harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);

        Assert.Equal(RestrictionState.OverrideActive, harness.Controller.State);
        Assert.Empty(harness.Prompt.Requests);
        Assert.Empty(harness.Power.Actions);
    }

    [Fact]
    public void Unlock_inside_window_executes_action_after_prompt_timeout()
    {
        var harness = ControllerHarness.Create(PromptResult.TimedOut());

        harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);

        Assert.Equal(RestrictionState.Restricted, harness.Controller.State);
        Assert.Single(harness.Power.Actions);
    }

    [Fact]
    public void Approved_override_is_persisted()
    {
        var harness = ControllerHarness.Create(PromptResult.OverrideApproved(OverrideRequest.ForMinutes(30)));

        harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);

        Assert.Equal(RestrictionState.OverrideActive, harness.Controller.State);
        Assert.NotNull(harness.Store.Settings.ActiveOverride);
        Assert.Empty(harness.Power.Actions);
    }

    [Fact]
    public void Reentrant_evaluation_does_not_show_duplicate_prompt()
    {
        var harness = ControllerHarness.Create(PromptResult.TimedOut());
        harness.Prompt.OnShow = () =>
            harness.Controller.Evaluate(RestrictionTrigger.Periodic, sessionLocked: false);

        harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);

        Assert.Single(harness.Prompt.Requests);
        Assert.Single(harness.Power.Actions);
    }

    [Fact]
    public void Resume_while_locked_is_deferred()
    {
        var harness = ControllerHarness.Create(PromptResult.TimedOut());
        harness.Controller.Evaluate(RestrictionTrigger.Startup, sessionLocked: false);
        harness.Prompt.Requests.Clear();
        harness.Power.Actions.Clear();

        harness.Controller.Evaluate(RestrictionTrigger.Resume, sessionLocked: true);

        Assert.Empty(harness.Prompt.Requests);
        Assert.Empty(harness.Power.Actions);
    }

    [Fact]
    public void Failed_action_sets_failure_state_and_bounded_retry()
    {
        var harness = ControllerHarness.Create(PromptResult.ExecuteNow());
        harness.Power.Result = PowerActionResult.Failure("failed", 5);

        harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);

        Assert.Equal(RestrictionState.ActionFailed, harness.Controller.State);
        Assert.InRange(
            harness.Controller.NextRetryAt!.Value,
            harness.Clock.Now.AddSeconds(30),
            harness.Clock.Now.AddMinutes(5));
    }

    [Fact]
    public void Expired_grant_is_cleared_before_restriction()
    {
        var harness = ControllerHarness.Create(PromptResult.TimedOut());
        harness.Store.Settings = harness.Store.Settings with
        {
            ActiveOverride = ActiveGrant(harness.Clock.Now.AddMinutes(-5), harness.Clock.Now)
        };

        harness.Controller.Evaluate(RestrictionTrigger.OverrideExpired, sessionLocked: false);

        Assert.Null(harness.Store.Settings.ActiveOverride);
        Assert.Single(harness.Power.Actions);
    }

    [Fact]
    public void Periodic_evaluation_waits_until_failure_retry_time()
    {
        var harness = ControllerHarness.Create(PromptResult.ExecuteNow());
        harness.Power.Result = PowerActionResult.Failure("failed");
        harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);
        harness.Prompt.Requests.Clear();
        harness.Power.Actions.Clear();

        harness.Controller.Evaluate(RestrictionTrigger.Periodic, sessionLocked: false);

        Assert.Empty(harness.Prompt.Requests);
        Assert.Empty(harness.Power.Actions);
    }
    private static OverrideGrant ActiveGrant(DateTimeOffset grantedAt, DateTimeOffset expiresAt)
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(23, 0), new TimeOnly(7, 0));
        var window = schedule.GetContainingWindow(grantedAt, TimeZoneInfo.Utc)!;
        return new OverrideGrant(window.Id, grantedAt, expiresAt);
    }

    private sealed class ControllerHarness
    {
        private ControllerHarness(PromptResult promptResult, DateTimeOffset now, bool enabled)
        {
            Clock = new FakeClock { Now = now };
            Store = new MemorySettingsStore { Settings = ValidSettings(enabled) };
            Prompt = new FakePrompt { Result = promptResult };
            Power = new FakePowerExecutor();
            var dependencies = new RestrictionControllerDependencies
            {
                Clock = Clock,
                SettingsStore = Store,
                Validator = new SettingsValidator(),
                Prompt = Prompt,
                PowerExecutor = Power,
                OverrideManager = new OverrideManager()
            };
            Controller = new RestrictionController(dependencies, TimeZoneInfo.Utc);
        }

        public RestrictionController Controller { get; }
        public FakeClock Clock { get; }
        public MemorySettingsStore Store { get; }
        public FakePrompt Prompt { get; }
        public FakePowerExecutor Power { get; }

        public static ControllerHarness Create(
            PromptResult? result = null,
            string now = "2026-07-13T23:10:00+00:00",
            bool enabled = true) =>
            new(result ?? PromptResult.TimedOut(), DateTimeOffset.Parse(now), enabled);

        private static AppSettings ValidSettings(bool enabled) => AppSettings.CreateDefaults() with
        {
            Enabled = enabled,
            Password = new PasswordHashRecord("PBKDF2-SHA256", 210_000, "c2FsdA==", "aGFzaA==")
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset Now { get; set; }
    }

    private sealed class MemorySettingsStore : ISettingsStore
    {
        public required AppSettings Settings { get; set; }
        public SettingsLoadResult Load() => new(true, Settings, Array.Empty<SettingsValidationError>());
        public void Save(AppSettings settings) => Settings = settings;
    }

    private sealed class FakePrompt : IRestrictionPrompt
    {
        public PromptResult Result { get; set; } = PromptResult.TimedOut();
        public List<PromptRequest> Requests { get; } = [];
        public Action? OnShow { get; set; }

        public PromptResult Show(PromptRequest request)
        {
            Requests.Add(request);
            OnShow?.Invoke();
            return Result;
        }
    }

    private sealed class FakePowerExecutor : IPowerActionExecutor
    {
        public PowerActionResult Result { get; set; } = PowerActionResult.Success();
        public List<RestrictionAction> Actions { get; } = [];

        public PowerActionResult Execute(RestrictionAction action)
        {
            Actions.Add(action);
            return Result;
        }
    }
}



