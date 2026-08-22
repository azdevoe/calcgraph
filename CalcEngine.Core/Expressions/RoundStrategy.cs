using CalcEngine.Core.Functions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>ROUND(number, digits) — rounds to the given number of decimal places, banker's-rounding-free (MidpointRounding.AwayFromZero, matching Excel).</summary>
public sealed class RoundStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "ROUND".</summary>
    public string Name => "ROUND";

    /// <summary>Gets the smallest number of arguments ROUND accepts, which is 2.</summary>
    public int MinArgs => 2;

    /// <summary>Gets the largest number of arguments ROUND accepts, which is 2.</summary>
    public int MaxArgs => 2;

    /// <summary>Rounds a number to a given number of decimal places.</summary>
    /// <param name="args">
    /// Two arguments: the number to round, and how many decimal places to
    /// keep.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The rounded number. A halfway value rounds away from zero, so 2.5
    /// becomes 3, matching what a spreadsheet user expects. An argument that
    /// is already an error is passed straight through, and text gives #VALUE!.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        var numberVal = args[0].Evaluate(context);
        var digitsVal = args[1].Evaluate(context);

        if (numberVal.Kind == ValueKind.Error) return numberVal;
        if (digitsVal.Kind == ValueKind.Error) return digitsVal;
        if (numberVal.Kind == ValueKind.Text || digitsVal.Kind == ValueKind.Text)
            return CellValue.FromError(ErrorKind.Value);

        int digits = (int)digitsVal.AsNumber();
        double rounded = Math.Round(numberVal.AsNumber(), digits, MidpointRounding.AwayFromZero);
        return CellValue.FromNumber(rounded);
    }
}