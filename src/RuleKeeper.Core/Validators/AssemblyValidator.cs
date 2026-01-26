using System.Reflection;
using RuleKeeper.Sdk;

namespace RuleKeeper.Core.Validators;

/// <summary>
/// Validator that loads and delegates to validators from external assemblies (DLLs).
/// </summary>
public class AssemblyValidator : CustomValidatorBase
{
    private readonly string _validatorId;
    private readonly string _assemblyPath;
    private readonly string _typeName;
    private ICustomValidator? _loadedValidator;
    private bool _initialized;

    public override string ValidatorId => _validatorId;
    public override string Name => _loadedValidator?.Name ?? _validatorId;
    public override string? Description => _loadedValidator?.Description;

    public AssemblyValidator(string validatorId, string assemblyPath, string typeName)
    {
        _validatorId = validatorId;
        _assemblyPath = assemblyPath;
        _typeName = typeName;
    }

    public override void Initialize(Dictionary<string, object> parameters)
    {
        base.Initialize(parameters);

        if (_initialized)
        {
            _loadedValidator?.Initialize(parameters);
            return;
        }

        try
        {
            var resolvedPath = ResolvePath(_assemblyPath);
            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Assembly not found: {resolvedPath}");
            }

            var assembly = Assembly.LoadFrom(resolvedPath);
            var type = assembly.GetType(_typeName);

            if (type == null)
            {
                throw new TypeLoadException($"Type '{_typeName}' not found in assembly '{resolvedPath}'");
            }

            if (!typeof(ICustomValidator).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type '{_typeName}' does not implement ICustomValidator");
            }

            _loadedValidator = (ICustomValidator?)Activator.CreateInstance(type);
            _loadedValidator?.Initialize(parameters);
            _initialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load validator from assembly: {ex.Message}", ex);
        }
    }

    public override async Task<IEnumerable<Violation>> ValidateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        if (_loadedValidator == null)
        {
            return new[]
            {
                CreateViolation(context, $"Assembly validator not loaded: {_assemblyPath}:{_typeName}", 1, 1)
            };
        }

        return await _loadedValidator.ValidateAsync(context, cancellationToken);
    }

    public override bool SupportsLanguage(Language language)
    {
        return _loadedValidator?.SupportsLanguage(language) ?? true;
    }

    private static string ResolvePath(string path)
    {
        // Handle relative paths
        if (!Path.IsPathRooted(path))
        {
            // Try relative to current directory
            var currentDir = Environment.CurrentDirectory;
            var resolvedPath = Path.Combine(currentDir, path);
            if (File.Exists(resolvedPath))
                return resolvedPath;

            // Try relative to plugins directory
            var pluginsDir = Path.Combine(currentDir, "plugins");
            resolvedPath = Path.Combine(pluginsDir, path);
            if (File.Exists(resolvedPath))
                return resolvedPath;
        }

        return path;
    }
}

/// <summary>
/// Factory for loading validators from assemblies.
/// </summary>
public class AssemblyValidatorLoader
{
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    /// <summary>
    /// Load all validators from an assembly file.
    /// </summary>
    public IEnumerable<ICustomValidator> LoadValidatorsFromAssembly(string assemblyPath)
    {
        var resolvedPath = Path.GetFullPath(assemblyPath);

        if (!_loadedAssemblies.TryGetValue(resolvedPath, out var assembly))
        {
            assembly = Assembly.LoadFrom(resolvedPath);
            _loadedAssemblies[resolvedPath] = assembly;
        }

        var validatorTypes = assembly.GetTypes()
            .Where(t => typeof(ICustomValidator).IsAssignableFrom(t)
                       && !t.IsAbstract
                       && !t.IsInterface
                       && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in validatorTypes)
        {
            ICustomValidator? validator = null;
            try
            {
                validator = (ICustomValidator?)Activator.CreateInstance(type);
            }
            catch
            {
            }

            if (validator != null)
                yield return validator;
        }
    }

    /// <summary>
    /// Load a specific validator type from an assembly.
    /// </summary>
    public ICustomValidator? LoadValidatorFromAssembly(string assemblyPath, string typeName)
    {
        var resolvedPath = Path.GetFullPath(assemblyPath);

        if (!_loadedAssemblies.TryGetValue(resolvedPath, out var assembly))
        {
            assembly = Assembly.LoadFrom(resolvedPath);
            _loadedAssemblies[resolvedPath] = assembly;
        }

        var type = assembly.GetType(typeName);
        if (type == null || !typeof(ICustomValidator).IsAssignableFrom(type))
            return null;

        return (ICustomValidator?)Activator.CreateInstance(type);
    }

    /// <summary>
    /// Load all validators from assemblies in a directory.
    /// </summary>
    public IEnumerable<ICustomValidator> LoadValidatorsFromDirectory(string directoryPath, string pattern = "*.dll")
    {
        if (!Directory.Exists(directoryPath))
            yield break;

        foreach (var file in Directory.GetFiles(directoryPath, pattern))
        {
            IEnumerable<ICustomValidator>? validators = null;
            try
            {
                validators = LoadValidatorsFromAssembly(file);
            }
            catch
            {
            }

            if (validators != null)
            {
                foreach (var validator in validators)
                {
                    yield return validator;
                }
            }
        }
    }
}
