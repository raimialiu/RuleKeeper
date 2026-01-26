using System.Text.RegularExpressions;
using RuleKeeper.Core.Configuration.Models;

namespace RuleKeeper.Core.Configuration;

/// <summary>
/// Validates RuleKeeper configuration.
/// </summary>
public class ConfigurationValidator
{
    /// <summary>
    /// Validates the configuration and returns any errors.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<ValidationError> Validate(RuleKeeperConfig config)
    {
        var errors = new List<ValidationError>();

        ValidateVersion(config, errors);
        ValidateScan(config.Scan, errors);
        ValidateOutput(config.Output, errors);
        ValidateCodingStandards(config.CodingStandards, errors);
        ValidateCustomValidators(config.CustomValidators, errors);
        ValidateCustomRules(config.CustomRules, errors);

        return errors;
    }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    public void ValidateAndThrow(RuleKeeperConfig config)
    {
        var errors = Validate(config);
        if (errors.Count <= 0) return;
        var message = string.Join(Environment.NewLine, errors.Select(e => $"  - {e.Path}: {e.Message}"));
        throw new ConfigurationException($"Configuration validation failed:{Environment.NewLine}{message}");
    }

    private void ValidateVersion(RuleKeeperConfig config, List<ValidationError> errors)
    {
        if (string.IsNullOrEmpty(config.Version)) return;
        var validVersions = new[] { "1.0", "1" };
        if (!validVersions.Contains(config.Version))
        {
            errors.Add(new ValidationError("version", $"Unsupported version '{config.Version}'. Supported versions: {string.Join(", ", validVersions)}"));
        }
    }

    private void ValidateScan(ScanConfig scan, List<ValidationError> errors)
    {
        if (scan.Include.Count == 0)
        {
            errors.Add(new ValidationError("scan.include", "At least one include pattern is required"));
        }

        foreach (var pattern in scan.Include)
        {
            if (!IsValidGlobPattern(pattern))
            {
                errors.Add(new ValidationError("scan.include", $"Invalid glob pattern: {pattern}"));
            }
        }

        foreach (var pattern in scan.Exclude)
        {
            if (!IsValidGlobPattern(pattern))
            {
                errors.Add(new ValidationError("scan.exclude", $"Invalid glob pattern: {pattern}"));
            }
        }

        if (scan.MaxParallelism < 0)
        {
            errors.Add(new ValidationError("scan.max_parallelism", "Max parallelism must be non-negative"));
        }

        if (scan.MaxFileSize <= 0)
        {
            errors.Add(new ValidationError("scan.max_file_size", "Max file size must be positive"));
        }
    }

    private void ValidateOutput(OutputConfig output, List<ValidationError> errors)
    {
        var validFormats = new[] { "console", "json", "sarif", "html" };
        if (!validFormats.Contains(output.Format.ToLowerInvariant()))
        {
            errors.Add(new ValidationError("output.format", $"Invalid output format '{output.Format}'. Valid formats: {string.Join(", ", validFormats)}"));
        }
    }

    private void ValidateCodingStandards(Dictionary<string, CategoryConfig> standards, List<ValidationError> errors)
    {
        foreach (var (categoryName, category) in standards)
        {
            var categoryPath = $"coding_standards.{categoryName}";

            foreach (var rule in category.Rules)
            {
                var rulePath = $"{categoryPath}.rules.{rule.Id ?? "unnamed"}";
                ValidateRule(rule, rulePath, errors);
            }
        }
    }

    private void ValidateRule(RuleDefinition rule, string path, List<ValidationError> errors)
    {
        if (!string.IsNullOrEmpty(rule.Pattern))
        {
            if (!IsValidRegex(rule.Pattern))
            {
                errors.Add(new ValidationError($"{path}.pattern", $"Invalid regex pattern: {rule.Pattern}"));
            }
        }

        if (!string.IsNullOrEmpty(rule.AntiPattern))
        {
            if (!IsValidRegex(rule.AntiPattern))
            {
                errors.Add(new ValidationError($"{path}.anti_pattern", $"Invalid regex pattern: {rule.AntiPattern}"));
            }
        }

        if (!string.IsNullOrEmpty(rule.FilePattern))
        {
            if (!IsValidGlobPattern(rule.FilePattern))
            {
                errors.Add(new ValidationError($"{path}.file_pattern", $"Invalid glob pattern: {rule.FilePattern}"));
            }
        }
    }

    private void ValidateCustomValidators(Dictionary<string, CustomValidatorConfig> validators, List<ValidationError> errors)
    {
        foreach (var (name, validator) in validators)
        {
            var path = $"custom_validators.{name}";

            var validTypes = new[] { "regex", "roslyn", "script", "pattern" };
            if (!validTypes.Contains(validator.Type.ToLowerInvariant()))
            {
                errors.Add(new ValidationError($"{path}.type", $"Invalid validator type '{validator.Type}'. Valid types: {string.Join(", ", validTypes)}"));
            }

            var isRegexType = validator.Type.ToLowerInvariant() is "regex" or "pattern";
            var regexPattern = validator.Pattern ?? validator.Regex;

            if (isRegexType && string.IsNullOrEmpty(regexPattern))
            {
                errors.Add(new ValidationError($"{path}.pattern", "Pattern or regex is required for regex validators"));
            }

            if (isRegexType && !string.IsNullOrEmpty(regexPattern))
            {
                if (!IsValidRegex(regexPattern))
                {
                    errors.Add(new ValidationError($"{path}.pattern", $"Invalid regex pattern: {regexPattern}"));
                }
            }
        }
    }

    private void ValidateCustomRules(List<CustomRuleSource> customRules, List<ValidationError> errors)
    {
        for (int i = 0; i < customRules.Count; i++)
        {
            var source = customRules[i];
            var path = $"custom_rules[{i}]";

            if (string.IsNullOrEmpty(source.Path) && string.IsNullOrEmpty(source.NuGet))
            {
                errors.Add(new ValidationError(path, "Either 'path' or 'nuget' must be specified"));
            }

            if (!string.IsNullOrEmpty(source.Path) && !string.IsNullOrEmpty(source.NuGet))
            {
                errors.Add(new ValidationError(path, "Only one of 'path' or 'nuget' should be specified"));
            }
        }
    }

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidGlobPattern(string pattern)
    {
        // Basic validation - check for invalid characters
        // Real glob validation would be more complex
        return !string.IsNullOrWhiteSpace(pattern) && !pattern.Contains('\0');
    }
}

/// <summary>
/// Represents a validation error in the configuration.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The path to the invalid configuration element.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; }

    public ValidationError(string path, string message)
    {
        Path = path;
        Message = message;
    }

    public override string ToString() => $"{Path}: {Message}";
}
