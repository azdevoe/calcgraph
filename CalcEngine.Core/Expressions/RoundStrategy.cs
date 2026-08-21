using CalcEngine.Core.Functions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>ROUND(number, digits) — rounds to the given number of decimal places, banker's-rounding-free (MidpointRounding.AwayFromZero, matching Excel).</summary>
public sealed class RoundStrategy : IFunctionStrategy
{
    public string Name => "ROUND";
    public int MinArgs => 2;
    public int MaxArgs => 2;

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