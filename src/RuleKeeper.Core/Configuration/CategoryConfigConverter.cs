using RuleKeeper.Core.Configuration.Models;
using RuleKeeper.Sdk;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RuleKeeper.Core.Configuration;

/// <summary>
/// Custom YAML type converter that handles two formats for CategoryConfig:
/// 1. Simple list format: category_name: [- id: ..., - id: ...]
/// 2. Full format: category_name: { enabled: true, severity: High, rules: [...] }
/// </summary>
public class CategoryConfigConverter : IYamlTypeConverter
{
    private readonly IDeserializer _ruleDeserializer;
    private readonly ISerializer _ruleSerializer;

    public CategoryConfigConverter()
    {
        _ruleDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _ruleSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    public bool Accepts(Type type) => type == typeof(CategoryConfig);

    public object? ReadYaml(IParser parser, Type type)
    {
        // Check if the current node is a sequence (list) or mapping (object)
        if (parser.TryConsume<SequenceStart>(out _))
        {
            // Simple list format - deserialize as List<RuleDefinition>
            var rules = new List<RuleDefinition>();

            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                var rule = ParseRuleDefinition(parser);
                if (rule != null)
                {
                    rules.Add(rule);
                }
            }

            return CategoryConfig.FromRules(rules);
        }
        else if (parser.TryConsume<MappingStart>(out _))
        {
            // Full format with enabled, severity, rules properties
            var config = new CategoryConfig();

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var propertyName = parser.Consume<Scalar>();

                switch (propertyName.Value.ToLowerInvariant())
                {
                    case "enabled":
                        var enabledScalar = parser.Consume<Scalar>();
                        config.Enabled = bool.TryParse(enabledScalar.Value, out var enabled) && enabled;
                        break;

                    case "severity":
                        var severityScalar = parser.Consume<Scalar>();
                        if (Enum.TryParse<SeverityLevel>(severityScalar.Value, true, out var severity))
                        {
                            config.Severity = severity;
                        }
                        break;

                    case "rules":
                        if (parser.TryConsume<SequenceStart>(out _))
                        {
                            while (!parser.TryConsume<SequenceEnd>(out _))
                            {
                                var rule = ParseRuleDefinition(parser);
                                if (rule != null)
                                {
                                    config.Rules.Add(rule);
                                }
                            }
                        }
                        break;

                    case "exclude":
                        if (parser.TryConsume<SequenceStart>(out _))
                        {
                            while (!parser.TryConsume<SequenceEnd>(out _))
                            {
                                var excludeScalar = parser.Consume<Scalar>();
                                config.Exclude.Add(excludeScalar.Value);
                            }
                        }
                        break;

                    default:
                        // Skip unknown properties
                        parser.SkipThisAndNestedEvents();
                        break;
                }
            }

            return config;
        }
        else if (parser.TryConsume<Scalar>(out var scalar))
        {
            // Handle null or empty values
            if (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "null")
            {
                return new CategoryConfig();
            }
        }

