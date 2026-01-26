# Usage Examples

Practical examples for using RuleKeeper in your projects

---

## Table of Contents

- [Quick Start](#quick-start)
- [Single Language Analysis](#single-language-analysis)
- [Multi-Language Analysis](#multi-language-analysis)
- [Configuration Examples](#configuration-examples)
- [CI/CD Integration](#cicd-integration)
- [Custom Rules](#custom-rules)
- [Output Formats](#output-formats)
- [Common Workflows](#common-workflows)

---

## Quick Start

### Your First Scan

Get started with RuleKeeper in under a minute:

**Step 1: Install RuleKeeper**

```bash
dotnet tool install -g RuleKeeper.Cli
```

**Step 2: Initialize Configuration**

```bash
rulekeeper init
```

This creates a `rulekeeper.yaml` file with sensible defaults.

**Step 3: Run Your First Scan**

```bash
rulekeeper scan ./src
```

**Example Output:**

```
RuleKeeper v2.0.0 - Multi-Language Static Analysis
Scanning: ./src
Languages: C#, Python, JavaScript

Analyzing 47 files...

[CS-ASYNC-001] AsyncVoidAnalyzer.cs:45
  Avoid async void methods except for event handlers
  Severity: Warning

[CROSS-LEN-001] UserService.cs:120
  Method 'ProcessUserData' has 67 lines (max: 50)
  Severity: Warning

[CS-SEC-001] DatabaseHelper.cs:89
  Potential SQL injection vulnerability detected
  Severity: Error

Analysis complete: 3 violations found
  Errors: 1 | Warnings: 2 | Info: 0
```

---

## Single Language Analysis

### C# Project Analysis

Analyze a C# solution with full Roslyn semantic analysis:

```bash
# Analyze using solution file (recommended for C#)
rulekeeper scan ./MyProject.sln --languages csharp

# Analyze specific project
rulekeeper scan ./src/MyProject/MyProject.csproj -L csharp

# Analyze directory with C# files
rulekeeper scan ./src -L csharp
```

> **Tip:** For C# projects, pointing to a `.sln` or `.csproj` file enables full semantic analysis including type resolution, which catches more issues.

---

### Python Project Analysis

Analyze Python code for PEP 8 compliance and common issues:

```bash
# Analyze Python project
rulekeeper scan ./src -L python

# Exclude virtual environments
rulekeeper scan ./src -L python --exclude "**/venv/**" --exclude "**/.venv/**"

# Analyze specific files
rulekeeper scan ./src/app.py ./src/utils.py -L python
```

---

### JavaScript/TypeScript Analysis

```bash
# Analyze JavaScript files
rulekeeper scan ./src -L javascript

# Analyze TypeScript files
rulekeeper scan ./src -L typescript

# Analyze both JS and TS
rulekeeper scan ./src -L javascript,typescript

# Exclude node_modules and dist
rulekeeper scan ./src -L typescript --exclude "**/node_modules/**" --exclude "**/dist/**"
```

---

### Java Project Analysis

```bash
# Analyze Java source files
rulekeeper scan ./src/main/java -L java

# Exclude test files
rulekeeper scan ./src -L java --exclude "**/test/**"

# Analyze Maven project
rulekeeper scan . -L java --exclude "**/target/**"
```

---

### Go Project Analysis

```bash
# Analyze Go project
rulekeeper scan . -L go

# Exclude vendor directory
rulekeeper scan . -L go --exclude "**/vendor/**"

# Analyze specific package
rulekeeper scan ./pkg/api -L go
```

---

## Multi-Language Analysis

### Polyglot Repository

Analyze a repository with multiple languages in a single scan:

```bash
# Analyze all supported languages
rulekeeper scan . --languages csharp,python,typescript,java,go

# Short form with multiple -L flags
rulekeeper scan . -L csharp -L python -L typescript

# Let RuleKeeper auto-detect languages (default behavior)
rulekeeper scan .
```

**Example Project Structure:**

```
my-project/
├── backend/
│   ├── api/           # C# ASP.NET Core
│   │   └── Controllers/
│   └── services/      # Python microservices
│       └── auth/
├── frontend/          # TypeScript React
│   └── src/
├── mobile/            # Java Android
│   └── app/
└── tools/             # Go CLI tools
    └── cmd/
```

```bash
# Scan entire polyglot project
rulekeeper scan ./my-project \
    --languages csharp,python,typescript,java,go \
    --exclude "**/node_modules/**" \
    --exclude "**/bin/**" \
    --exclude "**/obj/**" \
    --output sarif \
    --output-file results.sarif
```

---

### Targeted Multi-Language Scan

Scan specific directories with different languages:

```bash
# Scan backend (C# + Python) with stricter rules
rulekeeper scan ./backend \
    -L csharp,python \
    --min-severity warning \
    --fail-on error

# Scan frontend (TypeScript) with lenient rules
rulekeeper scan ./frontend \
    -L typescript \
    --min-severity error
```

---

## Configuration Examples

### Basic Configuration

Create a `rulekeeper.yaml` in your project root:

```yaml
# rulekeeper.yaml - Basic configuration
version: "2.0"

scan:
  languages: [csharp, python, typescript]

  include:
    - "src/**/*"
    - "lib/**/*"

  exclude:
    - "**/node_modules/**"
    - "**/bin/**"
    - "**/obj/**"
    - "**/*.generated.cs"

output:
  format: console
  verbosity: normal
```

---

### Language-Specific Settings

```yaml
# rulekeeper.yaml - Per-language configuration
version: "2.0"

scan:
  languages: [csharp, python, typescript, java, go]

  language_settings:
    csharp:
      include: ["src/**/*.cs"]
      exclude: ["**/obj/**", "**/bin/**", "**/*.Designer.cs"]
      use_project_files: true

    python:
      include: ["**/*.py"]
      exclude: ["**/venv/**", "**/.venv/**", "**/migrations/**"]

    typescript:
      include: ["**/*.ts", "**/*.tsx"]
      exclude: ["**/node_modules/**", "**/dist/**", "**/*.d.ts"]
      tsconfig_path: "./tsconfig.json"

    java:
      include: ["src/main/java/**/*.java"]
      exclude: ["**/target/**", "**/build/**"]

    go:
      include: ["**/*.go"]
      exclude: ["**/vendor/**", "**/*_test.go"]
```

---

### Rule Configuration

```yaml
# rulekeeper.yaml - Custom rule settings
version: "2.0"

# Cross-language rules (apply to all languages)
cross_language_rules:
  method_length:
    enabled: true
    severity: warning
    parameters:
      max_lines: 50

  cyclomatic_complexity:
    enabled: true
    severity: warning
    parameters:
      max_complexity: 10

  parameter_count:
    enabled: true
    parameters:
      max_parameters: 5

  naming_conventions:
    enabled: true
    severity: info

# Language-specific rules
language_rules:
  csharp:
    async_void:
      enabled: true
      severity: error

    sql_injection:
      enabled: true
      severity: error

    service_locator:
      enabled: false  # Disabled for this project

  python:
    pep8_naming:
      enabled: true
      severity: warning

    missing_docstring:
      enabled: true
      severity: info
      parameters:
        require_module_docstring: true
        require_class_docstring: true
        require_function_docstring: false

  javascript:
    no_console_log:
      enabled: true
      severity: warning
      parameters:
        allow_in_tests: true

    prefer_const_let:
      enabled: true
      severity: warning
```

---

### Strict Enterprise Configuration

```yaml
# rulekeeper.yaml - Strict enterprise settings
version: "2.0"

scan:
  languages: [csharp, typescript]
  parallel: true
  cache: true

thresholds:
  fail_on: error
  max_warnings: 10
  max_violations: 50

cross_language_rules:
  method_length:
    enabled: true
    severity: error  # Fail build on long methods
    parameters:
      max_lines: 30  # Stricter limit

  cyclomatic_complexity:
    enabled: true
    severity: error
    parameters:
      max_complexity: 8

language_rules:
  csharp:
    # Enable all security rules as errors
    sql_injection:
      enabled: true
      severity: error
    hardcoded_secrets:
      enabled: true
      severity: error

    # Strict async patterns
    async_void:
      enabled: true
      severity: error
    async_suffix:
      enabled: true
      severity: warning

output:
  format: sarif
  file: "./reports/rulekeeper-results.sarif"

  # Also generate HTML report
  additional_outputs:
    - format: html
      file: "./reports/rulekeeper-report.html"
```

---

## CI/CD Integration

### GitHub Actions

```yaml
# .github/workflows/code-quality.yml
name: Code Quality

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  rulekeeper:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install RuleKeeper
        run: dotnet tool install -g RuleKeeper.Cli

      - name: Run Analysis
        run: |
          rulekeeper scan . \
            --languages csharp,typescript,python \
            --output sarif \
            --output-file results.sarif \
            --fail-on error

      - name: Upload SARIF to GitHub
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: results.sarif
```

> **Tip:** When you upload SARIF results to GitHub, violations appear directly in the Security tab and as annotations on pull requests.

---

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: dotnet tool install -g RuleKeeper.Cli
    displayName: 'Install RuleKeeper'

  - script: |
      rulekeeper scan $(Build.SourcesDirectory) \
        --languages csharp,typescript \
        --output sarif \
        --output-file $(Build.ArtifactStagingDirectory)/rulekeeper.sarif \
        --fail-on error
    displayName: 'Run RuleKeeper Analysis'
    continueOnError: true

  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: '$(Build.ArtifactStagingDirectory)'
      artifactName: 'CodeAnalysis'
```

---

### GitLab CI

```yaml
# .gitlab-ci.yml
stages:
  - quality

code_analysis:
  stage: quality
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet tool install -g RuleKeeper.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - rulekeeper scan .
        --languages csharp,python,typescript
        --output json
        --output-file rulekeeper-results.json
        --fail-on error
  artifacts:
    reports:
      codequality: rulekeeper-results.json
    when: always
```

---

### Jenkins Pipeline

```groovy
// Jenkinsfile
pipeline {
    agent any

    stages {
        stage('Install Tools') {
            steps {
                sh 'dotnet tool install -g RuleKeeper.Cli || true'
            }
        }

        stage('Code Analysis') {
            steps {
                sh '''
                    export PATH="$PATH:$HOME/.dotnet/tools"
                    rulekeeper scan . \
                        --languages csharp,java,python \
                        --output html \
                        --output-file reports/rulekeeper-report.html \
                        --min-severity warning
                '''
            }
            post {
                always {
                    publishHTML(target: [
                        reportDir: 'reports',
                        reportFiles: 'rulekeeper-report.html',
                        reportName: 'RuleKeeper Analysis'
                    ])
                }
            }
        }
    }
}
```

---

### Docker Integration

```dockerfile
# Dockerfile.analysis
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS analyzer

RUN dotnet tool install -g RuleKeeper.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src
COPY . .

RUN rulekeeper scan . \
    --languages csharp,python,typescript \
    --output sarif \
    --output-file /results/analysis.sarif

# Use in CI:
# docker build -f Dockerfile.analysis -t analysis .
# docker run -v $(pwd)/results:/results analysis
```

---

## Custom Rules

RuleKeeper supports two ways to create custom rules:
1. **YAML-Based Rules** - Define rules directly in configuration (no code required)
2. **SDK-Based Rules** - Write rules in C# using the RuleKeeper SDK

### YAML-Based Custom Rules (No Code Required)

Define custom rules directly in your `rulekeeper.yaml` without writing C# code.

#### Regex Pattern Rules

```yaml
coding_standards:
  security:
    rules:
      no_console_output:
        id: CUSTOM-SEC-001
        name: "No Console Output"
        severity: medium
        anti_pattern_match:
          regex: "Console\\.(WriteLine|Write)\\s*\\("
          options: [ignorecase]
          message_template: "Use ILogger instead of Console.{match}"
        exclude: ["**/Tests/**"]
```

#### AST Query Rules

Query specific AST nodes declaratively:

```yaml
coding_standards:
  design:
    rules:
      no_public_fields:
        id: CUSTOM-DESIGN-001
        name: "No Public Fields"
        severity: high
        ast_query:
          node_kinds: [FieldDeclaration]
          properties:
            IsPublic: true
            IsConst: false
        message: "Public fields should be properties"
```

#### Multi-Pattern Rules (AND/OR Logic)

Combine multiple conditions:

```yaml
coding_standards:
  security:
    rules:
      sql_injection_risk:
        id: CUSTOM-SEC-002
        name: "SQL Injection Risk"
        severity: critical
        match:
          all:
            - pattern:
                regex: "(ExecuteSqlRaw|FromSqlRaw)"
            - ast_query:
                has_children: [BinaryExpression]
          none:
            - pattern:
                regex: "@\\w+"
        message: "Use parameterized queries"
```

#### Expression-Based Rules

Use C# expressions for complex logic:

```yaml
coding_standards:
  design:
    rules:
      method_too_long:
        id: CUSTOM-DESIGN-002
        name: "Method Too Long"
        severity: medium
        expression:
          condition: "Node is IMethodNode m && GetLineCount() > (int)Parameters[\"max_lines\"]"
        parameters:
          max_lines: 30
```

### Cross-Language Custom Rule (SDK)

Create a rule that works across all languages using C#:

```csharp
// TodoCommentAnalyzer.cs - Detects TODO comments in any language
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Rules;

[Rule("CUSTOM-TODO-001",
    Name = "TODO Comment Detected",
    Description = "Finds TODO, FIXME, and HACK comments",
    Severity = SeverityLevel.Info,
    Category = "maintainability")]
[SupportedLanguages(
    Language.CSharp,
    Language.Python,
    Language.JavaScript,
    Language.TypeScript,
    Language.Java,
    Language.Go)]
public class TodoCommentAnalyzer : BaseCrossLanguageRule
{
    private string[] _keywords = { "TODO", "FIXME", "HACK", "XXX" };

    public override void Initialize(Dictionary<string, object> parameters)
    {
        base.Initialize(parameters);

        if (parameters.TryGetValue("keywords", out var keywords))
        {
            _keywords = ((IEnumerable<object>)keywords)
                .Select(k => k.ToString()!)
                .ToArray();
        }
    }

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var sourceText = context.SourceText;
        var lines = sourceText.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (var keyword in _keywords)
            {
                var index = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && IsInComment(line, index, context.Language))
                {
                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        Message = $"{keyword} comment found: {line.Trim()}",
                        Severity = DefaultSeverity,
                        Language = context.Language,
                        Location = new SourceLocation
                        {
                            FilePath = context.FilePath,
                            StartLine = i + 1,
                            StartColumn = index + 1,
                            EndLine = i + 1,
                            EndColumn = index + keyword.Length + 1
                        }
                    };
                }
            }
        }
    }

    private bool IsInComment(string line, int index, Language language)
    {
        // Check if position is within a comment based on language
        var beforeIndex = line.Substring(0, index);

        return language switch
        {
            Language.Python => beforeIndex.Contains('#'),
            _ => beforeIndex.Contains("//") || beforeIndex.Contains("/*")
        };
    }
}
```

---

### Packaging Custom Rules

```bash
# Create a custom rules project
dotnet new classlib -n MyCompany.RuleKeeper.Rules
cd MyCompany.RuleKeeper.Rules

# Add RuleKeeper SDK reference
dotnet add package RuleKeeper.Sdk
dotnet add package RuleKeeper.Sdk.CSharp  # For C#-specific rules

# Build the rules package
dotnet build -c Release
```

Use in rulekeeper.yaml:

```yaml
# rulekeeper.yaml
version: "2.0"

custom_rules:
  - path: "./rules/MyCompany.RuleKeeper.Rules.dll"
  - nuget: "MyCompany.RuleKeeper.Rules"  # Or from NuGet

language_rules:
  csharp:
    # Configure your custom rule
    public_fields:
      enabled: true
      severity: warning
```

---

## Output Formats

### Console Output

```bash
# Default console output
rulekeeper scan . --output console

# Verbose output with more details
rulekeeper scan . --output console --verbosity detailed

# Minimal output (errors only)
rulekeeper scan . --output console --min-severity error
```

---

### JSON Output

```bash
# Output to JSON file
rulekeeper scan . --output json --output-file results.json

# Pretty-printed JSON to stdout
rulekeeper scan . --output json
```

Example JSON output:

```json
{
  "summary": {
    "totalFiles": 47,
    "totalViolations": 12,
    "errors": 2,
    "warnings": 8,
    "info": 2,
    "languages": ["csharp", "python", "typescript"]
  },
  "violations": [
    {
      "ruleId": "CS-ASYNC-001",
      "ruleName": "Avoid Async Void",
      "message": "Avoid async void methods except for event handlers",
      "severity": "warning",
      "language": "csharp",
      "location": {
        "filePath": "src/Services/UserService.cs",
        "startLine": 45,
        "startColumn": 5,
        "endLine": 45,
        "endColumn": 52
      },
      "fixHint": "Change return type to Task"
    }
  ]
}
```

---

### SARIF Output

SARIF (Static Analysis Results Interchange Format) is the industry standard for static analysis tools:

```bash
# Generate SARIF report
rulekeeper scan . --output sarif --output-file results.sarif

# SARIF with embedded source snippets
rulekeeper scan . --output sarif --output-file results.sarif --include-snippets
```

> SARIF output works with GitHub Code Scanning, Azure DevOps, VS Code SARIF Viewer, and many other tools.

---

### HTML Report

```bash
# Generate HTML report
rulekeeper scan . --output html --output-file report.html

# HTML report with charts and trends
rulekeeper scan . --output html --output-file report.html --include-charts
```

HTML reports include:
- Executive summary with violation counts
- Breakdown by language and category
- Sortable and filterable violation table
- Source code snippets with highlighting
- Fix suggestions where available

---

### Multiple Outputs

```bash
# Generate multiple output formats
rulekeeper scan . \
    --output console \
    --output sarif --output-file results.sarif \
    --output html --output-file report.html
```

Or configure in YAML:

```yaml
# rulekeeper.yaml
output:
  format: console

  additional_outputs:
    - format: sarif
      file: "./reports/results.sarif"
    - format: html
      file: "./reports/report.html"
    - format: json
      file: "./reports/results.json"
```

---

## Common Workflows

### Pre-Commit Hook

Run RuleKeeper before committing changes:

```bash
#!/bin/bash
# .git/hooks/pre-commit (make executable: chmod +x)

echo "Running RuleKeeper analysis..."

# Get list of staged files
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM)

if [ -z "$STAGED_FILES" ]; then
    exit 0
fi

# Run RuleKeeper on staged files only
rulekeeper scan $STAGED_FILES \
    --min-severity warning \
    --fail-on error \
    --output console

if [ $? -ne 0 ]; then
    echo "RuleKeeper found errors. Please fix before committing."
    exit 1
fi

exit 0
```

---

### Baseline Workflow

RuleKeeper supports three baseline modes for incremental scanning: git-based, file-based, and date-based.

#### Git-Based Baseline (Recommended)

Only scan files changed since a specific git reference:

```yaml
# rulekeeper.yaml
scan:
  baseline:
    enabled: true
    mode: git
    git_ref: main           # Compare against main branch
    include_uncommitted: true
    include_untracked: true
    filter_to_diff: true    # Only report violations on changed lines
```

```bash
# Or via CLI
rulekeeper scan . --baseline git --baseline-ref main
```

#### File-Based Baseline

Store known violations and only report new ones:

```yaml
# rulekeeper.yaml
scan:
  baseline:
    enabled: true
    mode: file
    baseline_file: .rulekeeper-baseline.json
    auto_update: false      # Set true to auto-update baseline
    on_missing: warn        # warn, fail, or ignore
```

```bash
# Step 1: Generate initial baseline
rulekeeper scan . --output json --output-file .rulekeeper-baseline.json

# Step 2: Enable file baseline - only new violations reported
rulekeeper scan . --baseline file --baseline-file .rulekeeper-baseline.json
```

#### Date-Based Baseline

Only scan files modified after a specific date:

```yaml
# rulekeeper.yaml
scan:
  baseline:
    enabled: true
    mode: date
    since_date: "2024-01-01"
```

```bash
rulekeeper scan . --baseline date --since-date "2024-01-01"
```

> **Baseline Strategy:** Use baselines when adopting RuleKeeper in existing projects. This lets you enforce quality standards on new code while gradually fixing legacy issues. Git-based baseline is recommended for CI/CD integration.

---

### IDE Integration

#### VS Code

```json
// .vscode/tasks.json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "RuleKeeper: Analyze",
      "type": "shell",
      "command": "rulekeeper",
      "args": [
        "scan",
        "${workspaceFolder}",
        "--output", "sarif",
        "--output-file", "${workspaceFolder}/.rulekeeper/results.sarif"
      ],
      "problemMatcher": {
        "owner": "rulekeeper",
        "fileLocation": ["relative", "${workspaceFolder}"],
        "pattern": {
          "regexp": "^\\[(.*)\\] (.*):(\\d+)$",
          "file": 2,
          "line": 3,
          "message": 1
        }
      },
      "group": {
        "kind": "build",
        "isDefault": true
      }
    }
  ]
}
```

#### JetBrains (Rider/IntelliJ)

```
Add as External Tool:
Program: rulekeeper
Arguments: scan $ProjectFileDir$ --output console
Working directory: $ProjectFileDir$
```

---

### Trend Analysis

Track code quality over time:

```bash
# Generate timestamped reports
DATE=$(date +%Y-%m-%d)
rulekeeper scan . \
    --output json \
    --output-file "./reports/rulekeeper-${DATE}.json"

