using System.Text.RegularExpressions;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

namespace RuleKeeper.Core.Fixing.Providers;

/// <summary>
/// Provides fixes for naming convention violations.
/// </summary>
[FixProvider("Naming Fix Provider", Category = "Naming", Description = "Fixes naming convention violations (PascalCase, camelCase, etc.)")]
public class NamingFixProvider : IFixProvider
{
    public IEnumerable<string> SupportedRuleIds => new[]
    {
        "CS-NAME-001", // Class naming (PascalCase)
        "CS-NAME-002", // Method naming (PascalCase)
        "CS-NAME-003", // Async suffix
        "CS-NAME-004", // Private field (_camelCase)
        "CS-NAME-005", // Constant naming (PascalCase)
        "CS-NAME-006", // Property naming (PascalCase)
        "CS-NAME-007", // Interface prefix (I)
        "CS-NAME-008", // Parameter naming (camelCase)
        "CS-NAME-009", // Local variable (camelCase)
        "CS-NAME-010"  // Type parameter (T prefix)
    };

    public bool CanFix(Violation violation)
    {
        return SupportedRuleIds.Contains(violation.RuleId) &&
               !string.IsNullOrEmpty(violation.FilePath);
    }

    public IEnumerable<CodeFix> GetFixes(Violation violation, string sourceCode)
    {
        var fix = violation.RuleId switch
        {
            "CS-NAME-001" => FixClassNaming(violation, sourceCode),
            "CS-NAME-002" => FixMethodNaming(violation, sourceCode),
            "CS-NAME-003" => FixAsyncNaming(violation, sourceCode),
            "CS-NAME-004" => FixPrivateFieldNaming(violation, sourceCode),
            "CS-NAME-005" => FixConstantNaming(violation, sourceCode),
            "CS-NAME-006" => FixPropertyNaming(violation, sourceCode),
            "CS-NAME-007" => FixInterfaceNaming(violation, sourceCode),
            "CS-NAME-008" => FixParameterNaming(violation, sourceCode),
            "CS-NAME-009" => FixLocalVariableNaming(violation, sourceCode),
            "CS-NAME-010" => FixTypeParameterNaming(violation, sourceCode),
            _ => null
        };

        if (fix != null)
        {
            yield return fix;
        }
    }

    private CodeFix? FixClassNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Class '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToPascalCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename class to PascalCase");
    }

    private CodeFix? FixMethodNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Method '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToPascalCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename method to PascalCase");
    }

    private CodeFix? FixAsyncNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Async method '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = name.EndsWith("Async") ? name : name + "Async";
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Add 'Async' suffix");
    }

    private CodeFix? FixPrivateFieldNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Private field '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToUnderscoreCamelCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename field to _camelCase");
    }

    private CodeFix? FixConstantNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Constant '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToPascalCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename constant to PascalCase");
    }

    private CodeFix? FixPropertyNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Property '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToPascalCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename property to PascalCase");
    }

    private CodeFix? FixInterfaceNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Interface '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1])
            ? name
            : "I" + ToPascalCase(name.TrimStart('I'));

        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Add 'I' prefix to interface");
    }

    private CodeFix? FixParameterNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Parameter '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToCamelCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename parameter to camelCase");
    }

    private CodeFix? FixLocalVariableNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Local variable '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = ToCamelCase(name);
        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Rename variable to camelCase");
    }

    private CodeFix? FixTypeParameterNaming(Violation violation, string sourceCode)
    {
        var name = ExtractIdentifier(violation.Message, "Type parameter '", "'");
        if (string.IsNullOrEmpty(name)) return null;

        var newName = name.StartsWith("T") && (name.Length == 1 || char.IsUpper(name[1]))
            ? name
            : "T" + ToPascalCase(name.TrimStart('T'));

        if (newName == name) return null;

        return CreateRenameFix(violation, name, newName, "Add 'T' prefix to type parameter");
    }

    private CodeFix CreateRenameFix(Violation violation, string oldName, string newName, string description)
    {
        return new CodeFix
        {
            FixId = $"FIX-{violation.RuleId}-{Guid.NewGuid():N}",
            RuleId = violation.RuleId,
            Description = $"{description}: '{oldName}' → '{newName}'",
            FilePath = violation.FilePath,
            Operation = FixOperation.Replace,
            StartLine = violation.StartLine,
            StartColumn = violation.StartColumn,
            EndLine = violation.EndLine,
            EndColumn = violation.EndColumn,
            OriginalText = oldName,
            ReplacementText = newName,
            Severity = violation.Severity,
            IsSafe = false, // Renaming requires updating all references
            Category = "naming"
        };
    }

    private static string? ExtractIdentifier(string message, string prefix, string suffix)
    {
        var startIndex = message.IndexOf(prefix);
        if (startIndex < 0) return null;
        startIndex += prefix.Length;

        var endIndex = message.IndexOf(suffix, startIndex);
        if (endIndex < 0) return null;

        return message.Substring(startIndex, endIndex - startIndex);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Remove leading underscores
        input = input.TrimStart('_');

        // Split by underscores or case changes
        var words = Regex.Split(input, @"(?<!^)(?=[A-Z])|_")
            .Where(w => !string.IsNullOrEmpty(w))
            .ToArray();

        return string.Concat(words.Select(w =>
            char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w.Substring(1).ToLowerInvariant() : "")));
    }

    private static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }

    private static string ToUnderscoreCamelCase(string input)
    {
        // Remove existing underscores for processing
        var clean = input.TrimStart('_');
        var camel = ToCamelCase(clean);
        return "_" + camel;
    }
}
