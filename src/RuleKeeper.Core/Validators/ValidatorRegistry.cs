using System.Reflection;
using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Registry for custom validators. Manages validator discovery, instantiation, and caching.
/// </summary>
public class ValidatorRegistry
{
    private readonly Dictionary<string, ValidatorInfo> _validators = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ICustomValidator> _instances = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Register a validator type.
    /// </summary>
    public void Register<T>() where T : ICustomValidator, new()
    {
        var instance = new T();
        Register(instance.ValidatorId, typeof(T), instance.Name, instance.Description, instance.SupportedLanguages.ToArray());
    }

    /// <summary>
    /// Register a validator type with explicit metadata.
    /// </summary>
    public void Register(string validatorId, Type type, string? name = null, string? description = null, Language[]? supportedLanguages = null)
    {
        if (!typeof(ICustomValidator).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type.Name} does not implement ICustomValidator", nameof(type));

        lock (_lock)
        {
            _validators[validatorId] = new ValidatorInfo
            {
                ValidatorId = validatorId,
                Name = name ?? validatorId,
                Description = description,
                Type = type,
                SupportedLanguages = supportedLanguages ?? Array.Empty<Language>()
            };
        }
    }

    /// <summary>
    /// Register a validator instance directly.
    /// </summary>
    public void Register(ICustomValidator validator)
    {
        lock (_lock)
        {
            _validators[validator.ValidatorId] = new ValidatorInfo
            {
                ValidatorId = validator.ValidatorId,
                Name = validator.Name,
                Description = validator.Description,
                Type = validator.GetType(),
                SupportedLanguages = validator.SupportedLanguages.ToArray()
            };
            _instances[validator.ValidatorId] = validator;
        }
    }

    /// <summary>
    /// Create or get a validator instance by ID.
    /// </summary>
    public ICustomValidator? GetValidator(string validatorId, Dictionary<string, object>? parameters = null)
    {
        lock (_lock)
        {
            // Check if we have a cached instance
            if (_instances.TryGetValue(validatorId, out var cached))
            {
                // Re-initialize with new parameters if provided
                if (parameters != null && parameters.Count > 0)
                {
                    cached.Initialize(parameters);
                }
                return cached;
            }

            // Check if we have a registered type
            if (!_validators.TryGetValue(validatorId, out var info))
                return null;

            // Create new instance
            var instance = (ICustomValidator?)Activator.CreateInstance(info.Type);
            if (instance == null)
                return null;

            instance.Initialize(parameters ?? new Dictionary<string, object>());
            _instances[validatorId] = instance;
            return instance;
        }
    }

    /// <summary>
    /// Check if a validator is registered.
    /// </summary>
    public bool IsRegistered(string validatorId)
    {
        lock (_lock)
        {
            return _validators.ContainsKey(validatorId);
        }
    }

    /// <summary>
    /// Get information about a validator.
    /// </summary>
    public ValidatorInfo? GetValidatorInfo(string validatorId)
    {
        lock (_lock)
        {
            return _validators.TryGetValue(validatorId, out var info) ? info : null;
        }
    }

    /// <summary>
    /// Get all registered validator IDs.
    /// </summary>
    public IEnumerable<string> GetRegisteredValidatorIds()
    {
        lock (_lock)
        {
            return _validators.Keys.ToList();
        }
    }

    /// <summary>
    /// Load validators from an assembly.
    /// </summary>
    public int LoadFromAssembly(Assembly assembly)
    {
        var count = 0;
        var validatorTypes = assembly.GetTypes()
            .Where(t => typeof(ICustomValidator).IsAssignableFrom(t)
                       && !t.IsAbstract
                       && !t.IsInterface
                       && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in validatorTypes)
        {
            try
            {
                var instance = (ICustomValidator?)Activator.CreateInstance(type);
                if (instance != null)
                {
                    Register(instance);
                    count++;
                }
            }
            catch
            {
                // Skip types that fail to instantiate
            }
        }

        return count;
    }

