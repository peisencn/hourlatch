using HourLatch.Core.Configuration;
using HourLatch.Core.Power;
using HourLatch.Core.Scheduling;
using HourLatch.Core.Security;
using HourLatch.Startup;

namespace HourLatch.UI;

public sealed class MainFormDependencies
{
    public required ISettingsStore SettingsStore { get; init; }
    public required SettingsValidator Validator { get; init; }
    public required PasswordHasher PasswordHasher { get; init; }
    public required StartupRegistration StartupRegistration { get; init; }
}

public sealed partial class MainForm : Form
{
    private readonly MainFormDependencies _dependencies;
    private AppSettings _settings = AppSettings.CreateDefaults();

    public MainForm(MainFormDependencies dependencies)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        InitializeComponent();
        Load += (_, _) => LoadSettings();
    }

    public event Action? SettingsChanged;

    private void LoadSettings()
    {
        var result = _dependencies.SettingsStore.Load();
        _settings = result.Settings;
        enabledCheckBox.Checked = _settings.Enabled;
        startTimePicker.Value = TodayAt(_settings.StartTime);
        endTimePicker.Value = TodayAt(_settings.EndTime);
        actionComboBox.SelectedIndex = _settings.Action == RestrictionAction.Lock ? 0 : 1;
        warningSecondsNumeric.Value = Math.Clamp(_settings.WarningSeconds, 1, 300);
        defaultOverrideComboBox.SelectedIndex = MinutesToIndex(_settings.DefaultOverrideMinutes);
        allowUntilEndCheckBox.Checked = _settings.AllowUntilWindowEnd;
        autoStartCheckBox.Checked = _settings.AutoStart;
        statusLabel.Text = result.IsValid ? "配置已加载" : "配置无效，限制已关闭";
        statusLabel.ForeColor = result.IsValid ? UiTheme.Muted : UiTheme.Danger;
        UpdateNextAction();
    }

    private void SaveSettings(object? sender, EventArgs args)
    {
        var candidate = ReadSettingsFromControls();
        var validation = _dependencies.Validator.Validate(candidate);
        if (!validation.IsValid)
        {
            ShowValidationErrors(validation);
            return;
        }

        if (!TryPersist(candidate))
        {
            return;
        }
        _settings = candidate;
        statusLabel.ForeColor = UiTheme.Accent;
        statusLabel.Text = "配置已保存";
        UpdateNextAction();
        SettingsChanged?.Invoke();
    }

    private bool TryPersist(AppSettings candidate)
    {
        try
        {
            var startupResult = candidate.AutoStart
                ? _dependencies.StartupRegistration.Enable()
                : _dependencies.StartupRegistration.Disable();
            if (!startupResult.Succeeded)
            {
                ShowError($"自动启动设置失败：{startupResult.ErrorMessage}");
                return false;
            }

            _dependencies.SettingsStore.Save(candidate);
            return true;
        }
        catch (Exception exception)
        {
            ShowError($"保存失败：{exception.Message}");
            return false;
        }
    }
    private AppSettings ReadSettingsFromControls() => _settings with
    {
        Enabled = enabledCheckBox.Checked,
        StartTime = TimeOnly.FromDateTime(startTimePicker.Value),
        EndTime = TimeOnly.FromDateTime(endTimePicker.Value),
        Action = actionComboBox.SelectedIndex == 1 ? RestrictionAction.Sleep : RestrictionAction.Lock,
        WarningSeconds = decimal.ToInt32(warningSecondsNumeric.Value),
        DefaultOverrideMinutes = IndexToMinutes(defaultOverrideComboBox.SelectedIndex),
        AllowUntilWindowEnd = allowUntilEndCheckBox.Checked,
        AutoStart = autoStartCheckBox.Checked
    };

    private void SetPassword(object? sender, EventArgs args)
    {
        if (_settings.Password is not null)
        {
            using var verification = new PasswordVerificationDialog(
                _settings.Password,
                _dependencies.PasswordHasher);
            if (verification.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
        }

        using var setup = new PasswordSetupDialog();
        if (setup.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings = _settings with { Password = _dependencies.PasswordHasher.Hash(setup.Password) };
        statusLabel.ForeColor = UiTheme.Accent;
        statusLabel.Text = "密码已更新，请保存配置";
    }

    private void RestrictionEnabledChanged(object? sender, EventArgs args)
    {
        if (enabledCheckBox.Checked && _settings.Password is null)
        {
            enabledCheckBox.Checked = false;
            ShowError("启用限制前必须先设置密码。");
        }
    }

    private void UpdateNextAction()
    {
        if (!_settings.Enabled)
        {
            nextActionLabel.Text = "下一次限制：未启用";
            return;
        }

        var schedule = new DailyRestrictionSchedule(_settings.StartTime, _settings.EndTime);
        var window = schedule.GetNextWindow(DateTimeOffset.Now, TimeZoneInfo.Local);
        nextActionLabel.Text = $"下一次限制：{window.Start.LocalDateTime:MM-dd HH:mm}";
    }

    private void ShowValidationErrors(SettingsValidationResult validation)
    {
        var message = string.Join(Environment.NewLine, validation.Errors.Select(error => error.Message));
        ShowError(message);
    }

    private void ShowError(string message)
    {
        statusLabel.ForeColor = UiTheme.Danger;
        statusLabel.Text = message;
    }

    private static DateTime TodayAt(TimeOnly time) => DateTime.Today.Add(time.ToTimeSpan());

    private static int MinutesToIndex(int minutes) => minutes switch { 15 => 0, 60 => 2, _ => 1 };

    private static int IndexToMinutes(int index) => index switch { 0 => 15, 2 => 60, _ => 30 };
}
