using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Core.Validators;
using RuleKeeper.Sdk;
using AnalysisContext = RuleKeeper.Sdk.AnalysisContext;

namespace RuleKeeper.Core.Rules;

/// <summary>
/// Executes rules against source code.
/// </summary>
public class RuleExecutor
{
    private readonly RuleRegistry _registry;
    private readonly ValidatorRegistry _validatorRegistry;
    private readonly ValidatorFactory _validatorFactory;

    public RuleExecutor(RuleRegistry registry, ValidatorRegistry? validatorRegistry = null)
    {
        _registry = registry;
        _validatorRegistry = validatorRegistry ?? new ValidatorRegistry();
        _validatorFactory = new ValidatorFactory(_validatorRegistry);
    }
    /// <summary>
    /// Gets the validator registry.
    /// </summary>
    public ValidatorRegistry ValidatorRegistry => _validatorRegistry;

    /// <summary>
    /// Gets the validator factory.
    /// </summary>
    public ValidatorFactory ValidatorFactory => _validatorFactory;

    /// <summary>
    /// Executes all enabled rules from the configuration against the given context.
    /// </summary>
    public async Task<List<Violation>> ExecuteAsync(
        AnalysisContext context,
        RuleKeeperConfig config,
        CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();
        foreach (var (_, category) in config.CodingStandards)
        {
            if (!category.Enabled)
                continue;

            if (IsExcluded(context.FilePath, category.Exclude))
                continue;

            foreach (var ruleDefinition in category.Rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ruleDefinition.IsEnabled)
                    continue;

                if (IsExcluded(context.FilePath, ruleDefinition.Exclude))
                    continue;

                if (!string.IsNullOrEmpty(ruleDefinition.FilePattern))
                {
                    var matcher = new Matcher();
                    matcher.AddInclude(ruleDefinition.FilePattern);
                    if (!matcher.Match(context.FilePath).HasMatches)
                        continue;
                }

                // Check language filter
                if (ruleDefinition.Languages.Count > 0 &&
                    !ruleDefinition.Languages.Any(l => l.Equals(context.Language.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var ruleViolations = await ExecuteRuleAsync(context, ruleDefinition, category.Severity, cancellationToken);
                violations.AddRange(ruleViolations);
            }
        }

        foreach (var (policyName, policy) in config.PrebuiltPolicies)
        {
            if (!policy.Enabled)
                continue;

            if (IsExcluded(context.FilePath, policy.Exclude))
                continue;

            // Pre-built policies are handled by built-in analyzers
            // We look up the policy rules in the registry
            var policyRules = _registry.GetRulesByCategory(policyName);
            foreach (var ruleInfo in policyRules)
            {
                if (policy.SkipRules.Contains(ruleInfo.RuleId, StringComparer.OrdinalIgnoreCase))
                    continue;

                var analyzer = _registry.CreateAnalyzer(ruleInfo.RuleId);
                if (analyzer == null)
                    continue;

                analyzer.Initialize(new Dictionary<string, object>());
                var ruleContext = context with
                {
                    Severity = policy.Severity ?? ruleInfo.DefaultSeverity
                };

                var ruleViolations = analyzer.Analyze(ruleContext);
                violations.AddRange(ruleViolations);
            }
        }

        return violations;
    }

    /// <summary>
    /// Executes a single rule definition.
    /// </summary>
    private async Task<List<Violation>> ExecuteRuleAsync(
        AnalysisContext context,
        RuleDefinition rule,
        SeverityLevel? categorySeverity,
        CancellationToken cancellationToken)
    {
        var violations = new List<Violation>();
        var severity = rule.Severity;
        if (categorySeverity.HasValue)
            severity = categorySeverity.Value;

        var validationType = rule.GetValidationType();

        switch (validationType)
        {
            case RuleValidationType.BuiltIn:
                // Try built-in analyzer first
                var analyzer = _registry.CreateAnalyzer(rule.Id!, rule.Parameters);
                if (analyzer != null)
                {
                    var ruleContext = context with
                    {
                        Severity = severity,
                        CustomMessage = rule.Message,
                        FixHint = rule.FixHint
                    };

                    violations.AddRange(analyzer.Analyze(ruleContext));
                    return violations;
                }
                break;

            case RuleValidationType.EnhancedPattern:
            case RuleValidationType.AstQuery:
            case RuleValidationType.MultiMatch:
            case RuleValidationType.Expression:
            case RuleValidationType.Script:
            case RuleValidationType.Validator:
            case RuleValidationType.CustomValidator:
                // Use the new validator system
                var customViolations = await ExecuteCustomValidatorAsync(context, rule, severity, cancellationToken);
                violations.AddRange(customViolations);
                return violations;

            case RuleValidationType.SimplePattern:
                // Legacy simple pattern matching
                await Task.Run(() =>
                {
                    violations.AddRange(ExecutePatternRule(context, rule, severity));
                }, cancellationToken);
                return violations;
        }

        return violations;
    }

    /// <summary>
    /// Executes a rule using the custom validator system.
    /// </summary>
    private async Task<List<Violation>> ExecuteCustomValidatorAsync(
        AnalysisContext context,
        RuleDefinition rule,
        SeverityLevel severity,
        CancellationToken cancellationToken)
    {
        var violations = new List<Violation>();

        // Create validator from rule definition
        var validator = _validatorFactory.CreateFromRuleDefinition(rule);
        if (validator == null)
            return violations;

        // Check language support
        if (!validator.SupportsLanguage(context.Language))
            return violations;

        // Initialize with rule parameters
        validator.Initialize(rule.Parameters);

        // Create validation context
        var validationContext = ValidationContext.FromRoslynContext(context, rule.Id, rule.Name);
        validationContext = validationContext.WithOverrides(
            severity: severity,
            message: rule.Message,
            fixHint: rule.FixHint,
            additionalParameters: rule.Parameters);

        // Execute validator
        var validatorViolations = await validator.ValidateAsync(validationContext, cancellationToken);
        violations.AddRange(validatorViolations);

        return violations;
    }

    /// <summary>
    /// Executes a pattern-based rule using regex.
    /// </summary>
    private List<Violation> ExecutePatternRule(
        AnalysisContext context,
        RuleDefinition rule,
        SeverityLevel severity)
    {
        var violations = new List<Violation>();
        var sourceText = context.SyntaxTree.GetText();
        var text = sourceText.ToString();
        if (!string.IsNullOrEmpty(rule.AntiPattern))
        {
            var regex = new Regex(rule.AntiPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(text))
            {
                var lineSpan = sourceText.Lines.GetLinePositionSpan(TextSpan.FromBounds(match.Index, match.Index + match.Length));

                violations.Add(new Violation
                {
                    RuleId = rule.Id ?? "PATTERN",
                    RuleName = rule.Name ?? "Pattern Rule",
                    Message = rule.Message ?? $"Anti-pattern detected: {match.Value}",
                    Severity = severity,
                    FilePath = context.FilePath,
                    StartLine = lineSpan.Start.Line + 1,
                    StartColumn = lineSpan.Start.Character + 1,
                    EndLine = lineSpan.End.Line + 1,
                    EndColumn = lineSpan.End.Character + 1,
                    FixHint = rule.FixHint,
                    CodeSnippet = sourceText.Lines[lineSpan.Start.Line].ToString()
                });
            }
        }

        if (string.IsNullOrEmpty(rule.Pattern) || rule.AppliesTo.Count <= 0) return violations;
        {
            var syntaxElements = GetSyntaxElements(context.SyntaxTree.GetRoot(), rule.AppliesTo);
            var regex = new Regex(rule.Pattern);

            foreach (var element in syntaxElements)
            {
                var identifier = GetIdentifier(element);
                if (identifier == null || regex.IsMatch(identifier)) continue;
                var location = element.GetLocation();
                violations.Add(Violation.FromLocation(
                    location,
                    rule.Id ?? "PATTERN",
                    rule.Name ?? "Pattern Rule",
                    rule.Message ?? $"'{identifier}' does not match required pattern",
                    severity,
                    rule.FixHint
                ));
            }
        }

        return violations;
    }

    private IEnumerable<SyntaxNode> GetSyntaxElements(SyntaxNode root, List<string> elementTypes)
    {
        foreach (var node in root.DescendantNodes())
        {
            var nodeType = node.GetType().Name.Replace("Syntax", "");
            if (elementTypes.Any(e => nodeType.Contains(e, StringComparison.OrdinalIgnoreCase)))
            {
                yield return node;
            }
        }
    }

    private string? GetIdentifier(SyntaxNode node)
    {
        var identifierProperty = node.GetType().GetProperty("Identifier");
        if (identifierProperty != null)
        {
            var identifier = identifierProperty.GetValue(node);
            if (identifier is SyntaxToken token)
            {
                return token.Text;
            }
        }
        return null;
    }

    private bool IsExcluded(string filePath, List<string> excludePatterns)
    {
        if (excludePatterns.Count == 0)
            return false;

        var matcher = new Matcher();
        foreach (var pattern in excludePatterns)
        {
            matcher.AddInclude(pattern);
        }

        return matcher.Match(filePath).HasMatches;
    }
}