    /// <summary>
    /// Load validators from an assembly file.
    /// </summary>
    public int LoadFromAssemblyFile(string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        return LoadFromAssembly(assembly);
    }

    /// <summary>
    /// Load validators from a directory of assemblies.
    /// </summary>
    public int LoadFromDirectory(string directoryPath, string pattern = "*.dll")
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        var count = 0;
        foreach (var file in Directory.GetFiles(directoryPath, pattern))
        {
            try
            {
                count += LoadFromAssemblyFile(file);
            }
            catch
            {
                // Skip assemblies that fail to load
            }
        }

        return count;
    }

    /// <summary>
    /// Create validators from YAML custom_validators configuration.
    /// </summary>
    public void LoadFromConfiguration(Dictionary<string, EnhancedCustomValidatorConfig> customValidators, ValidatorFactory factory)
    {
        foreach (var (name, config) in customValidators)
        {
            var validator = factory.CreateFromConfig(name, config);
            if (validator != null)
            {
                Register(validator);
            }
        }
    }

    /// <summary>
    /// Clear all cached instances (but keep registrations).
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _instances.Clear();
        }
    }

    /// <summary>
    /// Clear all registrations and instances.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _validators.Clear();
            _instances.Clear();
        }
    }
}

/// <summary>
/// Information about a registered validator.
/// </summary>
public class ValidatorInfo
{
    public required string ValidatorId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Type Type { get; init; }
    public Language[] SupportedLanguages { get; init; } = Array.Empty<Language>();
}

/// <summary>
/// Factory for creating validators from YAML configuration.
/// </summary>
public class ValidatorFactory
{
    private readonly ValidatorRegistry _registry;

