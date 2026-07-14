namespace WiseAutoShutdown.UI;

partial class MainForm
{
    private CheckBox enabledCheckBox = null!;
    private DateTimePicker startTimePicker = null!;
    private DateTimePicker endTimePicker = null!;
    private ComboBox actionComboBox = null!;
    private NumericUpDown warningSecondsNumeric = null!;
    private ComboBox defaultOverrideComboBox = null!;
    private CheckBox allowUntilEndCheckBox = null!;
    private CheckBox autoStartCheckBox = null!;
    private Button setPasswordButton = null!;
    private Label statusLabel = null!;
    private Label nextActionLabel = null!;
    private Button saveButton = null!;

    private void InitializeComponent()
    {
        ConfigureForm();
        var layout = CreateLayout();
        CreateControls();
        AddScheduleRows(layout);
        AddBehaviorRows(layout);
        AddFooter(layout);
        Controls.Add(layout);
        WireEvents();
    }

    private void ConfigureForm()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiTheme.Background;
        ClientSize = new Size(560, 590);
        Font = new Font("Segoe UI", 9F);
        ForeColor = UiTheme.Text;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "定时限制";
    }

    private static TableLayoutPanel CreateLayout() => new()
    {
        BackColor = UiTheme.Surface,
        ColumnCount = 2,
        Dock = DockStyle.Fill,
        Padding = new Padding(28, 22, 28, 22),
        RowCount = 0,
        ColumnStyles =
        {
            new ColumnStyle(SizeType.Percent, 42F),
            new ColumnStyle(SizeType.Percent, 58F)
        }
    };

    private void CreateControls()
    {
        enabledCheckBox = new CheckBox { Text = "启用每日限制", AutoSize = true };
        startTimePicker = CreateTimePicker();
        endTimePicker = CreateTimePicker();
        actionComboBox = CreateComboBox("锁屏", "休眠");
        warningSecondsNumeric = new NumericUpDown { Minimum = 1, Maximum = 300, Value = 60, Width = 160 };
        defaultOverrideComboBox = CreateComboBox("15 分钟", "30 分钟", "60 分钟");
        allowUntilEndCheckBox = new CheckBox { Text = "允许放行至本次结束", AutoSize = true };
        autoStartCheckBox = new CheckBox { Text = "登录 Windows 后自动启动", AutoSize = true };
        setPasswordButton = new Button { Text = "设置或修改密码", Width = 160 };
        UiTheme.StyleSecondaryButton(setPasswordButton);
    }

    private void AddScheduleRows(TableLayoutPanel layout)
    {
        AddSection(layout, "限制时段");
        AddWideRow(layout, enabledCheckBox);
        AddRow(layout, "开始时间", startTimePicker);
        AddRow(layout, "结束时间", endTimePicker);
        AddRow(layout, "执行动作", actionComboBox);
        AddRow(layout, "提醒倒计时（秒）", warningSecondsNumeric);
    }

    private void AddBehaviorRows(TableLayoutPanel layout)
    {
        AddSection(layout, "临时放行与启动");
        AddRow(layout, "默认放行时长", defaultOverrideComboBox);
        AddWideRow(layout, allowUntilEndCheckBox);
        AddWideRow(layout, autoStartCheckBox);
        AddRow(layout, "访问密码", setPasswordButton);
    }

    private void AddFooter(TableLayoutPanel layout)
    {
        statusLabel = new Label { AutoSize = true, ForeColor = UiTheme.Muted, MaximumSize = new Size(480, 0) };
        nextActionLabel = new Label { AutoSize = true, ForeColor = UiTheme.Muted };
        saveButton = new Button { Text = "保存配置", Width = 128 };
        UiTheme.StylePrimaryButton(saveButton);
        AddWideRow(layout, statusLabel);
        AddWideRow(layout, nextActionLabel);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        actions.Controls.Add(saveButton);
        AddWideRow(layout, actions);
    }

    private void WireEvents()
    {
        enabledCheckBox.CheckedChanged += RestrictionEnabledChanged;
        setPasswordButton.Click += SetPassword;
        saveButton.Click += SaveSettings;
    }

    private static DateTimePicker CreateTimePicker() => new()
    {
        CustomFormat = "HH:mm",
        Format = DateTimePickerFormat.Custom,
        ShowUpDown = true,
        Width = 160
    };

    private static ComboBox CreateComboBox(params string[] items)
    {
        var comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        comboBox.Items.AddRange(items);
        comboBox.SelectedIndex = 0;
        return comboBox;
    }

    private static void AddSection(TableLayoutPanel layout, string text)
    {
        var label = new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 11F), Margin = new Padding(0, 12, 0, 8) };
        AddWideRow(layout, label);
    }

    private static void AddRow(TableLayoutPanel layout, string labelText, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 0, 9) };
        control.Anchor = AnchorStyles.Left;
        control.Margin = new Padding(0, 5, 0, 5);
        layout.Controls.Add(label, 0, layout.RowCount);
        layout.Controls.Add(control, 1, layout.RowCount);
        layout.RowCount++;
    }

    private static void AddWideRow(TableLayoutPanel layout, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Margin = new Padding(0, 6, 0, 6);
        layout.Controls.Add(control, 0, layout.RowCount);
        layout.SetColumnSpan(control, 2);
        layout.RowCount++;
    }
}

