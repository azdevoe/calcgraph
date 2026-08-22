using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>
/// LOOKUP(searchValue, range) — searches range in order and returns the
/// first matching value. Exercises the RangeExpression path most heavily;
/// a LOOKUP over B2:B45 registers forty-four dependency edges.
/// </summary>
public sealed class LookupStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "LOOKUP".</summary>
    public string Name => "LOOKUP";

    /// <summary>Gets the fewest arguments LOOKUP accepts, which is 2.</summary>
    public int MinArgs => 2;

    /// <summary>Gets the most arguments LOOKUP accepts, which is 2.</summary>
    public int MaxArgs => 2;

    /// <summary>Finds a value in a range of cells.</summary>
    /// <param name="args">
    /// Two arguments: the value to look for, and the range to look in. The
    /// second argument must be a range, not a single cell.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The value of the earliest matching cell in row-major order. Numbers
    /// match exactly, text matches ignoring case, and the range need not be
    /// sorted. #N/A if no cell matches, and #VALUE! if the second argument is
    /// not a range. If the value being searched for is itself an error, that
    /// error is the result.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        var searchVal = args[0].Evaluate(context);
        if (searchVal.Kind == ValueKind.Error) return searchVal;

        if (args[1] is not RangeExpression rangeExpr)
            return CellValue.FromError(ErrorKind.Value);

        foreach (var candidate in context.GetRangeValues(rangeExpr.Range))
        {
            if (candidate.Kind == ValueKind.Error) continue; // skip, keep searching
            if (ValuesMatch(searchVal, candidate)) return candidate;
        }

        return CellValue.FromError(ErrorKind.NotAvailable); // #N/A
    }

    private static bool ValuesMatch(CellValue a, CellValue b)
    {
        if (a.Kind == ValueKind.Text && b.Kind == ValueKind.Text)
            return string.Equals(a.AsText(), b.AsText(), StringComparison.OrdinalIgnoreCase);
        if (a.Kind == ValueKind.Text || b.Kind == ValueKind.Text)
            return false;
        return a.AsNumber() == b.AsNumber();
    }
}