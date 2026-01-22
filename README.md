# RuleKeeper

**RuleKeeper** is a powerful, policy-as-code CLI tool for C# that uses Roslyn for deep semantic analysis of source code against configurable YAML policies. It helps teams enforce coding standards, security practices, and architectural guidelines automatically.

## Features

- **Deep Semantic Analysis**: Uses Roslyn for AST and semantic model analysis
- **YAML-Based Configuration**: Highly configurable rules via YAML files
- **24+ Built-in Rules**: Covering naming, security, async patterns, design, exceptions, and DI
- **Multiple Output Formats**: Console (tabular), JSON, SARIF, HTML
- **Configurable Thresholds**: Set acceptable violation limits before CI/CD failure
- **Visual Reports**: Bar charts and tables for violation distribution
- **Extensible**: Create custom rules using the RuleKeeper SDK
- **CI/CD Ready**: SARIF output for GitHub/Azure DevOps integration
- **EditorConfig Generation**: Generate IDE-compatible settings

## Installation

### As a .NET Global Tool

```bash
dotnet tool install -g RuleKeeper.Cli
```

### From Source

```bash
git clone https://github.com/your-org/rulekeeper.git
cd rulekeeper
dotnet build
dotnet run --project src/RuleKeeper.Cli -- --help
```

## Quick Start

### 1. Initialize Configuration

```bash
rulekeeper init
```

This creates a `rulekeeper.yaml` file in your current directory.

### 2. Run a Scan

```bash
rulekeeper scan ./src
```

### 3. View Results

The console output displays a tabular summary with:
- Violation counts by severity
- Severity distribution chart
- Top violations by rule
- Detailed violation list with code snippets and fix hints

## Command Reference

### `scan` - Scan Code for Violations

```bash
rulekeeper scan <path> [options]
```

**Arguments:**
- `<path>` - Path to scan (file, directory, project, or solution). Default: `.`

**Options:**

| Option | Alias | Description |
|--------|-------|-------------|
| `--config` | `-c` | Path to configuration file |
| `--output` | `-o` | Output format: `console`, `json`, `sarif`, `html` (default: `console`) |
| `--format` | `-F` | Additional output formats to generate |
| `--severity` | `-s` | Minimum severity to report |
| `--fail-on` | `-f` | Severity level that causes non-zero exit code |
| `--threshold-percent` | `--tp` | Maximum allowed violation percentage (0-100) |
| `--critical-threshold` | `--ct` | Maximum allowed critical violations |
| `--high-threshold` | `--ht` | Maximum allowed high severity violations |
| `--total-threshold` | `--tt` | Maximum allowed total violations |
| `--output-file` | | Output file path |
| `--include` | `-i` | File patterns to include |
| `--exclude` | `-e` | File patterns to exclude |
| `--parallel` | `-p` | Enable parallel analysis (default: true) |
| `--verbose` | `-v` | Verbose output |
| `--no-cache` | | Disable caching |
| `--no-color` | | Disable colored output |
| `--no-viz` | `--no-visualization` | Disable visualization in reports |
| `--no-table` | | Disable tabular summary |
| `--language` | `-l` | Programming language (default: `CSharp`) |

**Examples:**

```bash
# Basic scan with console output
rulekeeper scan ./src

# Scan with JSON output
rulekeeper scan ./src -o json --output-file report.json

# Scan with multiple output formats
rulekeeper scan ./src -o console -F json -F sarif

# Fail if more than 5 critical violations
rulekeeper scan ./src --fail-on Critical --critical-threshold 5

# Fail if total violations exceed 10%
rulekeeper scan ./src --threshold-percent 10

# Scan specific files
rulekeeper scan ./src -i "**/*Service.cs" -e "**/Tests/**"

# Verbose output with custom config
rulekeeper scan ./src -c ./policies/strict.yaml -v

# Disable visualization for cleaner CI output
rulekeeper scan ./src --no-viz --no-color
```

### `init` - Create Configuration File

```bash
rulekeeper init [options]
```

Creates a `rulekeeper.yaml` configuration file with sensible defaults.

**Options:**

| Option | Description |
|--------|-------------|
| `--output` | Output file path (default: `rulekeeper.yaml`) |
| `--force` | Overwrite existing file |
| `--minimal` | Create minimal configuration |

### `validate` - Validate Configuration

```bash
rulekeeper validate <config>
```

Validates a configuration file for syntax and semantic errors.

### `list-rules` - List Available Rules

```bash
rulekeeper list-rules [options]
```

**Options:**

| Option | Description |
|--------|-------------|
| `--category` | Filter by category |
| `--format` | Output format: `table`, `json` |

### `explain` - Explain a Rule

```bash
rulekeeper explain <rule-id>
```

Shows detailed information about a specific rule, including:
- Description
- Default severity
- Parameters
- Examples

### `generate-editorconfig` - Generate EditorConfig

```bash
rulekeeper generate-editorconfig [options]
```

Generates an `.editorconfig` file based on RuleKeeper rules for IDE integration.

**Options:**

| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output file path (default: `.editorconfig`) |
| `--config`, `-c` | RuleKeeper config to base settings on |
| `--append`, `-a` | Append to existing file |
| `--force`, `-f` | Overwrite without prompting |
| `--comments` | Include descriptive comments (default: true) |

## Configuration

### Basic Structure

```yaml
# rulekeeper.yaml
metadata:
  organization: "Your Company"
  version: "1.0.0"

scan_config:
  language: CSharp
  include:
    - "**/*.cs"
  exclude:
    - "**/obj/**"
    - "**/bin/**"
    - "**/*.Designer.cs"
    - "**/*.g.cs"
  parallel: true

output:
  format: console
  min_severity: Info
  fail_on: High
  visualization: true
  show_table: true

  # Thresholds (default: 0 = any violation fails)
  critical_threshold: 0
  high_threshold: 0
  total_threshold: 0
  threshold_percentage: 0

coding_standards:
  naming:
    enabled: true
    severity: Medium
    rules:
      class_naming:
        id: CS-NAME-001
        enabled: true
        severity: High
        message: "Class name must use PascalCase"

prebuilt_policies:
  security:
    enabled: true
    severity: Critical
    skip_rules:
      - CS-SEC-003  # Skip specific rules
```

### Threshold Configuration

Thresholds allow you to set acceptable violation limits. By default, all thresholds are 0, meaning any violation of the specified severity will cause failure.

```yaml
output:
  fail_on: High

  # Allow up to 5 critical violations
  critical_threshold: 5

  # Allow up to 10 high severity violations
  high_threshold: 10

  # Allow up to 100 total violations
  total_threshold: 100

  # Or use percentage-based threshold (violations per file)
  threshold_percentage: 5.0  # 5%
```

**Command-line override:**

```bash
# Allow 3 critical violations before failing
rulekeeper scan ./src --fail-on Critical --ct 3

# Allow up to 10% violation rate
rulekeeper scan ./src --tp 10

# Allow 50 total violations
rulekeeper scan ./src --fail-on High --tt 50
```

### Visualization Options

Control the visual output of reports:

```yaml
output:
  visualization: true   # Enable bar charts
  show_table: true      # Enable tabular summaries
  colors: true          # Enable colored output
  show_code: true       # Show code snippets
  show_hints: true      # Show fix hints
```

**Command-line control:**

```bash
# Disable visualization
rulekeeper scan ./src --no-viz

# Disable tables
rulekeeper scan ./src --no-table

# Disable colors (useful for CI logs)
rulekeeper scan ./src --no-color
```

### Multiple Output Formats

Generate multiple report formats simultaneously:

```yaml
output:
  format: console
  additional_formats:
    - json
    - sarif
    - html
```

Or via command line:

```bash
rulekeeper scan ./src -o console -F json -F sarif --output-file report
# Generates: report.json, report.sarif.json, and console output
```

### Language Configuration

RuleKeeper currently supports C# (default). Language can be configured for future extensibility:

```yaml
scan_config:
  language: CSharp  # Currently only CSharp is supported
```

## Built-in Rules

### Naming Conventions (`naming`)

| Rule ID | Name | Default Severity |
|---------|------|------------------|
| CS-NAME-001 | Class Naming (PascalCase) | Medium |
| CS-NAME-002 | Method Naming (PascalCase) | Medium |
| CS-NAME-003 | Async Method Naming (Async suffix) | Low |
| CS-NAME-004 | Private Field Naming (_camelCase) | Medium |
| CS-NAME-005 | Constant Naming (PascalCase) | Medium |
| CS-NAME-006 | Property Naming (PascalCase) | Medium |
| CS-NAME-007 | Interface Naming (I prefix) | Medium |

### Security (`security`)

| Rule ID | Name | Default Severity |
|---------|------|------------------|
| CS-SEC-001 | SQL Injection Detection | Critical |
| CS-SEC-002 | Hardcoded Secrets | Critical |
| CS-SEC-003 | Sensitive Data Logging | High |
| CS-SEC-004 | XSS Prevention | Critical |
| CS-SEC-005 | Path Traversal | High |

### Async Programming (`async`)

| Rule ID | Name | Default Severity |
|---------|------|------------------|
| CS-ASYNC-001 | Async Void Avoidance | High |
| CS-ASYNC-002 | Blocking Async Detection (.Result/.Wait()) | Critical |
| CS-ASYNC-003 | ConfigureAwait Usage | Low |

### Design (`design`)

| Rule ID | Name | Default Severity |
|---------|------|------------------|
| CS-DESIGN-001 | Method Length | Medium |
| CS-DESIGN-002 | Parameter Count | Medium |
| CS-DESIGN-003 | Cyclomatic Complexity | Medium |
| CS-DESIGN-004 | Class Length | Medium |

### Exception Handling (`exceptions`)

| Rule ID | Name | Default Severity |
|---------|------|------------------|
| CS-EXC-001 | Empty Catch Blocks | High |
| CS-EXC-002 | Catching Base Exception | Medium |
| CS-EXC-003 | Throwing in Finally | High |

### Dependency Injection (`dependency_injection`)

| Rule ID | Name | Default Severity |
|---------|------|------------------|
| CS-DI-001 | Concrete Type Injection | Medium |
| CS-DI-002 | Service Locator Pattern | High |

