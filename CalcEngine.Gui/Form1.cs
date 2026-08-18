using CalcEngine.Core;

namespace CalcEngine.Gui;

/// <summary>
/// The demonstration client for CalculationEngine (Design_Portfolio,
/// Results). A scrollable grid: select a cell, type a value or a
/// formula, dependent cells update instantly. Error values and
/// circular references are visibly flagged, as required by the brief.
///
/// This form is the Observer: it subscribes to the engine and reacts
/// to CellChangeSet/circular-reference notifications rather than
/// pushing updates itself after every edit. That is the pattern the
/// Design Portfolio specifies for change propagation (Observer), and
/// it means Undo/Redo — which also route through ApplyEdit — refresh
/// the grid for free, with no separate code path.
/// </summary>
public partial class Form1 : Form, ICellObserver
{
    private const int Rows = 100;
    private const int Cols = 26; // A .. Z

    private readonly CalculationEngine _engine = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _formulaBar = new();
    private readonly Label _addressLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Button _undoButton = new();
    private readonly Button _redoButton = new();
    private readonly ContextMenuStrip _cellMenu = new();

    private static readonly Color ErrorColor = Color.MistyRose;
    private static readonly Color NormalColor = Color.White;

    public Form1()
    {
        InitializeComponent();
        BuildLayout();
        BuildGrid();
        WireEvents();
        _engine.Subscribe(this);
    }

    // ── Layout ───────────────────────────────────────────────────

    private void BuildLayout()
    {
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 34 };

        _undoButton.Text = "Undo";
        _undoButton.AutoSize = true;
        _undoButton.Location = new Point(4, 4);
        _undoButton.Click += (_, _) => TryUndo();

        _redoButton.Text = "Redo";
        _redoButton.AutoSize = true;
        _redoButton.Location = new Point(70, 4);
        _redoButton.Click += (_, _) => TryRedo();

        toolbar.Controls.Add(_undoButton);
        toolbar.Controls.Add(_redoButton);

        var formulaBarPanel = new Panel { Dock = DockStyle.Top, Height = 30 };
        _addressLabel.Text = "";
        _addressLabel.TextAlign = ContentAlignment.MiddleCenter;
        _addressLabel.BorderStyle = BorderStyle.FixedSingle;
        _addressLabel.Location = new Point(0, 0);
        _addressLabel.Size = new Size(60, 28);

        _formulaBar.Location = new Point(60, 2);
        _formulaBar.Width = 1020;
        _formulaBar.ReadOnly = true; // editing happens in the grid itself

        formulaBarPanel.Controls.Add(_addressLabel);
        formulaBarPanel.Controls.Add(_formulaBar);

        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 26;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(4, 0, 0, 0);
        _statusLabel.BackColor = Color.WhiteSmoke;

        _grid.Dock = DockStyle.Fill;

