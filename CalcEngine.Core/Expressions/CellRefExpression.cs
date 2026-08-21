using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Leaf: a reference to a single cell, e.g. B2.
/// The row and column are both at least 1, since a reference outside the
/// sheet is not a cell anyone can point at.
/// </summary>
public sealed class CellRefExpression : IExpression
{
    /// <summary>Gets the address this node points at.</summary>
    public CellRef Ref { get; }

    /// <summary>Initializes a new reference to a single cell.</summary>
    /// <param name="cellRef">The address to point at.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="cellRef"/> has a row or column below 1, which is
    /// outside the sheet.
    /// </exception>
    public CellRefExpression(CellRef cellRef)
    {
        if (cellRef.Row < 1 || cellRef.Column < 1)
            throw new ArgumentException(
                $"Row and Column must be >= 1, got ({cellRef.Row}, {cellRef.Column}).",
                nameof(cellRef));
        Ref = cellRef;
    }

    /// <summary>Reads the current value of the cell this node points at.</summary>
    /// <param name="context">Supplies the cell values.</param>
    /// <returns>
    /// The cell's value, or CellValue.Empty if nothing has been entered there.
    /// </returns>
    public CellValue Evaluate(IEvalContext context) => context.GetCellValue(Ref);

    /// <summary>Calls the visitor's VisitCellRef method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitCellRef(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form CellRef(B2).</returns>
    public override string ToString() => $"CellRef({Ref})";
}