namespace CalcEngine.Gui;

/// <summary>
/// Tiny modal dialog for attaching a RangeRule to a cell from the grid's
/// right-click menu. No designer file — built entirely in code, the
/// same approach as Form1.
/// </summary>
public sealed class RangeRuleDialog : Form
{
    private readonly NumericUpDown _minInput = new() { Minimum = -1000000, Maximum = 1000000, Width = 100 };
    private readonly NumericUpDown _maxInput = new() { Minimum = -1000000, Maximum = 1000000, Width = 100, Value = 100 };

    public double Min => (double)_minInput.Value;
    public double Max => (double)_maxInput.Value;

    public RangeRuleDialog()
    {
        Text = "Set Range Rule";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(260, 130);

        var minLabel = new Label { Text = "Minimum:", Location = new Point(10, 15), AutoSize = true };
        _minInput.Location = new Point(100, 12);

        var maxLabel = new Label { Text = "Maximum:", Location = new Point(10, 45), AutoSize = true };
        _maxInput.Location = new Point(100, 42);

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(60, 85) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(150, 85) };

        Controls.Add(minLabel);
        Controls.Add(_minInput);
        Controls.Add(maxLabel);
        Controls.Add(_maxInput);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}