# Compare with previous report
rulekeeper compare \
    --previous "./reports/rulekeeper-2024-01-01.json" \
    --current "./reports/rulekeeper-${DATE}.json" \
    --output markdown
```

---

### Monorepo Setup

Configure RuleKeeper for monorepo projects:

```
monorepo/
├── rulekeeper.yaml          # Root config
├── packages/
│   ├── api/
│   │   ├── rulekeeper.yaml   # API-specific overrides
│   │   └── src/
│   ├── web/
│   │   ├── rulekeeper.yaml   # Web-specific overrides
│   │   └── src/
│   └── shared/
│       └── src/
└── services/
    ├── auth/
    └── payments/
```

```yaml
# Root rulekeeper.yaml - shared settings
version: "2.0"

# Base configuration inherited by all packages
scan:
  languages: [typescript, python]

cross_language_rules:
  method_length:
    parameters:
      max_lines: 50
```

```yaml
# packages/api/rulekeeper.yaml - API overrides
version: "2.0"
extends: "../../rulekeeper.yaml"

scan:
  languages: [python]

language_rules:
  python:
    missing_docstring:
      enabled: true
      severity: error  # Stricter for API
```

```bash
# Scan entire monorepo
rulekeeper scan . --recursive

# Scan specific package
rulekeeper scan ./packages/api

# Scan multiple packages in parallel
rulekeeper scan ./packages/api ./packages/web ./services --parallel
```

---

*RuleKeeper Documentation (c) 2024*
