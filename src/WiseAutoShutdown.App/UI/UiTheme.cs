using System.Drawing;

namespace WiseAutoShutdown.UI;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(245, 247, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = Color.FromArgb(32, 33, 36);
    public static readonly Color Muted = Color.FromArgb(95, 99, 104);
    public static readonly Color Accent = Color.FromArgb(19, 111, 99);
    public static readonly Color Danger = Color.FromArgb(176, 48, 48);

    public static void StylePrimaryButton(Button button)
    {
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Height = 36;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.BackColor = Surface;
        button.ForeColor = Text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(190, 195, 200);
        button.Height = 36;
    }
}
