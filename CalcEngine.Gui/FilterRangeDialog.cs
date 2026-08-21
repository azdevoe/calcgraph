using CalcEngine.Core;

namespace CalcEngine.Gui;

/// <summary>
/// Modal dialog for CalculationEngine.SetFilter (Group C feature:
/// Sorting &amp; Filtering) — picks a column (within the range the grid
/// had selected) and one of the three IRowFilter strategies already in
/// CalcEngine.Core. No designer file, same approach as RangeRuleDialog.
/// </summary>
public sealed class FilterRangeDialog : Form
{
    private readonly ComboBox _columnCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly ComboBox _typeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };

    private readonly Label _minLabel = new() { Text = "Min:", Location = new Point(10, 80), AutoSize = true };
    private readonly NumericUpDown _minInput = new() { Minimum = -1000000, Maximum = 1000000, Width = 100, Location = new Point(60, 77) };
    private readonly Label _maxLabel = new() { Text = "Max:", Location = new Point(170, 80), AutoSize = true };
    private readonly NumericUpDown _maxInput = new() { Minimum = -1000000, Maximum = 1000000, Value = 100, Width = 100, Location = new Point(220, 77) };

    private readonly Label _textLabel = new() { Text = "Contains:", Location = new Point(10, 80), AutoSize = true };
    private readonly TextBox _textInput = new() { Width = 220, Location = new Point(80, 77) };

    private readonly int _firstColumn;

    /// <summary>Absolute column number (CellRef.Column) chosen to filter on.</summary>
    public int SelectedColumn { get; private set; }

    /// <summary>The filter built from whichever inputs were visible when OK was pressed.</summary>
    public IRowFilter? Filter { get; private set; }

    public FilterRangeDialog(CellRange range)
    {
        _firstColumn = range.TopLeft.Column;

        Text = "Filter Range";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(340, 160);

        var columnLabel = new Label { Text = "Filter column:", Location = new Point(10, 15), AutoSize = true };
        _columnCombo.Location = new Point(150, 12);
        for (int c = range.TopLeft.Column; c <= range.BottomRight.Column; c++)
            _columnCombo.Items.Add(ColumnLetter(c));
        _columnCombo.SelectedIndex = 0;

        var typeLabel = new Label { Text = "Show rows where:", Location = new Point(10, 45), AutoSize = true };
        _typeCombo.Location = new Point(150, 42);
        _typeCombo.Items.Add("Number is between Min and Max");
        _typeCombo.Items.Add("Text contains...");
        _typeCombo.Items.Add("Cell is not empty");
        _typeCombo.SelectedIndex = 0;
        _typeCombo.SelectedIndexChanged += (_, _) => UpdateInputVisibility();

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(140, 125) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(230, 125) };
        okButton.Click += (_, _) => BuildResult();

        Controls.Add(columnLabel);
        Controls.Add(_columnCombo);
        Controls.Add(typeLabel);
        Controls.Add(_typeCombo);
        Controls.Add(_minLabel);
        Controls.Add(_minInput);
        Controls.Add(_maxLabel);
        Controls.Add(_maxInput);
        Controls.Add(_textLabel);
        Controls.Add(_textInput);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        UpdateInputVisibility();
    }

    private void UpdateInputVisibility()
    {
        bool isNumberRange = _typeCombo.SelectedIndex == 0;
        bool isTextContains = _typeCombo.SelectedIndex == 1;

        _minLabel.Visible = isNumberRange;
        _minInput.Visible = isNumberRange;
        _maxLabel.Visible = isNumberRange;
        _maxInput.Visible = isNumberRange;

        _textLabel.Visible = isTextContains;
        _textInput.Visible = isTextContains;
    }

    private void BuildResult()
    {
        SelectedColumn = _firstColumn + _columnCombo.SelectedIndex;

        Filter = _typeCombo.SelectedIndex switch
        {
            0 => new NumberRangeFilter((double)_minInput.Value, (double)_maxInput.Value),
            1 => new TextContainsFilter(_textInput.Text),
            _ => new NonEmptyFilter()
        };
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
