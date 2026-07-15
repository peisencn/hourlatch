using HourLatch.Core.Overrides;
using HourLatch.Core.Power;
using HourLatch.Core.Runtime;
using HourLatch.Core.Security;

namespace HourLatch.UI;

public sealed partial class RestrictionPromptForm : Form, IRestrictionPrompt
{
    private readonly PasswordHasher _hasher;
    private PromptRequest? _request;
    private PromptResult _result = PromptResult.TimedOut();
    private DateTimeOffset _deadline;

    public RestrictionPromptForm(PasswordHasher hasher)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        InitializeComponent();
    }

    PromptResult IRestrictionPrompt.Show(PromptRequest request)
    {
        Prepare(request);
        ShowDialog();
        countdownTimer.Stop();
        passwordTextBox.Clear();
        return _result;
    }

    private void Prepare(PromptRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _result = PromptResult.TimedOut();
        _deadline = DateTimeOffset.Now.AddSeconds(request.WarningSeconds);
        windowLabel.Text = $"限制时段  {request.Window.Start.LocalDateTime:HH:mm} - {request.Window.End.LocalDateTime:HH:mm}";
        actionLabel.Text = request.Action == RestrictionAction.Lock ? "即将锁定电脑" : "即将使电脑进入休眠";
        statusLabel.Text = string.Empty;
        PopulateDurations(request);
        UpdateCountdown();
        countdownTimer.Start();
    }

    private void PopulateDurations(PromptRequest request)
    {
        durationComboBox.Items.Clear();
        durationComboBox.Items.Add(new DurationOption("15 分钟", 15));
        durationComboBox.Items.Add(new DurationOption("30 分钟", 30));
        durationComboBox.Items.Add(new DurationOption("60 分钟", 60));
        durationComboBox.Items.Add(new DurationOption("自定义", null, IsCustom: true));
        if (request.AllowUntilWindowEnd)
        {
            durationComboBox.Items.Add(new DurationOption("直到本次限制结束", null, UntilEnd: true));
        }

        var defaultIndex = request.DefaultOverrideMinutes switch { 15 => 0, 60 => 2, _ => 1 };
        durationComboBox.SelectedIndex = defaultIndex;
    }

    private void AllowTemporarily(object? sender, EventArgs args)
    {
        if (_request is null || !_hasher.Verify(passwordTextBox.Text, _request.Password))
        {
            statusLabel.Text = "密码错误";
            passwordTextBox.Clear();
            passwordTextBox.Focus();
            return;
        }

        var option = (DurationOption)durationComboBox.SelectedItem!;
        var overrideRequest = option.UntilEnd
            ? OverrideRequest.UntilWindowEnd()
            : OverrideRequest.ForMinutes(option.IsCustom
                ? decimal.ToInt32(customMinutesNumeric.Value)
                : option.Minutes!.Value);
        _result = PromptResult.OverrideApproved(overrideRequest);
        DialogResult = DialogResult.OK;
    }

    private void ExecuteImmediately(object? sender, EventArgs args)
    {
        _result = PromptResult.ExecuteNow();
        DialogResult = DialogResult.OK;
    }

    private void DurationChanged(object? sender, EventArgs args)
    {
        customMinutesNumeric.Enabled = durationComboBox.SelectedItem is DurationOption { IsCustom: true };
    }

    private void CountdownTick(object? sender, EventArgs args)
    {
        if (DateTimeOffset.Now >= _deadline)
        {
            _result = PromptResult.TimedOut();
            DialogResult = DialogResult.Cancel;
            return;
        }

        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        var seconds = Math.Max(0, (int)Math.Ceiling((_deadline - DateTimeOffset.Now).TotalSeconds));
        countdownLabel.Text = $"{seconds} 秒";
    }

    private void PromptClosing(object? sender, FormClosingEventArgs args)
    {
        if (DialogResult == DialogResult.None)
        {
            _result = PromptResult.TimedOut();
        }
    }

    private sealed record DurationOption(
        string Text,
        int? Minutes,
        bool IsCustom = false,
        bool UntilEnd = false)
    {
        public override string ToString() => Text;
    }
}
