namespace WiseAutoShutdown.UI;

partial class PasswordSetupDialog
{
    private TextBox passwordTextBox = null!;
    private TextBox confirmTextBox = null!;
    private Label statusLabel = null!;
    private Button confirmButton = null!;
    private Button cancelButton = null!;

    private void InitializeComponent()
    {
        ConfigureForm();
        var layout = CreateLayout();
        passwordTextBox = CreatePasswordBox();
        confirmTextBox = CreatePasswordBox();
        statusLabel = new Label { AutoSize = true, ForeColor = UiTheme.Danger };
        CreateButtons();
        AddRow(layout, "新密码", passwordTextBox);
        AddRow(layout, "确认密码", confirmTextBox);
        AddWideRow(layout, statusLabel);
        AddWideRow(layout, CreateActions());
        Controls.Add(layout);
        AcceptButton = confirmButton;
        CancelButton = cancelButton;
    }

    private void ConfigureForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 250);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "设置密码";
    }

    private static TableLayoutPanel CreateLayout() => new()
    {
        BackColor = UiTheme.Surface,
        ColumnCount = 2,
        Dock = DockStyle.Fill,
        Padding = new Padding(24),
        RowCount = 0,
        ColumnStyles =
        {
            new ColumnStyle(SizeType.Absolute, 100F),
            new ColumnStyle(SizeType.Percent, 100F)
        }
    };

    private void CreateButtons()
    {
        confirmButton = new Button { Text = "设置", Width = 96 };
        cancelButton = new Button { Text = "取消", Width = 96, DialogResult = DialogResult.Cancel };
        UiTheme.StylePrimaryButton(confirmButton);
        UiTheme.StyleSecondaryButton(cancelButton);
        confirmButton.Click += Confirm;
    }

    private FlowLayoutPanel CreateActions()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 16, 0, 0) };
        panel.Controls.Add(confirmButton);
        panel.Controls.Add(cancelButton);
        return panel;
    }

    private static TextBox CreatePasswordBox() => new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };

    private static void AddWideRow(TableLayoutPanel layout, Control control)
    {
        var row = layout.RowCount++;
        layout.Controls.Add(control, 0, row);
        layout.SetColumnSpan(control, 2);
    }
    private static void AddRow(TableLayoutPanel layout, string text, Control control)
    {
        var row = layout.RowCount++;
        var label = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
        control.Margin = new Padding(0, 6, 0, 6);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }
}



