using CalcEngine.Core.Sorting;
using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Model;
using CalcEngine.Core.Validation;

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
    private readonly Button _sortButton = new();
    private readonly Button _filterButton = new();
    private readonly Button _clearFilterButton = new();
    private readonly ContextMenuStrip _cellMenu = new();

    // Group C feature: Sorting & Filtering. FilterManager (in Core)
    // holds the real filter state, keyed by (range, column) — this is
    // just enough client-side bookkeeping to know which rows to hide
    // in the grid and which filters "Clear Filter" should remove.
    // Simplification: only one filtered range is tracked at a time,
    // and undoing/redoing a filter command through Ctrl+Z does not
    // resync this bookkeeping — acceptable for a demo client, since
    // FilterManager has no "list active filters" query to rebuild it
    // from (documented limitation, not an oversight).
    private CellRange? _activeFilterRange;
    private readonly HashSet<int> _activeFilterColumns = new();

    private static readonly Color ErrorColor = Color.MistyRose;
    private static readonly Color NormalColor = Color.White;

    /// <summary>
    /// Creates the spreadsheet window, ready for the user to type in, and
    /// signs it up to be told whenever the engine changes anything.
    /// </summary>
    public Form1()
    {
        InitializeComponent();
        BuildLayout();
        BuildGrid();
        WireEvents();
        _engine.Subscribe(this);
    }

    // ── Layout ───────────────────────────────────────────────────

    /// <summary>
    /// Puts the window together: the toolbar along the top, the formula bar
    /// beneath it, the status line along the bottom, and the grid filling
    /// what is left.
    /// </summary>
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

        _sortButton.Text = "Sort...";
        _sortButton.AutoSize = true;
        _sortButton.Location = new Point(140, 4);
        _sortButton.Click += (_, _) => ShowSortDialog();

        _filterButton.Text = "Filter...";
        _filterButton.AutoSize = true;
        _filterButton.Location = new Point(210, 4);
        _filterButton.Click += (_, _) => ShowFilterDialog();

        _clearFilterButton.Text = "Clear Filter";
        _clearFilterButton.AutoSize = true;
        _clearFilterButton.Location = new Point(280, 4);
        _clearFilterButton.Click += (_, _) => ClearActiveFilter();

        toolbar.Controls.Add(_undoButton);
        toolbar.Controls.Add(_redoButton);
        toolbar.Controls.Add(_sortButton);
        toolbar.Controls.Add(_filterButton);
        toolbar.Controls.Add(_clearFilterButton);

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
        _grid.RowHeadersWidth = 80;
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

    /// <summary>
    /// Moves the selection to the cell that was right-clicked, so the
    /// right-click menu acts on the cell the user pointed at rather than the
    /// one that happened to be selected already.
    /// </summary>
    /// <param name="sender">The grid that was clicked.</param>
    /// <param name="e">Which button was pressed, and over which cell.</param>
    private void OnCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        }
    }
    /// <summary>Turns a position in the grid into the cell address it stands for.</summary>
    /// <param name="rowIndex">The grid row, counting from 0.</param>
    /// <param name="colIndex">The grid column, counting from 0.</param>
    /// <returns>The matching address, which counts from 1.</returns>
    private static CellRef ToCellRef(int rowIndex, int colIndex) =>
        new(rowIndex + 1, colIndex + 1);

    /// <summary>Returns the heading to put above a grid column.</summary>
    /// <param name="colIndex">The grid column, counting from 0.</param>
    /// <returns>A single letter, from "A" for the first column.</returns>
    private static string ColumnLetter(int colIndex) =>
        ((char)('A' + colIndex)).ToString();

    /// <summary>
    /// Finds where a cell sits in the grid, if it is on screen at all.
    /// </summary>
    /// <param name="cellRef">The address to look for.</param>
    /// <param name="rowIndex">When this returns, the grid row holding it.</param>
    /// <param name="colIndex">When this returns, the grid column holding it.</param>
    /// <returns>
    /// true if the address falls inside the part of the sheet the grid shows;
    /// otherwise, false. The engine has no fixed size, so a formula may well
    /// refer to cells beyond the displayed rows and columns.
    /// </returns>
    private bool TryToGridPosition(CellRef cellRef, out int rowIndex, out int colIndex)
    {
        rowIndex = cellRef.Row - 1;
        colIndex = cellRef.Column - 1;
        return rowIndex >= 0 && rowIndex < Rows && colIndex >= 0 && colIndex < Cols;
    }

    // ── Editing ──────────────────────────────────────────────────

    /// <summary>
    /// Swaps a cell's displayed result for the text behind it as editing
    /// starts, so the user edits the formula they wrote rather than the number
    /// it produced.
    /// </summary>
    /// <param name="sender">The grid being edited.</param>
    /// <param name="e">Which cell is about to be edited.</param>
    private void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        // Show the raw formula/text for editing, not the computed value.
        var cellRef = ToCellRef(e.RowIndex, e.ColumnIndex);
        _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = _engine.GetFormula(cellRef);
    }

    /// <summary>
    /// Hands what the user typed to the engine once they finish editing a
    /// cell.
    /// </summary>
    /// <param name="sender">The grid being edited.</param>
    /// <param name="e">Which cell was edited.</param>
    /// <remarks>
    /// If the engine turns the edit down, the cell goes back to showing what
    /// it really holds, is tinted to draw the eye, and the status line explains
    /// what was wrong. Nothing is lost, because a refused edit never changed
    /// anything in the first place.
    /// </remarks>
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
            // If edit failed due to a parse error or circular reference, 
            // display failure reason in status bar and highlight cell
            FlashError(e.RowIndex, e.ColumnIndex);
            _statusLabel.Text = DescribeFailure(result);
            _statusLabel.BackColor = ErrorColor;

            // If it was a circular reference, trigger circular reference formatting
            if (result.FailureReason == ChangeFailureReason.Circular && result.CircularPath != null)
            {
                OnCircularReference(result.CircularPath);
            }
            else
            {
                // Preserve what the engine currently holds
                _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = DisplayValueFor(cellRef);
            }
        }

        UpdateFormulaBar();
        UpdateUndoRedoButtons();
    }

    /// <summary>Puts a refused edit into words for the status line.</summary>
    /// <param name="result">The refusal to describe.</param>
    /// <returns>
    /// A sentence naming what went wrong: the syntax problem, the loop of
    /// cells that led back on itself, or the rule the value broke.
    /// </returns>
    private static string DescribeFailure(CellChangeSet result) => result.FailureReason switch
    {
        ChangeFailureReason.ParseError => $"Syntax error: {result.ErrorMessage}",
        ChangeFailureReason.Circular => $"Circular reference: {string.Join(" \u2192 ", result.CircularPath!.Select(c => c.ToA1()))}",
        ChangeFailureReason.ValidationError => $"Rejected: {result.ErrorMessage}",
        _ => "Edit rejected."
    };

    /// <summary>Returns what a cell should show in the grid.</summary>
    /// <param name="cellRef">The cell to read.</param>
    /// <returns>
    /// The cell's value as text, which for a cell in error is the usual
    /// spreadsheet marker such as "#DIV/0!".
    /// </returns>
    private string DisplayValueFor(CellRef cellRef) => _engine.GetValue(cellRef).ToString();

    /// <summary>Tints a cell to show that something about it was refused.</summary>
    /// <param name="rowIndex">The grid row holding the cell.</param>
    /// <param name="colIndex">The grid column holding the cell.</param>
    private void FlashError(int rowIndex, int colIndex)
    {
        _grid.Rows[rowIndex].Cells[colIndex].Style.BackColor = ErrorColor;
    }

    // ── Observer callbacks ────────────────────────────────────────

    /// <summary>
    /// Redraws the cells the engine reports as changed, tinting any that ended
    /// up in error.
    /// </summary>
    /// <param name="changeSet">The cells whose values changed.</param>
    /// <remarks>
    /// This is how a single edit updates everything that depends on it: the
    /// grid is told once, with the whole set, and never has to work out for
    /// itself which cells to refresh. Cells outside the displayed area are
    /// skipped.
    /// </remarks>
    public void OnCellsChanged(CellChangeSet changeSet)
    {
        foreach (var cellRef in changeSet.ChangedCells)
        {
            if (!TryToGridPosition(cellRef, out var r, out var c)) continue;

            var value = _engine.GetValue(cellRef);
            var cell = _grid.Rows[r].Cells[c];
            string displayString = value.ToString();
            cell.Value = string.IsNullOrEmpty(displayString) && value.IsError
                ? "#ERROR!"
                : displayString;

            // Highlight error cells in light red/pink
            cell.Style.BackColor = value.IsError ? ErrorColor : NormalColor;

            // Clear old cycle tooltips if the cell is now valid
            if (!value.IsError)
            {
                cell.ToolTipText = string.Empty;
            }
        }

        // A sort can move the exact rows a filter is watching; a plain
        // edit can change a value a filter reads. Either way, whatever
        // is currently filtered needs its visible-row set recomputed.
        RefreshFilterVisibility();
    }

    /// <summary>
    /// Reports a refused edit that would have made a cell depend on itself,
    /// naming the loop in the status line and tinting every cell caught up in
    /// it.
    /// </summary>
    /// <param name="cyclePath">
    /// The cells that lead round the loop, with the edited cell appearing
    /// again at the end.
    /// </param>
    public void OnCircularReference(IReadOnlyList<CellRef> cyclePath)
    {
        _statusLabel.Text = $"Circular reference: {string.Join(" \u2192 ", cyclePath.Select(c => c.ToA1()))}";
        _statusLabel.BackColor = ErrorColor;

        foreach (var cellRef in cyclePath)
            if (TryToGridPosition(cellRef, out var r, out var c))
            {
                var cell = _grid.Rows[r].Cells[c];

                // Set cell value to explicit error text (Fixes Issue 6)
                cell.Value = "#CIRCULAR!";
                cell.Style.BackColor = ErrorColor;

                // Show exact cycle path when hovering over the cell
                cell.ToolTipText = $"Circular Reference Chain: {cyclePath}";
            }
    }

    // ── Selection / formula bar ─────────────────────────────────────

    /// <summary>Keeps the formula bar in step as the user moves around the grid.</summary>
    /// <param name="sender">The grid whose selection moved.</param>
    /// <param name="e">Carries no extra detail.</param>
    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateFormulaBar();

    /// <summary>
    /// Shows the selected cell's address and the text behind it in the formula
    /// bar. Does nothing when no cell is selected.
    /// </summary>
    private void UpdateFormulaBar()
    {
        if (_grid.CurrentCell is null) return;

        var cellRef = ToCellRef(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex);
        _addressLabel.Text = cellRef.ToA1();
        _formulaBar.Text = _engine.GetFormula(cellRef);
    }

    // ── Undo / Redo ──────────────────────────────────────────────

    /// <summary>Handles the undo and redo keyboard shortcuts.</summary>
    /// <param name="sender">The window the key was pressed in.</param>
    /// <param name="e">Which key was pressed, and with which modifiers.</param>
    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z) { TryUndo(); e.Handled = true; }
        if (e.Control && e.KeyCode == Keys.Y) { TryRedo(); e.Handled = true; }
    }

    /// <summary>
    /// Undoes the last operation and brings the window back into step with it.
    /// Does nothing when there is nothing to undo.
    /// </summary>
    private void TryUndo()
    {
        if (!_engine.CanUndo) return;
        _engine.Undo();
        UpdateFormulaBar();
        UpdateUndoRedoButtons();
        // Undoing a cell edit fires OnCellsChanged, which already
        // refreshes filter visibility — but undoing a filter command
        // itself does not (filtering never notifies observers), so
        // refresh unconditionally here too.
        RefreshFilterVisibility();
    }

    /// <summary>
    /// Carries out the last undone operation again and brings the window back
    /// into step with it. Does nothing when there is nothing to redo.
    /// </summary>
    private void TryRedo()
    {
        if (!_engine.CanRedo) return;
        _engine.Redo();
        UpdateFormulaBar();
        UpdateUndoRedoButtons();
        RefreshFilterVisibility();
    }

    /// <summary>
    /// Greys out the undo and redo buttons when there is nothing for them to
    /// do.
    /// </summary>
    private void UpdateUndoRedoButtons()
    {
        _undoButton.Enabled = _engine.CanUndo;
        _redoButton.Enabled = _engine.CanRedo;
    }

    // ── Data validation rule assignment (Group C feature) ───────────

    /// <summary>
    /// Asks the user for a lower and upper bound, then holds the selected cell
    /// to it from now on. Does nothing if they cancel, or if no cell is
    /// selected. The value already in the cell is left alone.
    /// </summary>
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

    /// <summary>
    /// Lifts any validation rule from the selected cell. Does nothing if no
    /// cell is selected, and is harmless if the cell had no rule.
    /// </summary>
    private void ClearRuleOnSelectedCell()
    {
        if (_grid.CurrentCell is null) return;
        var cellRef = ToCellRef(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex);
        _engine.ClearValidationRule(cellRef);
        _statusLabel.Text = $"Rule cleared on {cellRef.ToA1()}";
        _statusLabel.BackColor = Color.WhiteSmoke;
    }

    // ── Sorting and Filtering (Group C feature) ──────────────────────

    /// <summary>Works out which range of cells the user has selected.</summary>
    /// <param name="range">
    /// When this returns, the smallest rectangle covering every selected cell.
    /// A selection with gaps in it is squared off into that rectangle.
    /// </param>
    /// <returns>
    /// true if anything was selected; otherwise, false.
    /// </returns>
    private bool TryGetSelectedRange(out CellRange range)
    {
        range = default;
        if (_grid.SelectedCells.Count == 0) return false;

        int minRow = int.MaxValue, maxRow = int.MinValue, minCol = int.MaxValue, maxCol = int.MinValue;
        foreach (DataGridViewCell cell in _grid.SelectedCells)
        {
            minRow = Math.Min(minRow, cell.RowIndex);
            maxRow = Math.Max(maxRow, cell.RowIndex);
            minCol = Math.Min(minCol, cell.ColumnIndex);
            maxCol = Math.Max(maxCol, cell.ColumnIndex);
        }

        range = new CellRange(ToCellRef(minRow, minCol), ToCellRef(maxRow, maxCol));
        return true;
    }

    /// <summary>
    /// Asks the user how to sort the selected range, then sorts it.
    /// </summary>
    /// <remarks>
    /// Says so in the status line if nothing is selected, or if the sort is
    /// turned down because a moved formula would end up pointing off the
    /// sheet. A sort that is turned down leaves the range exactly as it was.
    /// </remarks>
    private void ShowSortDialog()
    {
        if (!TryGetSelectedRange(out var range))
        {
            _statusLabel.Text = "Select a range of cells to sort first.";
            _statusLabel.BackColor = ErrorColor;
            return;
        }

        using var dialog = new SortRangeDialog(range);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var comparer = dialog.Ascending ? (ISortComparer)new AscendingComparer() : new DescendingComparer();
        var keys = new[] { new SortKey(dialog.SelectedColumn, comparer) };

        var result = _engine.SortRange(range, keys, dialog.HasHeader);

        if (result.Success)
        {
            _statusLabel.Text = $"Sorted {range}.";
            _statusLabel.BackColor = Color.WhiteSmoke;
        }
        else
        {
            _statusLabel.Text = DescribeFailure(result);
            _statusLabel.BackColor = ErrorColor;
        }

        UpdateUndoRedoButtons();
        // OnCellsChanged already fires (and refreshes filter visibility)
        // when the sort succeeds; a rejected sort touched nothing.
    }

    /// <summary>
    /// Asks the user how to filter the selected range, then hides the rows
    /// that do not match. Says so in the status line if nothing is selected.
    /// </summary>
    private void ShowFilterDialog()
    {
        if (!TryGetSelectedRange(out var range))
        {
            _statusLabel.Text = "Select a range of cells to filter first.";
            _statusLabel.BackColor = ErrorColor;
            return;
        }

        using var dialog = new FilterRangeDialog(range);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _engine.SetFilter(range, dialog.SelectedColumn, dialog.Filter!);

        _activeFilterRange = range;
        _activeFilterColumns.Add(dialog.SelectedColumn);

        RefreshFilterVisibility();
        UpdateUndoRedoButtons();
        _statusLabel.Text = $"Filter applied to {range}.";
        _statusLabel.BackColor = Color.WhiteSmoke;
    }

    /// <summary>
    /// Lifts every filter currently in force and shows all the rows again.
    /// Says so in the status line if no filter was in force.
    /// </summary>
    private void ClearActiveFilter()
    {
        if (_activeFilterRange is not { } range)
        {
            _statusLabel.Text = "No active filter to clear.";
            _statusLabel.BackColor = Color.WhiteSmoke;
            return;
        }

        foreach (var column in _activeFilterColumns)
            _engine.ClearFilter(range, column);

        _activeFilterColumns.Clear();
        _activeFilterRange = null;

        foreach (DataGridViewRow row in _grid.Rows)
            row.Visible = true;

        UpdateUndoRedoButtons();
        _statusLabel.Text = "Filter cleared.";
        _statusLabel.BackColor = Color.WhiteSmoke;
    }

    /// <summary>
    /// Brings the shown and hidden rows back into line with whatever filter is
    /// in force. Does nothing when there is no filter.
    /// </summary>
    /// <remarks>
    /// Worth doing again after any edit, since a changed value can move a row
    /// in or out of a filter, and a sort can move the very rows the filter is
    /// watching. Only which rows are shown changes; no value or formula is
    /// touched.
    /// </remarks>
    private void RefreshFilterVisibility()
    {
        if (_activeFilterRange is not { } range) return;

        var visibleRows = new HashSet<int>(_engine.GetVisibleRows(range));
        for (int r = range.TopLeft.Row; r <= range.BottomRight.Row; r++)
        {
            if (!TryToGridPosition(new CellRef(r, range.TopLeft.Column), out var rowIndex, out _)) continue;
            _grid.Rows[rowIndex].Visible = visibleRows.Contains(r);
        }
    }

    /// <summary>
    /// Runs when the window first opens. Nothing is left to do here: the grid
    /// and its controls are already built by the constructor.
    /// </summary>
    /// <param name="sender">The window being opened.</param>
    /// <param name="e">Carries no extra detail.</param>
    private void Form1_Load(object sender, EventArgs e)
    {

    }
}