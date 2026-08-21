using CalcEngine.Core.Filtering;
using CalcEngine.Core.Model;

namespace CalcEngine.Gui;

/// <summary>
/// Modal dialog for CalculationEngine.SetFilter (Group C feature:
/// Sorting and Filtering) — picks a column (within the range the grid
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

    /// <summary>
    /// Gets the column the user chose to filter on, counting from 1 at column
    /// A. Only meaningful once the user has accepted the dialog.
    /// </summary>
    public int SelectedColumn { get; private set; }

    /// <summary>
    /// Gets the filter the user described, or null if they have not accepted
    /// the dialog.
    /// </summary>
    public IRowFilter? Filter { get; private set; }

    /// <summary>Creates the dialog for a range the user has selected.</summary>
    /// <param name="range">
    /// The range about to be filtered. Only its columns are offered as
    /// choices, so the user cannot pick one that lies outside it.
    /// </param>
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

    /// <summary>
    /// Shows only the inputs that belong to the kind of filter the user has
    /// picked, so the bounds are offered for a number filter and the search
    /// text for a text filter.
    /// </summary>
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

    /// <summary>
    /// Reads the user's choices into SelectedColumn and Filter, ready for the
    /// caller to collect once the dialog closes.
    /// </summary>
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

    /// <summary>Turns a column number into the letters a user recognises.</summary>
    /// <param name="column">The column number, counting from 1 at column A.</param>
    /// <returns>The column letters, so 27 gives "AA".</returns>
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