## Output Formats

### Console (Default)

Beautiful tabular output with:
- Summary table with path, config, files, duration
- Severity breakdown table with PASS/FAIL indicators
- Bar chart visualization of violation distribution
- Top violations by rule
- Detailed violations with code snippets and fix hints

### JSON

```bash
rulekeeper scan ./src -o json --output-file report.json
```

Structured JSON output suitable for programmatic processing.

### SARIF

```bash
rulekeeper scan ./src -o sarif --output-file results.sarif.json
```

[SARIF (Static Analysis Results Interchange Format)](https://sarifweb.azurewebsites.net/) for GitHub Code Scanning, Azure DevOps, and other CI/CD tools.

### HTML

```bash
rulekeeper scan ./src -o html --output-file report.html
```

Interactive HTML report with charts and filtering.

## CI/CD Integration

### GitHub Actions

```yaml
name: Code Analysis
on: [push, pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Install RuleKeeper
        run: dotnet tool install -g RuleKeeper.Cli

      - name: Run Analysis
        run: |
          rulekeeper scan ./src \
            -o sarif \
            --output-file results.sarif.json \
            --fail-on High \
            --critical-threshold 0 \
            --no-color

      - name: Upload SARIF
        uses: github/codeql-action/upload-sarif@v2
        if: always()
        with:
          sarif_file: results.sarif.json
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: dotnet tool install -g RuleKeeper.Cli
    displayName: 'Install RuleKeeper'

  - script: |
      rulekeeper scan ./src \
        -o sarif \
        --output-file $(Build.ArtifactStagingDirectory)/results.sarif.json \
        --fail-on High \
        --no-color
    displayName: 'Run Analysis'

  - task: PublishBuildArtifacts@1
    inputs:
      pathtoPublish: '$(Build.ArtifactStagingDirectory)'
      artifactName: 'analysis-results'
```

### GitLab CI

```yaml
code-analysis:
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet tool install -g RuleKeeper.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - rulekeeper scan ./src -o json --output-file report.json --fail-on High
  artifacts:
    reports:
      codequality: report.json
```

## Custom Rules

Create custom rules by implementing `IRuleAnalyzer`:

```csharp
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

[Rule("CUSTOM-001",
    Name = "No Console.WriteLine",
    Description = "Disallow Console.WriteLine in production code",
    Severity = SeverityLevel.Medium,
    Category = "logging")]
public class NoConsoleWriteLineAnalyzer : BaseRuleAnalyzer
{
    [RuleParameter("allow_in_tests", Description = "Allow in test files", DefaultValue = true)]
    public bool AllowInTests { get; set; } = true;

    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        // Skip test files if configured
        if (AllowInTests && context.FilePath.Contains("Test"))
            yield break;

        var invocations = context.SyntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => i.ToString().Contains("Console.Write"));

        foreach (var invocation in invocations)
        {
            yield return CreateViolation(
                invocation.GetLocation(),
                "Use structured logging instead of Console.WriteLine",
                context,
                "Replace with _logger.LogInformation()"
            );
        }
    }
}
```

Then reference your assembly in the configuration:

```yaml
custom_rules:
  - path: "./custom-rules/MyCompany.Rules.dll"
```

## IDE Integration

### Visual Studio / VS Code

Generate an `.editorconfig` file for IDE integration:

```bash
rulekeeper generate-editorconfig -o .editorconfig
```

This creates naming convention rules and analyzer settings that your IDE will recognize.

The generated file includes:
- C# naming conventions matching RuleKeeper rules
- Code style preferences
- Analyzer severity settings mapped to Roslyn diagnostics
- Formatting rules

## Project Structure

```
RuleKeeper/
├── src/
│   ├── RuleKeeper.Cli/          # CLI application
│   ├── RuleKeeper.Core/         # Core analysis engine
│   ├── RuleKeeper.Rules/        # Built-in rule analyzers
│   └── RuleKeeper.Sdk/          # SDK for custom rules
├── tests/
│   ├── RuleKeeper.Core.Tests/
│   ├── RuleKeeper.Rules.Tests/
│   └── RuleKeeper.Integration.Tests/
└── samples/
    └── rulekeeper.yaml          # Sample configuration
```

## Troubleshooting

### Common Issues

**Q: Analysis is slow**
A: Try enabling parallel analysis with `--parallel` (enabled by default) and ensure caching is enabled.

**Q: Rules not being applied**
A: Check that the rule is enabled in your configuration and that the file patterns match.

**Q: SARIF upload fails in GitHub**
A: Ensure the SARIF file is under 10MB. Use `--max-total` to limit violations.

**Q: Threshold not working as expected**
A: Remember that thresholds default to 0. If you want to allow some violations, explicitly set the threshold:
```bash
rulekeeper scan ./src --fail-on High --high-threshold 5
```

### Debug Mode

```bash
rulekeeper scan ./src -v --no-cache
```

Verbose mode shows detailed information about:
- Configuration file path
- Output format
- Language setting
- Threshold values
- Analysis progress

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/your-org/rulekeeper/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/rulekeeper/discussions)
