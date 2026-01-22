namespace RuleKeeper.Sdk;

/// <summary>
/// Interface for components that can provide code fixes for violations.
/// Implement this interface to create custom fix providers.
/// </summary>
public interface IFixProvider
{
    /// <summary>
    /// The rule ID(s) this provider can fix.
    /// </summary>
    IEnumerable<string> SupportedRuleIds { get; }

    /// <summary>
    /// Whether this provider can fix the given violation.
    /// </summary>
    /// <param name="violation">The violation to check.</param>
    /// <returns>True if this provider can fix the violation.</returns>
    bool CanFix(Violation violation);

    /// <summary>
    /// Gets the available fixes for a violation.
    /// </summary>
    /// <param name="violation">The violation to fix.</param>
    /// <param name="sourceCode">The source code of the file containing the violation.</param>
    /// <returns>Zero or more fixes that can resolve the violation.</returns>
    IEnumerable<CodeFix> GetFixes(Violation violation, string sourceCode);
}
