using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RuleKeeper.Core.Configuration;

/// <summary>
/// Loads and parses RuleKeeper configuration from YAML files.
/// </summary>
public class ConfigurationLoader
{
    private readonly IDeserializer _deserializer;

    public ConfigurationLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new CategoryConfigConverter())
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads configuration from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The loaded configuration.</returns>
    public RuleKeeperConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        var yaml = File.ReadAllText(filePath);
        return LoadFromString(yaml);
    }

    /// <summary>
    /// Loads configuration from a YAML string.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The loaded configuration.</returns>
    public RuleKeeperConfig LoadFromString(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new RuleKeeperConfig();
        }

        try
        {
            var config = _deserializer.Deserialize<RuleKeeperConfig>(yaml);
            return config ?? new RuleKeeperConfig();
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new ConfigurationException($"Invalid YAML syntax: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tries to find and load a configuration file.
    /// Searches current directory and parent directories.
    /// </summary>
    /// <param name="startPath">Starting path for the search.</param>
    /// <param name="configFileName">Name of the config file to search for.</param>
    /// <returns>The loaded configuration, or default if not found.</returns>
    public (RuleKeeperConfig Config, string? FilePath) LoadFromDirectory(
        string startPath,
        string configFileName = "rulekeeper.yaml")
    {
        var searchPaths = new[]
        {
            configFileName,
            ".rulekeeper.yaml",
            ".rulekeeper.yml",
            "rulekeeper.yml"
        };

        var directory = new DirectoryInfo(startPath);
        while (directory != null)
        {
            foreach (var searchPath in searchPaths)
            {
                var configPath = Path.Combine(directory.FullName, searchPath);
                if (File.Exists(configPath))
                {
                    return (LoadFromFile(configPath), configPath);
                }
            }

            directory = directory.Parent;
        }

        return (new RuleKeeperConfig(), null);
    }

    /// <summary>
    /// Generates a default configuration file.
    /// </summary>
    public static string GenerateDefaultConfig()
    {
        return """
            # RuleKeeper Configuration
            # Supports two formats for coding_standards:
            # 1. Simple list: category_name: [- id: ..., - id: ...]
            # 2. Full format: category_name: { enabled: true, severity: High, rules: [...] }

            version: "1.0"

            metadata:
              name: "My Project Rules"
              description: "Code quality rules for my project"
              author: ""

            scan:
              include:
                - "**/*.cs"
              exclude:
                - "**/obj/**"
                - "**/bin/**"
                - "**/*.Designer.cs"
                - "**/*.g.cs"
                - "**/*.generated.cs"
              parallel: true
              cache: true

            output:
              format: console
              min_severity: info
              fail_on: high
              show_code: true
              show_hints: true
              colors: true
              visualization: true
              show_table: true
              critical_threshold: 0
              high_threshold: 0
              total_threshold: 0
              threshold_percentage: 0

            # Pre-built policies
            prebuilt_policies:
              security:
                enabled: true
                severity: high
              async_best_practices:
                enabled: true
              naming_conventions:
                enabled: true

            # Custom coding standards (using simple list format)
            coding_standards:
              naming:
                - id: CS-NAME-001
                  name: "Class Naming Convention"
                  description: "Classes must use PascalCase"
                  severity: medium
                  enabled: true
                  pattern: "^[A-Z][a-zA-Z0-9]*$"
                  message: "Class names must start with uppercase and use PascalCase"
                  fix_hint: "Rename the class to use PascalCase (e.g., MyClassName)"
                  applies_to:
                    - classes

                - id: CS-NAME-007
                  name: "Interface Naming Convention"
                  description: "Interfaces must start with 'I' prefix"
                  severity: medium
                  enabled: true
                  pattern: "^I[A-Z][a-zA-Z0-9]*$"
                  message: "Interface names must start with 'I' prefix"
                  fix_hint: "Add 'I' prefix to interface name (e.g., IMyInterface)"
                  applies_to:
                    - interfaces

              security:
                - id: CS-SEC-002
                  name: "No Hardcoded Secrets"
                  description: "Detect hardcoded passwords, API keys, and secrets"
                  severity: critical
                  enabled: true
                  anti_pattern: "(password|secret|apikey|api_key|connectionstring)\\s*=\\s*[\"'][^\"']+[\"']"
                  message: "Potential hardcoded secret detected"
                  fix_hint: "Use environment variables or a secrets manager"

              async:
                - id: CS-ASYNC-002
                  name: "No Blocking on Async"
                  description: "Avoid .Result and .Wait() on Tasks"
                  severity: high
                  enabled: true
                  message: "Avoid blocking on async with .Result or .Wait()"
                  fix_hint: "Use 'await' instead of .Result or .Wait()"

            # Custom validators (for advanced use cases)
            custom_validators: {}

            # Custom rule assemblies
            custom_rules: []
            """;
    }
}