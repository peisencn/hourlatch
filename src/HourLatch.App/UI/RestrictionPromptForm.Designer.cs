namespace HourLatch.UI;

partial class RestrictionPromptForm
{
    private System.ComponentModel.IContainer components = null!;
    private Label windowLabel = null!;
    private Label actionLabel = null!;
    private Label countdownLabel = null!;
    private TextBox passwordTextBox = null!;
    private ComboBox durationComboBox = null!;
    private NumericUpDown customMinutesNumeric = null!;
    private Button allowButton = null!;
    private Button executeNowButton = null!;
    private Label statusLabel = null!;
    private System.Windows.Forms.Timer countdownTimer = null!;

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
        CreateControls();
        var layout = CreateLayout();
        AddHeader(layout);
        AddInputRows(layout);
        AddActions(layout);
        Controls.Add(layout);
        WireEvents();
    }

    private void ConfigureForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(520, 430);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "HourLatch - 限制提醒";
        TopMost = true;
    }

    private void CreateControls()
    {
        windowLabel = new Label { AutoSize = true, ForeColor = UiTheme.Muted };
        actionLabel = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 16F) };
        countdownLabel = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 28F), ForeColor = UiTheme.Danger };
        passwordTextBox = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        durationComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        customMinutesNumeric = new NumericUpDown { Minimum = 1, Maximum = 1440, Value = 30, Dock = DockStyle.Fill, Enabled = false };
        statusLabel = new Label { AutoSize = true, ForeColor = UiTheme.Danger };
        allowButton = new Button { Text = "验证并临时放行", Width = 160 };
        executeNowButton = new Button { Text = "立即执行", Width = 112 };
        UiTheme.StylePrimaryButton(allowButton);
        UiTheme.StyleSecondaryButton(executeNowButton);
        components = new System.ComponentModel.Container();
        countdownTimer = new System.Windows.Forms.Timer(components) { Interval = 250 };
    }

    private static TableLayoutPanel CreateLayout() => new()
    {
        BackColor = UiTheme.Surface,
        ColumnCount = 2,
        Dock = DockStyle.Fill,
        Padding = new Padding(28, 24, 28, 24),
        RowCount = 0,
        ColumnStyles =
        {
            new ColumnStyle(SizeType.Absolute, 130F),
            new ColumnStyle(SizeType.Percent, 100F)
        }
    };

    private void AddHeader(TableLayoutPanel layout)
    {
        AddWideRow(layout, windowLabel);
        AddWideRow(layout, actionLabel);
        AddWideRow(layout, countdownLabel);
    }

    private void AddInputRows(TableLayoutPanel layout)
    {
        AddRow(layout, "访问密码", passwordTextBox);
        AddRow(layout, "放行时长", durationComboBox);
        AddRow(layout, "自定义分钟", customMinutesNumeric);
        AddWideRow(layout, statusLabel);
    }

    private void AddActions(TableLayoutPanel layout)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 16, 0, 0) };
        panel.Controls.Add(allowButton);
        panel.Controls.Add(executeNowButton);
        AddWideRow(layout, panel);
    }

    private void WireEvents()
    {
        allowButton.Click += AllowTemporarily;
        executeNowButton.Click += ExecuteImmediately;
        durationComboBox.SelectedIndexChanged += DurationChanged;
        countdownTimer.Tick += CountdownTick;
        FormClosing += PromptClosing;
    }

    private static void AddRow(TableLayoutPanel layout, string text, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 10, 0, 10) };
        control.Margin = new Padding(0, 6, 0, 6);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddWideRow(TableLayoutPanel layout, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Margin = new Padding(0, 6, 0, 6);
        layout.Controls.Add(control, 0, row);
        layout.SetColumnSpan(control, 2);
    }
}