    public ValidatorFactory(ValidatorRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Create a validator from YAML configuration.
    /// </summary>
    public ICustomValidator? CreateFromConfig(string name, EnhancedCustomValidatorConfig config)
    {
        return config.Type.ToLowerInvariant() switch
        {
            "pattern" or "regex" => CreatePatternValidator(name, config),
            "ast_query" or "ast" => CreateAstQueryValidator(name, config),
            "expression" => CreateExpressionValidator(name, config),
            "script" => CreateScriptValidator(name, config),
            "assembly" => CreateAssemblyValidator(name, config),
            _ => null
        };
    }

    /// <summary>
    /// Create a validator from a rule definition.
    /// </summary>
    public ICustomValidator? CreateFromRuleDefinition(RuleDefinition rule)
    {
        var validationType = rule.GetValidationType();
        var validatorId = rule.Id ?? $"rule-{Guid.NewGuid():N}";

        return validationType switch
        {
            RuleValidationType.EnhancedPattern => CreatePatternValidatorFromRule(validatorId, rule),
            RuleValidationType.AstQuery => CreateAstQueryValidatorFromRule(validatorId, rule),
            RuleValidationType.MultiMatch => CreateMultiMatchValidatorFromRule(validatorId, rule),
            RuleValidationType.Expression => CreateExpressionValidatorFromRule(validatorId, rule),
            RuleValidationType.Script => CreateScriptValidatorFromRule(validatorId, rule),
            RuleValidationType.Validator => ResolveValidatorReference(rule),
            RuleValidationType.CustomValidator => _registry.GetValidator(rule.CustomValidator!),
            _ => null
        };
    }

    private ICustomValidator? CreatePatternValidator(string name, EnhancedCustomValidatorConfig config)
    {
        var pattern = config.Pattern ?? (config.Regex != null ? new PatternConfig { Regex = config.Regex } : null);
        if (pattern?.Regex == null)
            return null;

        return new RegexValidator(name, name, config.Description, pattern, config.Message);
    }

    private ICustomValidator? CreatePatternValidatorFromRule(string validatorId, RuleDefinition rule)
    {
        var pattern = rule.PatternMatch ?? rule.AntiPatternMatch;
        if (pattern == null)
            return null;

        var isAntiPattern = rule.AntiPatternMatch != null;
        if (isAntiPattern && pattern.Negate == false)
            pattern.Negate = true;

        return new RegexValidator(validatorId, rule.Name ?? validatorId, rule.Description, pattern, rule.Message);
    }

    private ICustomValidator? CreateAstQueryValidator(string name, EnhancedCustomValidatorConfig config)
    {
        if (config.AstQuery == null)
            return null;

        return new AstQueryValidator(name, name, config.Description, config.AstQuery, config.Message);
    }

    private ICustomValidator? CreateAstQueryValidatorFromRule(string validatorId, RuleDefinition rule)
    {
        if (rule.AstQuery == null)
            return null;

        return new AstQueryValidator(validatorId, rule.Name ?? validatorId, rule.Description, rule.AstQuery, rule.Message);
    }

    private ICustomValidator? CreateMultiMatchValidatorFromRule(string validatorId, RuleDefinition rule)
    {
        if (rule.Match == null)
            return null;

        return new MultiMatchValidator(validatorId, rule.Name ?? validatorId, rule.Description, rule.Match, rule.Message, this);
    }

    private ICustomValidator? CreateExpressionValidator(string name, EnhancedCustomValidatorConfig config)
    {
        if (config.Expression == null)
            return null;

        return new ExpressionValidator(name, name, config.Description, config.Expression, config.Message);
    }

    private ICustomValidator? CreateExpressionValidatorFromRule(string validatorId, RuleDefinition rule)
    {
        if (rule.Expression == null)
            return null;

        return new ExpressionValidator(validatorId, rule.Name ?? validatorId, rule.Description, rule.Expression, rule.Message);
    }

    private ICustomValidator? CreateScriptValidator(string name, EnhancedCustomValidatorConfig config)
    {
        var script = config.Script ?? (config.ScriptCode != null ? new ScriptConfig { Code = config.ScriptCode } : null);
        if (script == null)
            return null;

        return new ScriptValidator(name, name, config.Description, script, config.Message);
    }

    private ICustomValidator? CreateScriptValidatorFromRule(string validatorId, RuleDefinition rule)
    {
        if (rule.Script == null)
            return null;

        return new ScriptValidator(validatorId, rule.Name ?? validatorId, rule.Description, rule.Script, rule.Message);
    }

    private ICustomValidator? CreateAssemblyValidator(string name, EnhancedCustomValidatorConfig config)
    {
        if (string.IsNullOrEmpty(config.Assembly) || string.IsNullOrEmpty(config.TypeName))
            return null;

        return new AssemblyValidator(name, config.Assembly, config.TypeName);
    }

    private ICustomValidator? ResolveValidatorReference(RuleDefinition rule)
    {
        if (rule.Validator == null)
            return null;

        // Reference to existing validator
        if (!string.IsNullOrEmpty(rule.Validator.Reference))
        {
            return _registry.GetValidator(rule.Validator.Reference, rule.Parameters);
        }

        // Inline validator definition
        if (rule.Validator.Inline != null)
        {
            var inline = rule.Validator.Inline;
            var validatorId = rule.Id ?? $"inline-{Guid.NewGuid():N}";

            return inline.Type.ToLowerInvariant() switch
            {
                "pattern" or "regex" when inline.Pattern != null =>
                    new RegexValidator(validatorId, rule.Name ?? validatorId, rule.Description, inline.Pattern, rule.Message),
                "ast_query" or "ast" when inline.AstQuery != null =>
                    new AstQueryValidator(validatorId, rule.Name ?? validatorId, rule.Description, inline.AstQuery, rule.Message),
                "expression" when inline.Expression != null =>
                    new ExpressionValidator(validatorId, rule.Name ?? validatorId, rule.Description, inline.Expression, rule.Message),
                "script" when inline.Script != null =>
                    new ScriptValidator(validatorId, rule.Name ?? validatorId, rule.Description, inline.Script, rule.Message),
                "assembly" when !string.IsNullOrEmpty(inline.Assembly) && !string.IsNullOrEmpty(inline.TypeName) =>
                    new AssemblyValidator(validatorId, inline.Assembly, inline.TypeName),
                _ => null
            };
        }

        return null;
    }
}
