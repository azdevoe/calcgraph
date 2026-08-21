using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Leaf: a reference to a single cell, e.g. B2.
/// RI: Ref.Row &gt;= 1 AND Ref.Column &gt;= 1.
/// </summary>
public sealed class CellRefExpression : IExpression
{
    public CellRef Ref { get; }

    public CellRefExpression(CellRef cellRef)
    {
        if (cellRef.Row < 1 || cellRef.Column < 1)
            throw new ArgumentException(
                $"Row and Column must be >= 1, got ({cellRef.Row}, {cellRef.Column}).",
                nameof(cellRef));
        Ref = cellRef;
    }

    public CellValue Evaluate(IEvalContext context) => context.GetCellValue(Ref);

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitCellRef(this);

    public override string ToString() => $"CellRef({Ref})";
}