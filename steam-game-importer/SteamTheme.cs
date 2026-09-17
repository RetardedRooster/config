namespace SteamGameImporter;

internal static class SteamTheme
{
    public static readonly Color Background = Color.FromArgb(23, 29, 37);
    public static readonly Color Panel = Color.FromArgb(27, 40, 56);
    public static readonly Color Card = Color.FromArgb(42, 71, 94);
    public static readonly Color Accent = Color.FromArgb(102, 192, 244);
    public static readonly Color AccentDark = Color.FromArgb(47, 137, 188);
    public static readonly Color Text = Color.FromArgb(199, 213, 224);
    public static readonly Color Muted = Color.FromArgb(143, 163, 179);
    public static readonly Color Success = Color.FromArgb(117, 176, 34);

    public static void Apply(Form form)
    {
        form.BackColor = Background; form.ForeColor = Text; form.Font = new Font("Segoe UI", 9F);
        ApplyTo(form.Controls);
    }

    private static void ApplyTo(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0;
                    button.BackColor = button.Text.Contains("Hozzáadás", StringComparison.OrdinalIgnoreCase) || button.Text.Contains("Jóváhagyás", StringComparison.OrdinalIgnoreCase) ? Success : AccentDark;
                    button.ForeColor = Color.White; button.Padding = new Padding(10, 4, 10, 4); button.Cursor = Cursors.Hand;
                    break;
                case TextBox textBox:
                    textBox.BackColor = Color.FromArgb(15, 25, 35); textBox.ForeColor = Text; textBox.BorderStyle = BorderStyle.FixedSingle; break;
                case ComboBox combo:
                    combo.BackColor = Color.FromArgb(15, 25, 35); combo.ForeColor = Text; combo.FlatStyle = FlatStyle.Flat; break;
                case DataGridView grid:
                    StyleGrid(grid); break;
                case StatusStrip status:
                    status.BackColor = Color.FromArgb(15, 25, 35); status.ForeColor = Muted; status.SizingGrip = false; break;
                case FlowLayoutPanel flow:
                    flow.BackColor = Panel; flow.ForeColor = Text; break;
                case TableLayoutPanel table:
                    table.BackColor = Background; table.ForeColor = Text; break;
                case Label label:
                    if (label.ForeColor == Color.Black || label.ForeColor == SystemColors.ControlText) label.ForeColor = Text; break;
            }
            if (control.HasChildren) ApplyTo(control.Controls);
        }
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Background; grid.BorderStyle = BorderStyle.None; grid.GridColor = Color.FromArgb(50, 70, 88);
        grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Card; grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Card; grid.ColumnHeadersHeight = 36;
        grid.DefaultCellStyle.BackColor = Panel; grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = AccentDark; grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(31, 46, 63);
        grid.RowHeadersVisible = false; grid.RowTemplate.Height = 32;
    }
}
