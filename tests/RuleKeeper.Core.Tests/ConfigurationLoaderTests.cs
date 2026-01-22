using FluentAssertions;
using RuleKeeper.Core.Configuration;
using RuleKeeper.Sdk;
using Xunit;

namespace RuleKeeper.Core.Tests;

public class ConfigurationLoaderTests
{
    private readonly ConfigurationLoader _loader;

    public ConfigurationLoaderTests()
    {
        _loader = new ConfigurationLoader();
    }

    [Fact]
    public void LoadFromString_WithEmptyYaml_ReturnsDefaultConfig()
    {
        var config = _loader.LoadFromString("");

        config.Should().NotBeNull();
        config.Version.Should().Be("1.0");
    }

    [Fact]
    public void LoadFromString_WithValidYaml_ParsesCorrectly()
    {
        var yaml = @"
version: ""1.0""
metadata:
  name: ""Test Config""
scan:
  include:
    - ""**/*.cs""
  parallel: true
output:
  format: json
  min_severity: high
";

        var config = _loader.LoadFromString(yaml);

        config.Version.Should().Be("1.0");
        config.Metadata.Should().NotBeNull();
        config.Metadata!.Name.Should().Be("Test Config");
        config.Scan.Include.Should().Contain("**/*.cs");
        config.Scan.Parallel.Should().BeTrue();
        config.Output.Format.Should().Be("json");
        config.Output.MinSeverity.Should().Be(SeverityLevel.High);
    }

    [Fact]
    public void LoadFromString_WithCodingStandards_ParsesRules()
    {
        // Using the simple list format for rules
        var yaml = @"
coding_standards:
  naming:
    - id: CS-NAME-001
      name: Class Naming
      severity: medium
      parameters:
        pattern: ""^[A-Z]""
";

        var config = _loader.LoadFromString(yaml);

        config.CodingStandards.Should().ContainKey("naming");
        var namingCategory = config.CodingStandards["naming"];
        namingCategory.Enabled.Should().BeTrue();
        namingCategory.Rules.Should().HaveCount(1);

        var rule = namingCategory.Rules[0];
        rule.Id.Should().Be("CS-NAME-001");
        rule.Severity.Should().Be(SeverityLevel.Medium);
        rule.Parameters.Should().ContainKey("pattern");
    }

    [Fact]
    public void LoadFromString_WithInvalidYaml_ThrowsConfigurationException()
    {
        var invalidYaml = "invalid: yaml: syntax: [";

        var action = () => _loader.LoadFromString(invalidYaml);

        action.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void GenerateDefaultConfig_ReturnsValidYaml()
    {
        var yaml = ConfigurationLoader.GenerateDefaultConfig();

        yaml.Should().NotBeNullOrEmpty();
        yaml.Should().Contain("version:");
        yaml.Should().Contain("scan:");
        yaml.Should().Contain("output:");
        yaml.Should().Contain("coding_standards:");

        // Should be parseable
        var config = _loader.LoadFromString(yaml);
        config.Should().NotBeNull();
    }
}