        return new CategoryConfig();
    }

    private RuleDefinition? ParseRuleDefinition(IParser parser)
    {
        if (!parser.TryConsume<MappingStart>(out _))
        {
            parser.SkipThisAndNestedEvents();
            return null;
        }

        var rule = new RuleDefinition();

        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var propertyName = parser.Consume<Scalar>();

            switch (propertyName.Value.ToLowerInvariant())
            {
                case "id":
                    rule.Id = parser.Consume<Scalar>().Value;
                    break;
                case "name":
                    rule.Name = parser.Consume<Scalar>().Value;
                    break;
                case "description":
                    rule.Description = parser.Consume<Scalar>().Value;
                    break;
                case "severity":
                    var sevValue = parser.Consume<Scalar>().Value;
                    if (Enum.TryParse<SeverityLevel>(sevValue, true, out var sev))
                    {
                        rule.Severity = sev;
                    }
                    break;
                case "enabled":
                    var enabledValue = parser.Consume<Scalar>().Value;
                    rule.Enabled = bool.TryParse(enabledValue, out var en) && en;
                    break;
                case "skip":
                    var skipValue = parser.Consume<Scalar>().Value;
                    rule.Skip = bool.TryParse(skipValue, out var sk) && sk;
                    break;
                case "pattern":
                    rule.Pattern = ParseScalarOrNull(parser);
                    break;
                case "anti_pattern":
                    rule.AntiPattern = ParseScalarOrNull(parser);
                    break;
                case "file_pattern":
                    rule.FilePattern = ParseScalarOrNull(parser);
                    break;
                case "custom_validator":
                    rule.CustomValidator = ParseScalarOrNull(parser);
                    break;
                case "message":
                    rule.Message = ParseScalarOrNull(parser);
                    break;
                case "fix_hint":
                    rule.FixHint = ParseScalarOrNull(parser);
                    break;
                case "applies_to":
                    rule.AppliesTo = ParseStringList(parser);
                    break;
                case "exclude":
                    rule.Exclude = ParseStringList(parser);
                    break;
                case "parameters":
                    rule.Parameters = ParseDictionary(parser);
                    break;
                default:
                    // Skip unknown properties
                    parser.SkipThisAndNestedEvents();
                    break;
            }
        }

        return rule;
    }

    private string? ParseScalarOrNull(IParser parser)
    {
        var scalar = parser.Consume<Scalar>();
        return string.IsNullOrEmpty(scalar.Value) || scalar.Value == "null" ? null : scalar.Value;
    }

    private List<string> ParseStringList(IParser parser)
    {
        var list = new List<string>();

        if (parser.TryConsume<SequenceStart>(out _))
        {
            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                var scalar = parser.Consume<Scalar>();
                if (!string.IsNullOrEmpty(scalar.Value))
                {
                    list.Add(scalar.Value);
                }
            }
        }
        else
        {
            parser.SkipThisAndNestedEvents();
        }

        return list;
    }

    private Dictionary<string, object> ParseDictionary(IParser parser)
    {
        var dict = new Dictionary<string, object>();

        if (parser.TryConsume<MappingStart>(out _))
        {
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>().Value;
                var value = ParseValue(parser);
                if (value != null)
                {
                    dict[key] = value;
                }
            }
        }
        else
        {
            parser.SkipThisAndNestedEvents();
        }

        return dict;
    }

    private object? ParseValue(IParser parser)
    {
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            // Try to parse as various types
            if (bool.TryParse(scalar.Value, out var boolVal))
                return boolVal;
            if (int.TryParse(scalar.Value, out var intVal))
                return intVal;
            if (double.TryParse(scalar.Value, out var doubleVal))
                return doubleVal;
            return scalar.Value;
        }
        else if (parser.TryConsume<SequenceStart>(out _))
        {
            var list = new List<object>();
            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                var item = ParseValue(parser);
                if (item != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }
        else if (parser.TryConsume<MappingStart>(out _))
        {
            var dict = new Dictionary<string, object>();
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>().Value;
                var value = ParseValue(parser);
                if (value != null)
                {
                    dict[key] = value;
                }
            }
            return dict;
        }

        parser.SkipThisAndNestedEvents();
        return null;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        if (value is not CategoryConfig config)
        {
            emitter.Emit(new Scalar(null, "null"));
            return;
        }

        // Always write in the simple list format for cleaner output
        emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

        foreach (var rule in config.Rules)
        {
            // Write rule as a mapping
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            if (rule.Id != null)
            {
                emitter.Emit(new Scalar("id"));
                emitter.Emit(new Scalar(rule.Id));
            }
            if (rule.Name != null)
            {
                emitter.Emit(new Scalar("name"));
                emitter.Emit(new Scalar(rule.Name));
            }
            if (rule.Description != null)
            {
                emitter.Emit(new Scalar("description"));
                emitter.Emit(new Scalar(rule.Description));
            }
            emitter.Emit(new Scalar("severity"));
            emitter.Emit(new Scalar(rule.Severity.ToString().ToLowerInvariant()));
            emitter.Emit(new Scalar("enabled"));
            emitter.Emit(new Scalar(rule.Enabled.ToString().ToLowerInvariant()));

            emitter.Emit(new MappingEnd());
        }

        emitter.Emit(new SequenceEnd());
    }
}
