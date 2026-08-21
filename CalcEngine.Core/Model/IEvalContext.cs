using CalcEngine.Core.Expressions;

namespace CalcEngine.Core.Model;

/// <summary>
/// Evaluation environment passed to IExpression.Evaluate.
/// </summary>
public interface IEvalContext
{
    /// <summary>Current value of the cell. Absent cell returns CellValue.Empty.</summary>
    CellValue GetCellValue(CellRef cellRef);

    /// <summary>Every value in the range, row-major order.</summary>
    IReadOnlyList<CellValue> GetRangeValues(CellRange range);

    /// <summary>
    /// Evaluates a function call. Args are unevaluated so IF can short-circuit.
    /// </summary>
    CellValue CallFunction(string name, IReadOnlyList<IExpression> args);
}