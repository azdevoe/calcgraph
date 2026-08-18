namespace CalcEngine.Core;

/// <summary>
/// Holds at most one IValidationRule per cell. CalculationEngine
/// consults this on every edit and delegates the actual check to the
/// rule via IValidationRule.Validate — this class is pure storage.
/// </summary>
public sealed class ValidationRegistry
{
    private readonly Dictionary<CellRef, IValidationRule> _rules = new();

    /// <summary>Attaches rule to cellRef, replacing any rule already there.</summary>
    public void SetRule(CellRef cellRef, IValidationRule rule) => _rules[cellRef] = rule;

    /// <summary>Removes any rule attached to cellRef. A no-op if none was set.</summary>
    public void ClearRule(CellRef cellRef) => _rules.Remove(cellRef);

    /// <summary>The rule attached to cellRef, or null if none.</summary>
    public IValidationRule? GetRule(CellRef cellRef) =>
        _rules.TryGetValue(cellRef, out var rule) ? rule : null;
}