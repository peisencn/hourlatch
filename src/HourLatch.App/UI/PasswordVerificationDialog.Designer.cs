namespace HourLatch.UI;

partial class PasswordVerificationDialog
{
    private System.ComponentModel.IContainer components = null!;
    private TextBox passwordTextBox = null!;
    private Button submitButton = null!;
    private Button cancelButton = null!;
    private Label statusLabel = null!;
    private System.Windows.Forms.Timer throttleTimer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }
    private void InitializeComponent()
    {
        ConfigureForm();
        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            RowCount = 4,
            BackColor = UiTheme.Surface
        };
        CreateControls();
        layout.Controls.Add(new Label { Text = "输入当前密码", AutoSize = true, Font = new Font("Segoe UI Semibold", 11F) });
        layout.Controls.Add(passwordTextBox);
        layout.Controls.Add(statusLabel);
        layout.Controls.Add(CreateActions());
        Controls.Add(layout);
        AcceptButton = submitButton;
        CancelButton = cancelButton;
    }

    private void ConfigureForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(390, 220);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "验证密码";
    }

    private void CreateControls()
    {
        passwordTextBox = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Top, Margin = new Padding(0, 12, 0, 8) };
        statusLabel = new Label { AutoSize = true, ForeColor = UiTheme.Muted, Height = 24 };
        submitButton = new Button { Text = "确认", Width = 96 };
        cancelButton = new Button { Text = "取消", Width = 96, DialogResult = DialogResult.Cancel };
        UiTheme.StylePrimaryButton(submitButton);
        UiTheme.StyleSecondaryButton(cancelButton);
        submitButton.Click += VerifyPassword;
        components = new System.ComponentModel.Container();
        throttleTimer = new System.Windows.Forms.Timer(components) { Interval = 2000 };
        throttleTimer.Tick += EndThrottle;
    }

    private FlowLayoutPanel CreateActions()
    {
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 12, 0, 0)
        };
        actions.Controls.Add(submitButton);
        actions.Controls.Add(cancelButton);
        return actions;
    }
}
