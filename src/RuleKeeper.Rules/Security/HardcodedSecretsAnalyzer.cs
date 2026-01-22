using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Rules.Security;

/// <summary>
/// Detects hardcoded secrets, passwords, and API keys.
/// </summary>
[Rule("CS-SEC-002",
    Name = "No Hardcoded Secrets",
    Description = "Detects hardcoded passwords, API keys, and secrets",
    Severity = SeverityLevel.Critical,
    Category = "security")]
public class HardcodedSecretsAnalyzer : BaseRuleAnalyzer
{
    private static readonly Regex SecretPatterns = new(
        @"(password|passwd|pwd|secret|apikey|api_key|api-key|token|auth|bearer|credential|connectionstring|conn_str)\s*[:=]\s*[""'][^""']{4,}[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HighEntropyPattern = new(
        @"[""'][A-Za-z0-9+/=]{32,}[""']",
        RegexOptions.Compiled);

    private static readonly string[] SkipPatterns = new[]
    {
        "placeholder", "example", "sample", "test", "dummy", "mock",
        "xxx", "yyy", "zzz", "abc", "123", "todo", "fixme"
    };

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxTree.GetRoot(context.CancellationToken);
        var sourceText = context.SyntaxTree.GetText();
        var text = sourceText.ToString();

        // Check for secret patterns in string literals
        var matches = SecretPatterns.Matches(text);
        foreach (Match match in matches)
        {
            if (ShouldSkip(match.Value))
                continue;

            var lineSpan = sourceText.Lines.GetLinePositionSpan(
                TextSpan.FromBounds(match.Index, match.Index + match.Length));

            yield return new Violation
            {
                RuleId = RuleId,
                RuleName = RuleName,
                Message = "Potential hardcoded secret detected",
                Severity = context.Severity,
                FilePath = context.FilePath,
                StartLine = lineSpan.Start.Line + 1,
                StartColumn = lineSpan.Start.Character + 1,
                EndLine = lineSpan.End.Line + 1,
                EndColumn = lineSpan.End.Character + 1,
                FixHint = "Use environment variables, user secrets, or a secure vault",
                CodeSnippet = MaskSecret(sourceText.Lines[lineSpan.Start.Line].ToString())
            };
        }

        // Check for variable assignments with suspicious names
        var assignments = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer != null);

        foreach (var assignment in assignments)
        {
            var name = assignment.Identifier.Text.ToLowerInvariant();
            if (IsSecretVariableName(name) && assignment.Initializer?.Value is LiteralExpressionSyntax literal)
            {
                var value = literal.Token.ValueText;
                if (!string.IsNullOrEmpty(value) && value.Length >= 4 && !ShouldSkip(value))
                {
                    yield return CreateViolation(
                        assignment.GetLocation(),
                        $"Potential hardcoded secret in variable '{assignment.Identifier.Text}'",
                        context,
                        "Use environment variables or configuration"
                    );
                }
            }
        }
    }

    private static bool IsSecretVariableName(string name)
    {
        var patterns = new[]
        {
            "password", "passwd", "pwd", "secret", "apikey", "api_key",
            "token", "auth", "bearer", "credential", "key", "cert"
        };

        return patterns.Any(p => name.Contains(p));
    }

    private static bool ShouldSkip(string value)
    {
        var lowerValue = value.ToLowerInvariant();
        return SkipPatterns.Any(p => lowerValue.Contains(p)) ||
               string.IsNullOrWhiteSpace(value) ||
               value.Length < 4;
    }

    private static string MaskSecret(string line)
    {
        // Mask potential secrets in the code snippet
        return Regex.Replace(line, @"[""'][^""']{4,}[""']", "\"***MASKED***\"");
    }
}