        // Add order matters for Dock layout: controls are docked in the
        // REVERSE of Controls.Add order (last added is docked first).
        // _grid (Dock=Fill) must be added FIRST so it's processed LAST and
        // only claims whatever space is left after the Top/Bottom panels
        // have taken theirs. Adding it last (as before) made it claim the
        // whole client area before toolbar/formulaBarPanel got a turn,
        // so those panels rendered on top of the grid instead of above it.
        Controls.Add(_grid);
        Controls.Add(toolbar);
        Controls.Add(formulaBarPanel);
        Controls.Add(_statusLabel);
    }
    private void BuildGrid()
    {
        _grid.RowHeadersWidth = 40;
        _grid.ColumnHeadersVisible = true;
        _grid.ColumnHeadersHeight = 28;
        _grid.RowHeadersVisible = true;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.Gainsboro;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
        _grid.RowHeadersDefaultCellStyle.BackColor = Color.Gainsboro;
        _grid.RowHeadersDefaultCellStyle.ForeColor = Color.Black;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        for (int c = 0; c < Cols; c++)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = ColumnLetter(c),
                HeaderText = ColumnLetter(c),
                Width = 70,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            _grid.Columns.Add(column);
        }

        _grid.Rows.Add(Rows);
        for (int r = 0; r < Rows; r++)
            _grid.Rows[r].HeaderCell.Value = (r + 1).ToString();

        // Right-click a cell to attach or clear a data validation rule
        // (Group C feature) without leaving the grid.
        var setRule = new ToolStripMenuItem("Set Range Rule...");
        setRule.Click += (_, _) => SetRangeRuleOnSelectedCell();
        var clearRule = new ToolStripMenuItem("Clear Rule");
        clearRule.Click += (_, _) => ClearRuleOnSelectedCell();
        _cellMenu.Items.Add(setRule);
        _cellMenu.Items.Add(clearRule);
        _grid.ContextMenuStrip = _cellMenu;
    }
    private void WireEvents()
    {
        _grid.CellBeginEdit += OnCellBeginEdit;
        _grid.CellEndEdit += OnCellEndEdit;
        _grid.SelectionChanged += OnSelectionChanged;
        _grid.DataError += (_, e) => e.ThrowException = false;

        KeyPreview = true;
        KeyDown += OnFormKeyDown;
        _grid.CellMouseDown += OnCellMouseDown;
    }

    // ── Grid <-> CellRef mapping ─────────────────────────────────

    private void OnCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        }
    }
    private static CellRef ToCellRef(int rowIndex, int colIndex) =>
        new(rowIndex + 1, colIndex + 1);

    private static string ColumnLetter(int colIndex) =>
        ((char)('A' + colIndex)).ToString();

    private bool TryToGridPosition(CellRef cellRef, out int rowIndex, out int colIndex)
    {
        rowIndex = cellRef.Row - 1;
        colIndex = cellRef.Column - 1;
        return rowIndex >= 0 && rowIndex < Rows && colIndex >= 0 && colIndex < Cols;
    }

    // ── Editing ──────────────────────────────────────────────────

    private void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        // Show the raw formula/text for editing, not the computed value.
        var cellRef = ToCellRef(e.RowIndex, e.ColumnIndex);
        _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = _engine.GetFormula(cellRef);
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var cellRef = ToCellRef(e.RowIndex, e.ColumnIndex);
        var rawInput = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string ?? "";

        var result = _engine.SetCellContent(cellRef, rawInput);

        if (result.Success)
        {
            _statusLabel.Text = "";
            _statusLabel.BackColor = Color.WhiteSmoke;
            // OnCellsChanged (fired synchronously by the engine) already
            // refreshed every affected cell's display, including this one.
        }
        else
        {
            // Rejected: put the cell back to whatever it actually holds
            // (the engine never touched it) and explain why.
            _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = DisplayValueFor(cellRef);
            FlashError(e.RowIndex, e.ColumnIndex);
            _statusLabel.Text = DescribeFailure(result);
            _statusLabel.BackColor = ErrorColor;
        }

        UpdateFormulaBar();
        UpdateUndoRedoButtons();
    }

    private static string DescribeFailure(CellChangeSet result) => result.FailureReason switch
    {
        ChangeFailureReason.ParseError => $"Syntax error: {result.ErrorMessage}",
        ChangeFailureReason.Circular => $"Circular reference: {string.Join(" \u2192 ", result.CircularPath!.Select(c => c.ToA1()))}",
        ChangeFailureReason.ValidationError => $"Rejected: {result.ErrorMessage}",
        _ => "Edit rejected."
    };

    private string DisplayValueFor(CellRef cellRef) => _engine.GetValue(cellRef).ToString();

    private void FlashError(int rowIndex, int colIndex)
    {
        _grid.Rows[rowIndex].Cells[colIndex].Style.BackColor = ErrorColor;
    }

    // ── Observer callbacks (Design_Portfolio: Observer pattern) ────

    public void OnCellsChanged(CellChangeSet changeSet)
    {
        foreach (var cellRef in changeSet.ChangedCells)
        {
            if (!TryToGridPosition(cellRef, out var r, out var c)) continue;

            var value = _engine.GetValue(cellRef);
            var cell = _grid.Rows[r].Cells[c];
            cell.Value = value.ToString();
            cell.Style.BackColor = value.IsError ? ErrorColor : NormalColor;
        }
    }

    public void OnCircularReference(IReadOnlyList<CellRef> cyclePath)
    {
        _statusLabel.Text = $"Circular reference: {string.Join(" \u2192 ", cyclePath.Select(c => c.ToA1()))}";
        _statusLabel.BackColor = ErrorColor;

        foreach (var cellRef in cyclePath)
            if (TryToGridPosition(cellRef, out var r, out var c))
                _grid.Rows[r].Cells[c].Style.BackColor = ErrorColor;
    }

    // ── Selection / formula bar ─────────────────────────────────────

    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateFormulaBar();

    private void UpdateFormulaBar()
    {
        if (_grid.CurrentCell is null) return;

        var cellRef = ToCellRef(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex);
        _addressLabel.Text = cellRef.ToA1();
        _formulaBar.Text = _engine.GetFormula(cellRef);
    }

    // ── Undo / Redo ──────────────────────────────────────────────

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z) { TryUndo(); e.Handled = true; }
        if (e.Control && e.KeyCode == Keys.Y) { TryRedo(); e.Handled = true; }
    }

    private void TryUndo()
    {
        if (!_engine.CanUndo) return;
        _engine.Undo();
        UpdateFormulaBar();
        UpdateUndoRedoButtons();
    }

    private void TryRedo()
    {
        if (!_engine.CanRedo) return;
        _engine.Redo();
        UpdateFormulaBar();
        UpdateUndoRedoButtons();
    }

    private void UpdateUndoRedoButtons()
    {
        _undoButton.Enabled = _engine.CanUndo;
        _redoButton.Enabled = _engine.CanRedo;
    }

    // ── Data validation rule assignment (Group C feature) ───────────

    private void SetRangeRuleOnSelectedCell()
    {
        if (_grid.CurrentCell is null) return;
        var cellRef = ToCellRef(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex);

        using var dialog = new RangeRuleDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _engine.SetValidationRule(cellRef, new RangeRule(dialog.Min, dialog.Max));
            _statusLabel.Text = $"Rule set on {cellRef.ToA1()}: between {dialog.Min} and {dialog.Max}";
            _statusLabel.BackColor = Color.WhiteSmoke;
        }
    }

    private void ClearRuleOnSelectedCell()
    {
        if (_grid.CurrentCell is null) return;
        var cellRef = ToCellRef(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex);
        _engine.ClearValidationRule(cellRef);
        _statusLabel.Text = $"Rule cleared on {cellRef.ToA1()}";
        _statusLabel.BackColor = Color.WhiteSmoke;
    }
}