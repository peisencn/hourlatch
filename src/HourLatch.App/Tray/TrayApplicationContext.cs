using HourLatch.Core.Configuration;
using HourLatch.Core.Overrides;
using HourLatch.Core.Runtime;
using HourLatch.Core.Scheduling;
using HourLatch.Core.Security;
using HourLatch.Diagnostics;
using HourLatch.SystemEvents;
using HourLatch.UI;

namespace HourLatch.Tray;

public sealed class TrayApplicationDependencies
{
    public required RestrictionController Controller { get; init; }
    public required MainForm MainForm { get; init; }
    public required RestrictionPromptForm PromptForm { get; init; }
    public required SystemEventMonitor SystemEvents { get; init; }
    public required ISettingsStore SettingsStore { get; init; }
    public required OverrideManager OverrideManager { get; init; }
    public required PasswordHasher PasswordHasher { get; init; }
    public required LocalLog Log { get; init; }
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Local;
    public bool ShowSettingsOnStart { get; init; }
}

public sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan PeriodicFallback = TimeSpan.FromSeconds(30);
    private readonly TrayApplicationDependencies _dependencies;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly System.Windows.Forms.Timer _evaluationTimer;
    private bool _evaluationInProgress;
    private bool _exiting;

    public TrayApplicationContext(TrayApplicationDependencies dependencies)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        _contextMenu = CreateContextMenu();
        _notifyIcon = CreateNotifyIcon();
        _evaluationTimer = new System.Windows.Forms.Timer();
        _evaluationTimer.Tick += (_, _) => Evaluate(RestrictionTrigger.Periodic);
        _dependencies.SystemEvents.EvaluationRequested += Evaluate;
        _dependencies.MainForm.SettingsChanged += OnSettingsChanged;
        _dependencies.MainForm.FormClosing += OnSettingsClosing;
        Evaluate(RestrictionTrigger.Startup);
        if (_dependencies.ShowSettingsOnStart)
        {
            ShowSettings();
        }
    }

    protected override void ExitThreadCore()
    {
        _evaluationTimer.Stop();
        _evaluationTimer.Dispose();
        _dependencies.SystemEvents.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _dependencies.MainForm.Dispose();
        _dependencies.PromptForm.Dispose();
        base.ExitThreadCore();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("打开设置", null, (_, _) => ShowSettings());
        menu.Items.Add("临时放行", null, (_, _) => Evaluate(RestrictionTrigger.SettingsChanged));
        menu.Items.Add("结束临时放行", null, (_, _) => EndOverride());
        menu.Items.Add("暂停本次限制", null, (_, _) => PauseCurrentWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ProtectedExit());
        return menu;
    }

    private NotifyIcon CreateNotifyIcon() => new()
    {
        ContextMenuStrip = _contextMenu,
        Icon = SystemIcons.Shield,
        Text = AppIdentity.ProductName,
        Visible = true
    };

    private void Evaluate(RestrictionTrigger trigger)
    {
        if (_evaluationInProgress)
        {
            return;
        }

        _evaluationInProgress = true;
        _evaluationTimer.Stop();
        var previousState = _dependencies.Controller.State;
        try
        {
            _dependencies.Controller.Evaluate(trigger, _dependencies.SystemEvents.IsSessionLocked);
            var currentState = _dependencies.Controller.State;
            _dependencies.Log.Event(trigger.ToString(), previousState, currentState);
            _notifyIcon.Text = $"{AppIdentity.ProductName} - {StateText(currentState)}";
        }
        catch (Exception exception)
        {
            _dependencies.Log.Exception(trigger.ToString(), exception);
            Notify("评估失败，请打开设置检查配置。");
        }
        finally
        {
            _evaluationInProgress = false;
            ScheduleNextTick();
        }
    }

    private void ScheduleNextTick()
    {
        if (_exiting)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        DateTimeOffset? target = null;
        try
        {
            target = GetNextTarget(now);
        }
        catch (Exception exception)
        {
            _dependencies.Log.Exception("schedule_next_tick", exception);
        }

        var delay = EvaluationTimerPolicy.GetDelay(now, target, PeriodicFallback);
        _evaluationTimer.Interval = (int)Math.Ceiling(delay.TotalMilliseconds);
        _evaluationTimer.Start();
    }

    private DateTimeOffset? GetNextTarget(DateTimeOffset now)
    {
        if (_dependencies.Controller.NextRetryAt is not null)
        {
            return _dependencies.Controller.NextRetryAt;
        }

        var load = _dependencies.SettingsStore.Load();
        var settings = load.Settings;
        if (!load.IsValid || !settings.Enabled)
        {
            return null;
        }

        var schedule = new DailyRestrictionSchedule(settings.StartTime, settings.EndTime);
        var window = schedule.GetContainingWindow(now, _dependencies.TimeZone);
        if (window is null)
        {
            return schedule.GetNextWindow(now, _dependencies.TimeZone).Start;
        }

        return settings.ActiveOverride?.IsActive(window, now) == true
            ? settings.ActiveOverride.ExpiresAt
            : window.End;
    }

    private void ShowSettings()
    {
        if (_dependencies.MainForm.Visible)
        {
            _dependencies.MainForm.Activate();
            return;
        }

        _dependencies.MainForm.Show();
        _dependencies.MainForm.Activate();
    }

    private void EndOverride()
    {
        var load = _dependencies.SettingsStore.Load();
        if (load.Settings.ActiveOverride is null)
        {
            Notify("当前没有临时放行。");
            return;
        }

        _dependencies.SettingsStore.Save(load.Settings with { ActiveOverride = null });
        Evaluate(RestrictionTrigger.OverrideExpired);
    }

    private void PauseCurrentWindow()
    {
        var load = _dependencies.SettingsStore.Load();
        var settings = load.Settings;
        if (!load.IsValid || settings.Password is null || !VerifyPassword(settings.Password))
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var schedule = new DailyRestrictionSchedule(settings.StartTime, settings.EndTime);
        var window = schedule.GetContainingWindow(now, _dependencies.TimeZone);
        if (window is null)
        {
            Notify("当前不在限制时段内。");
            return;
        }

        var grant = _dependencies.OverrideManager.Create(window, now, OverrideRequest.UntilWindowEnd());
        _dependencies.SettingsStore.Save(settings with { ActiveOverride = grant });
        Evaluate(RestrictionTrigger.SettingsChanged);
    }

    private bool VerifyPassword(PasswordHashRecord password)
    {
        using var dialog = new PasswordVerificationDialog(password, _dependencies.PasswordHasher);
        return dialog.ShowDialog() == DialogResult.OK;
    }

    private void ProtectedExit()
    {
        var load = _dependencies.SettingsStore.Load();
        if (load.Settings.Password is not null && !VerifyPassword(load.Settings.Password))
        {
            return;
        }

        _exiting = true;
        ExitThread();
    }

    private void OnSettingsChanged()
    {
        Evaluate(RestrictionTrigger.SettingsChanged);
        _dependencies.MainForm.Hide();
    }

    private void OnSettingsClosing(object? sender, FormClosingEventArgs args)
    {
        if (_exiting)
        {
            return;
        }

        args.Cancel = true;
        _dependencies.MainForm.Hide();
    }

    private void Notify(string message)
    {
        _notifyIcon.BalloonTipTitle = AppIdentity.ProductName;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(3000);
    }

    private static string StateText(RestrictionState state) => state switch
    {
        RestrictionState.Disabled => "未启用",
        RestrictionState.OutsideWindow => "等待中",
        RestrictionState.Warning => "提醒中",
        RestrictionState.Restricted => "已执行限制",
        RestrictionState.OverrideActive => "临时放行",
        RestrictionState.ActionFailed => "执行失败",
        _ => state.ToString()
    };
}
