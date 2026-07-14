namespace WiseAutoShutdown.UI;

public sealed partial class PasswordSetupDialog : Form
{
    public PasswordSetupDialog()
    {
        InitializeComponent();
    }

    public string Password => passwordTextBox.Text;

    private void Confirm(object? sender, EventArgs args)
    {
        if (passwordTextBox.Text.Length < 6)
        {
            statusLabel.Text = "密码至少需要 6 个字符";
            return;
        }

        if (!string.Equals(passwordTextBox.Text, confirmTextBox.Text, StringComparison.Ordinal))
        {
            statusLabel.Text = "两次输入不一致";
            confirmTextBox.Clear();
            confirmTextBox.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }
}
