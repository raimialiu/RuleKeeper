# RuleKeeper CLI Reference

Command-line interface for multi-language static code analysis

**Version:** v2.0 | **Multi-Language Support**

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Commands](#commands)
  - [scan](#scan-command)
  - [init](#init-command)
  - [list-rules](#list-rules-command)
  - [explain](#explain-command)
  - [validate](#validate-command)
  - [generate-editorconfig](#generate-editorconfig-command)
- [Language Options](#language-options)
- [Examples](#examples)
- [CI/CD Integration](#cicd-integration)

---

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

---

## Quick Start

```bash
# Create configuration file
rulekeeper init

# Scan the ./src directory for policy violations
rulekeeper scan ./src

# Scan multiple languages in your codebase
rulekeeper scan ./src -L csharp,python,javascript
```

---

## Commands

### scan Command

Scans code for policy violations across one or more programming languages.

```bash
rulekeeper scan <path> [options]
```

#### Arguments

| Argument | Description | Default |
|----------|-------------|---------|
| `<path>` | Path to scan (file, directory, project, or solution) | `.` (current directory) |

#### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--config` | `-c` | Path to configuration file | Auto-detected |
| `--output` | `-o` | Output format (console, json, sarif, html) | `console` |
| `--format` | `-F` | Additional output formats to generate | None |
| `--language` | `-l` | Primary programming language | `CSharp` |
| `--languages` | `-L` | Multiple languages (comma-separated) | None |
| `--severity` | `-s` | Minimum severity to report | `Info` |
| `--fail-on` | `-f` | Severity that causes non-zero exit | `High` |
| `--output-file` | | Output file path | stdout |
| `--include` | `-i` | File patterns to include | All files |
| `--exclude` | `-e` | File patterns to exclude | None |
| `--parallel` | `-p` | Enable parallel analysis | `true` |
| `--verbose` | `-v` | Verbose output | `false` |
| `--no-cache` | | Disable caching | `false` |
| `--no-color` | | Disable colored output | `false` |
| `--no-viz` | | Disable visualization | `false` |
| `--no-table` | | Disable tabular summary | `false` |
| `--summary-only` | | Show only summary | `false` |

#### Threshold Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--threshold-percent` | `--tp` | Maximum violation percentage (0-100) | `0` |
| `--critical-threshold` | `--ct` | Maximum critical violations allowed | `0` |
| `--high-threshold` | `--ht` | Maximum high severity violations | `0` |
| `--total-threshold` | `--tt` | Maximum total violations | `0` |

#### Baseline Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--baseline` | `-b` | Enable baseline mode (git, file, or date) | None |
| `--baseline-ref` | | Git reference for comparison | `main` |
| `--baseline-file` | | Path to baseline violations file | None |
| `--filter-to-diff` | | Only report violations on changed lines | `true` |
| `--include-uncommitted` | | Include uncommitted changes | `true` |
| `--include-untracked` | | Include untracked files | `true` |

---

### init Command

Creates a new RuleKeeper configuration file with sensible defaults.

```bash
rulekeeper init [options]
```

#### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--output` | Output file path | `rulekeeper.yaml` |
| `--force` | Overwrite existing file | `false` |
| `--minimal` | Create minimal configuration | `false` |

---

### list-rules Command

Lists all available rules with their supported languages.

```bash
rulekeeper list-rules [options]
```

#### Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--category` | `-c` | Filter by category |
| `--format` | `-f` | Output format (table, json, markdown) |

---

### explain Command

Shows detailed information about a specific rule.

```bash
rulekeeper explain <rule-id>
```

**Example:**

```bash
rulekeeper explain CS-ASYNC-001
```

---

### validate Command

Validates a configuration file for syntax and semantic errors.

```bash
rulekeeper validate <config-path>
```

---

### generate-editorconfig Command

Generates IDE configuration files, pre-commit hooks, and Roslyn analyzers based on RuleKeeper rules.

```bash
rulekeeper generate-editorconfig [options]
```

#### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--output` | `-o` | Output directory | `.` |
| `--config` | `-c` | RuleKeeper config to base settings on | Auto-detected |
| `--append` | `-a` | Append to existing file | `false` |
| `--force` | `-f` | Overwrite without prompting | `false` |
| `--languages` | `-L` | Languages to generate config for | All from config |
| `--hooks` | | Generate pre-commit hooks | `false` |
| `--analyzer` | | Generate Roslyn analyzer for C# | `false` |

#### Generated Files by Language

| Language | Config File | Description |
|----------|-------------|-------------|
| C# | `.editorconfig` | EditorConfig with naming/style rules |
| JavaScript | `.eslintrc.json` | ESLint configuration |
| TypeScript | `.eslintrc.json` | ESLint + TypeScript config |
| Python | `pyproject.toml` | Ruff/Black/Flake8 settings |
| Go | `.golangci.yml` | GolangCI-Lint configuration |
| Java | `checkstyle.xml` | Checkstyle rules |

#### Pre-commit Hooks (`--hooks`)

When `--hooks` is specified, generates:

- `.pre-commit-config.yaml` - Pre-commit framework config
- `.git/hooks/pre-commit` - Git hook script (bash)

```bash
# Generate hooks for all languages
rulekeeper generate-editorconfig -c rulekeeper.yaml --hooks

# The pre-commit hook will run rulekeeper on staged files
```

#### Roslyn Analyzer (`--analyzer`)

For C# projects, `--analyzer` generates a full Roslyn analyzer project:

```bash
rulekeeper generate-editorconfig -c rulekeeper.yaml --analyzer
```

**Generated Files:**
- `RuleKeeper.Analyzers/RuleKeeper.Analyzers.csproj`
- `RuleKeeper.Analyzers/GeneratedAnalyzer.cs`
- `Directory.Build.props` (auto-references analyzer)

This provides **full IDE squiggle support** in Visual Studio, Rider, and VS Code for custom rules defined in YAML.

#### Examples

```bash
# Generate .editorconfig only
rulekeeper generate-editorconfig

# Generate for specific languages
rulekeeper generate-editorconfig -L csharp,typescript

# Generate with pre-commit hooks
rulekeeper generate-editorconfig --hooks

# Generate C# analyzer for IDE integration
rulekeeper generate-editorconfig --analyzer

# Full generation: config + hooks + analyzer
rulekeeper generate-editorconfig -c rulekeeper.yaml --hooks --analyzer

# Custom output directory
rulekeeper generate-editorconfig -o ./config --hooks
```

---

## Language Options

RuleKeeper supports analyzing multiple programming languages.

### Supported Languages

| Language | Value | File Extensions |
|----------|-------|-----------------|
| C# | `CSharp` | .cs |
| Python | `Python` | .py, .pyw |
| JavaScript | `JavaScript` | .js, .jsx, .mjs |
| TypeScript | `TypeScript` | .ts, .tsx, .mts |
| Java | `Java` | .java |
| Go | `Go` | .go |

### Single Language

```bash
rulekeeper scan ./src -l Python
```

### Multiple Languages

```bash
rulekeeper scan ./src -L csharp,python,javascript,typescript
```

---

## Examples

### Basic Usage

```bash
# Basic scan with console output
rulekeeper scan ./src

# Scan with JSON output
rulekeeper scan ./src -o json --output-file report.json

# Scan with multiple output formats
rulekeeper scan ./src -o console -F json -F sarif -F html

# Scan specific file patterns
rulekeeper scan ./src -i "**/*Service.cs" -e "**/Tests/**"

# Verbose output with custom config
rulekeeper scan ./src -c ./policies/strict.yaml -v
```

### Multi-Language Examples

```bash
# Scan Python codebase
rulekeeper scan ./src -l Python

# Scan polyglot project
rulekeeper scan ./project -L csharp,python,typescript,java,go

# Scan frontend and backend separately
# Frontend (TypeScript/JavaScript)
rulekeeper scan ./frontend -L typescript,javascript

# Backend (C#)
rulekeeper scan ./backend -l CSharp
```

---

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
            -L csharp,python,typescript \
            -o sarif \
            --output-file results.sarif.json \
            --fail-on High \
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
```

---

*RuleKeeper Documentation (c) 2024*
