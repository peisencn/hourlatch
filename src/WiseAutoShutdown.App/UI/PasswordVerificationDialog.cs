using WiseAutoShutdown.Core.Security;

namespace WiseAutoShutdown.UI;

public sealed partial class PasswordVerificationDialog : Form
{
    private readonly PasswordHashRecord _password;
    private readonly PasswordHasher _hasher;
    private int _failureCount;

    public PasswordVerificationDialog(PasswordHashRecord password, PasswordHasher hasher)
    {
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        InitializeComponent();
    }

    private void VerifyPassword(object? sender, EventArgs args)
    {
        if (_hasher.Verify(passwordTextBox.Text, _password))
        {
            DialogResult = DialogResult.OK;
            return;
        }

        _failureCount++;
        passwordTextBox.Clear();
        statusLabel.Text = "密码错误";
        statusLabel.ForeColor = UiTheme.Danger;
        if (_failureCount >= 3)
        {
            submitButton.Enabled = false;
            statusLabel.Text = "请稍后重试";
            throttleTimer.Start();
        }
    }

    private void EndThrottle(object? sender, EventArgs args)
    {
        throttleTimer.Stop();
        _failureCount = 0;
        submitButton.Enabled = true;
        statusLabel.Text = string.Empty;
        passwordTextBox.Focus();
    }
}
