namespace CalcEngine.Core;

/// <summary>
/// LOOKUP(searchValue, range) — searches range in order and returns the
/// first matching value. Exercises the RangeExpression path most heavily;
/// a LOOKUP over B2:B45 registers forty-four dependency edges.
/// </summary>
public sealed class LookupStrategy : IFunctionStrategy
{
    public string Name => "LOOKUP";
    public int MinArgs => 2;
    public int MaxArgs => 2;

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