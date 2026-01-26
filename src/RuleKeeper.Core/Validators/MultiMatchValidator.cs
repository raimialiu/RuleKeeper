using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Validator that combines multiple conditions with AND/OR/NONE logic.
/// </summary>
public class MultiMatchValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _name;
    private readonly string? _description;
    private readonly MatchConfig _match;
    private readonly string? _defaultMessage;
    private readonly ValidatorFactory _factory;

    public override string ValidatorId => _validatorId;
    public override string Name => _name;
    public override string? Description => _description;

    public MultiMatchValidator(
        string validatorId,
        string name,
        string? description,
        MatchConfig match,
        string? defaultMessage,
        ValidatorFactory factory)
    {
        _validatorId = validatorId;
        _name = name;
        _description = description;
        _match = match;
        _defaultMessage = defaultMessage;
        _factory = factory;
    }

    public override async Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var result = await EvaluateMatchAsync(_match, context, cancellationToken);

        if (result.Matched)
        {
            return result.Violations;
        }

        return Enumerable.Empty<Violation>();
    }

    private async Task<MatchResult> EvaluateMatchAsync(MatchConfig match, ValidationContext context, CancellationToken cancellationToken)
    {
        var allViolations = new List<Violation>();

        // Evaluate "all" conditions (AND logic)
        if (match.All != null && match.All.Count > 0)
        {
            var allResults = new List<MatchResult>();

            foreach (var condition in match.All)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await EvaluateConditionAsync(condition, context, cancellationToken);
                allResults.Add(result);

                if (!result.Matched && match.ShortCircuit)
                {
                    // Short-circuit: one failed, so AND fails
                    return new MatchResult { Matched = false };
                }
            }

            // All must match
            if (allResults.Any(r => !r.Matched))
            {
                return new MatchResult { Matched = false };
            }

            // Collect violations from all matched conditions
            allViolations.AddRange(allResults.SelectMany(r => r.Violations));
        }

        // Evaluate "any" conditions (OR logic)
        if (match.Any != null && match.Any.Count > 0)
        {
            var anyResults = new List<MatchResult>();
            var matchedCount = 0;

            foreach (var condition in match.Any)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await EvaluateConditionAsync(condition, context, cancellationToken);
                anyResults.Add(result);

                if (result.Matched)
                {
                    matchedCount++;
                    allViolations.AddRange(result.Violations);

                    if (match.ShortCircuit && matchedCount >= match.MinAnyMatches)
                    {
                        // Short-circuit: enough matched
                        break;
                    }
                }
            }

            // Check if enough matched
            if (matchedCount < match.MinAnyMatches)
            {
                return new MatchResult { Matched = false };
            }
        }

        // Evaluate "none" conditions (NOT logic)
        if (match.None != null && match.None.Count > 0)
        {
            foreach (var condition in match.None)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await EvaluateConditionAsync(condition, context, cancellationToken);

                if (result.Matched)
                {
                    // Something matched that shouldn't have - fail
                    if (match.ShortCircuit)
                    {
                        return new MatchResult { Matched = false };
                    }
                }
            }
        }

        // If we get here, all conditions passed
        // If no violations were collected, create a default one
        if (allViolations.Count == 0)
        {
            allViolations.Add(CreateDefaultViolation(context));
        }

        return new MatchResult
        {
            Matched = true,
            Violations = allViolations
        };
    }

    private async Task<MatchResult> EvaluateConditionAsync(MatchCondition condition, ValidationContext context, CancellationToken cancellationToken)
    {
        IEnumerable<Violation> violations = Enumerable.Empty<Violation>();
        bool matched;

        if (condition.Pattern != null)
        {
            var validator = new RegexValidator(
                $"{_validatorId}-pattern",
                $"{_name} Pattern",
                null,
                condition.Pattern,
                _defaultMessage);
            validator.Initialize(Parameters);

            violations = await validator.ValidateAsync(context, cancellationToken);
            matched = violations.Any();
        }
        else if (condition.AstQuery != null)
        {
            var validator = new AstQueryValidator(
                $"{_validatorId}-ast",
                $"{_name} AST Query",
                null,
                condition.AstQuery,
                _defaultMessage);
            validator.Initialize(Parameters);

            violations = await validator.ValidateAsync(context, cancellationToken);
            matched = violations.Any();
        }
        else if (condition.Match != null)
        {
            // Nested match - recurse
            var result = await EvaluateMatchAsync(condition.Match, context, cancellationToken);
            matched = result.Matched;
            violations = result.Violations;
        }
        else if (!string.IsNullOrEmpty(condition.Expression))
        {
            // Expression condition
            var expressionConfig = new ExpressionConfig { Condition = condition.Expression };
            var validator = new ExpressionValidator(
                $"{_validatorId}-expr",
                $"{_name} Expression",
                null,
                expressionConfig,
                _defaultMessage);
            validator.Initialize(Parameters);

            violations = await validator.ValidateAsync(context, cancellationToken);
            matched = violations.Any();
        }
        else
        {
            // No condition configured - treat as no match
            matched = false;
        }

        // Apply negation
        if (condition.Negate)
        {
            matched = !matched;
            if (matched)
            {
                // Negated condition matched, but we don't have violations from it
                // Create a generic violation
                violations = new[] { CreateConditionViolation(context, condition) };
            }
            else
            {
                violations = Enumerable.Empty<Violation>();
            }
        }

        return new MatchResult
        {
            Matched = matched,
            Violations = violations.ToList()
        };
    }

    private Violation CreateDefaultViolation(ValidationContext context)
    {
        var message = context.CustomMessage ?? _defaultMessage ?? "Multi-pattern match rule violated";
        return CreateViolation(context, message, 1, 1);
    }

    private Violation CreateConditionViolation(ValidationContext context, MatchCondition condition)
    {
        var label = condition.Label ?? "condition";
        var message = context.CustomMessage ?? _defaultMessage ?? $"Negated {label} matched";
        return CreateViolation(context, message, 1, 1);
    }

    private class MatchResult
    {
        public bool Matched { get; init; }
        public List<Violation> Violations { get; init; } = new();
    }
}
