using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// Holds at most one IValidationRule per cell. CalculationEngine
/// consults this on every edit and delegates the actual check to the
/// rule via IValidationRule.Validate — this class is pure storage.
/// </summary>
public sealed class ValidationRegistry
{
    private readonly Dictionary<CellRef, IValidationRule> _rules = new();

    /// <summary>
    /// Puts a rule on a cell, replacing any rule already there. A cell can
    /// carry only one rule at a time.
    /// </summary>
    /// <param name="cellRef">The cell to guard.</param>
    /// <param name="rule">The rule its values must satisfy.</param>
    public void SetRule(CellRef cellRef, IValidationRule rule) => _rules[cellRef] = rule;

    /// <summary>
    /// Takes the rule off a cell. Clearing a cell that has no rule makes no
    /// difference.
    /// </summary>
    /// <param name="cellRef">The cell to stop guarding.</param>
    public void ClearRule(CellRef cellRef) => _rules.Remove(cellRef);

    /// <summary>Returns the rule on a cell.</summary>
    /// <param name="cellRef">The cell to ask about.</param>
    /// <returns>The rule guarding that cell, or null if it has none.</returns>
    public IValidationRule? GetRule(CellRef cellRef) =>
        _rules.TryGetValue(cellRef, out var rule) ? rule : null;
}