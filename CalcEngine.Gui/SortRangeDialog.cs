using CalcEngine.Core;

namespace CalcEngine.Gui;

/// <summary>
/// Modal dialog for CalculationEngine.SortRange (Group C feature:
/// Sorting &amp; Filtering) — picks a single sort column (within the
/// range the grid had selected), direction, and whether the range's
/// first row is a header. No designer file, same approach as
/// RangeRuleDialog: built entirely in code.
///
/// SortRange itself supports multi-key sorts (a list of SortKey), but
/// this dialog only offers one — a second key is a straightforward
/// extension of AvailableColumns/the result if the demo ever needs it.
/// </summary>
public sealed class SortRangeDialog : Form
{
    private readonly ComboBox _columnCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly RadioButton _ascending = new() { Text = "Ascending", Checked = true, AutoSize = true };
    private readonly RadioButton _descending = new() { Text = "Descending", AutoSize = true };
    private readonly CheckBox _hasHeader = new() { Text = "First row is a header (excluded from sorting)", AutoSize = true };

    private readonly int _firstColumn;

    /// <summary>Absolute column number (CellRef.Column) chosen to sort by.</summary>
    public int SelectedColumn { get; private set; }

    public bool Ascending => _ascending.Checked;
    public bool HasHeader => _hasHeader.Checked;

    public SortRangeDialog(CellRange range)
    {
        _firstColumn = range.TopLeft.Column;

        Text = "Sort Range";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(320, 170);

        var columnLabel = new Label { Text = "Sort by column:", Location = new Point(10, 15), AutoSize = true };
        _columnCombo.Location = new Point(150, 12);
        for (int c = range.TopLeft.Column; c <= range.BottomRight.Column; c++)
            _columnCombo.Items.Add(ColumnLetter(c));
        _columnCombo.SelectedIndex = 0;

        _ascending.Location = new Point(10, 45);
        _descending.Location = new Point(10, 70);
        _hasHeader.Location = new Point(10, 100);

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, 135) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(210, 135) };
        okButton.Click += (_, _) => SelectedColumn = _firstColumn + _columnCombo.SelectedIndex;

        Controls.Add(columnLabel);
        Controls.Add(_columnCombo);
        Controls.Add(_ascending);
        Controls.Add(_descending);
        Controls.Add(_hasHeader);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    /// <summary>1-based column number to A1-style letters — the same rule as CellRef.ToA1.</summary>
    private static string ColumnLetter(int column)
    {
        var buf = new Stack<char>();
        int n = column;
        while (n > 0)
        {
            n--;
            buf.Push((char)('A' + n % 26));
            n /= 26;
        }
        return new string(buf.ToArray());
    }
}
