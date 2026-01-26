# YAML Configuration Reference

Complete reference for RuleKeeper YAML configuration files

**Version:** v2.0 | **Custom Rule Types:** 10

---

## Table of Contents

- [Configuration Structure](#configuration-structure)
- [Custom Rule Types](#custom-rule-types)
- [Rule Type Examples](#rule-type-examples)
- [Configuration Sections](#configuration-sections)
- [Complete Example](#complete-example)

---

## Configuration Structure

```yaml
version: "1.0"

metadata:
  name: "Project Name"
  description: "Description"
  author: "Team Name"

scan:
  languages: [csharp, python, javascript]
  include: ["**/*.cs", "**/*.py"]
  exclude: ["**/bin/**", "**/obj/**"]
  parallel: true
  cache: true
  baseline:
    enabled: false
    mode: git
    git_ref: main

output:
  format: console
  min_severity: info
  fail_on: high
  show_code: true
  show_hints: true

prebuilt_policies:
  security:
    enabled: true
    severity: critical
  naming_conventions:
    enabled: true

coding_standards:
  category_name:
    enabled: true
    rules:
      - id: RULE-001
        name: "Rule Name"
        severity: medium

custom_validators:
  validator_name:
    type: regex
    regex: "pattern"

custom_rules: []
```

---

## Custom Rule Types

RuleKeeper supports **10 different rule validation types**:

| # | Type | YAML Key | Description | Complexity |
|---|------|----------|-------------|------------|
| 1 | **BuiltIn** | `id:` only | Pre-coded C# Roslyn analyzers | None |
| 2 | **SimplePattern** | `pattern:` / `anti_pattern:` | Basic regex string matching | Low |
| 3 | **EnhancedPattern** | `pattern_match:` / `anti_pattern_match:` | Regex with captures, options, scope | Low |
| 4 | **AstQuery** | `ast_query:` | Declarative AST node matching | Medium |
| 5 | **MultiMatch** | `match:` | Combine patterns with AND/OR/NONE | Medium |
| 6 | **Expression** | `expression:` | C# expression (single condition) | Medium |
| 7 | **Script** | `script:` | Full C# script with custom logic | High |
| 8 | **Validator** | `validator:` | Reference or inline validator | Medium |
| 9 | **CustomValidator** | `custom_validator:` | Reference to custom_validators | Low |
| 10 | **None** | - | No validation configured | - |

### Type Priority

When multiple validation types are specified, RuleKeeper uses this priority order:

```
validator > script > expression > match > ast_query >
pattern_match/anti_pattern_match > pattern/anti_pattern >
custom_validator > id (built-in)
```

---

## Rule Type Examples

### 1. BuiltIn Rules

Reference pre-coded analyzers by ID only:

```yaml
coding_standards:
  naming:
    rules:
      - id: CS-NAME-001  # Just the ID triggers built-in lookup
        severity: medium
        parameters:
          style: pascal_case
```

### 2. SimplePattern Rules

Basic regex matching:

```yaml
coding_standards:
  security:
    rules:
      - id: CUSTOM-001
        name: "No Console Output"
        severity: medium
        anti_pattern: "Console\\.(WriteLine|Write)\\s*\\("
        message: "Use ILogger instead of Console"
```

### 3. EnhancedPattern Rules

Regex with captures, options, and scope:

```yaml
coding_standards:
  security:
    rules:
      - id: CUSTOM-002
        name: "Hardcoded Secrets"
        severity: critical
        anti_pattern_match:
          regex: "(password|secret|apikey)\\s*[=:]\\s*[\"']([^\"']+)[\"']"
          options: [ignorecase]
          captures:
            type: "$1"
            value: "$2"
          message_template: "Hardcoded {type} found: {value}"
          scope: file
          negate: false
        fix_hint: "Use environment variables or a secrets manager"
```

**Pattern Options:**
- `ignorecase` - Case-insensitive matching
- `multiline` - ^ and $ match line boundaries
- `singleline` - Dot matches newlines
- `compiled` - Compile regex for performance

**Scope Options:**
- `file` - Match anywhere in file (default)
- `class` - Match within class bodies
- `method` - Match within method bodies
- `block` - Match within code blocks

### 4. AstQuery Rules

Declarative AST node matching:

```yaml
coding_standards:
  design:
    rules:
      - id: CUSTOM-003
        name: "No Public Fields"
        severity: high
        ast_query:
          node_kinds: [FieldDeclaration]
          properties:
            IsPublic: true
            IsConst: false
            IsStatic: false
          has_parent: [ClassDeclaration]
          has_children: []
        message: "Public fields should be properties"
```

**AST Query Properties:**
- `node_kinds` - List of syntax node types to match
- `properties` - Property conditions (key: value)
- `has_parent` - Required parent node types
- `has_children` - Required child node types
- `has_ancestor` - Required ancestor node types
- `has_descendant` - Required descendant node types

**Common Node Kinds (C#):**
- `ClassDeclaration`, `InterfaceDeclaration`, `StructDeclaration`
- `MethodDeclaration`, `PropertyDeclaration`, `FieldDeclaration`
- `IfStatement`, `ForStatement`, `WhileStatement`, `TryStatement`
- `InvocationExpression`, `BinaryExpression`, `LiteralExpression`

### 5. MultiMatch Rules (AND/OR/NONE Logic)

Combine multiple conditions:

```yaml
coding_standards:
  security:
    rules:
      - id: CUSTOM-004
        name: "SQL Injection Risk"
        severity: critical
        match:
          all:   # AND - all must match
            - pattern:
                regex: "(ExecuteSqlRaw|FromSqlRaw)"
            - ast_query:
                has_children: [BinaryExpression]
          any:   # OR - at least one must match
            - pattern:
                regex: "\\$\""
            - pattern:
                regex: "\\+ "
          none:  # NOT - none must match
            - pattern:
                regex: "@\\w+"
        message: "SQL injection risk - use parameterized queries"
```

**Match Operators:**
- `all` - All conditions must match (AND)
- `any` - At least one condition must match (OR)
- `none` - No condition must match (NOT)

### 6. Expression Rules

C# expressions for complex conditions:

```yaml
coding_standards:
  design:
    rules:
      - id: CUSTOM-005
        name: "Method Too Long"
        severity: medium
        expression:
          condition: "LineCount > MaxLines"
          variables:
            MaxLines: 30
        parameters:
          max_lines: 30
        message: "Method exceeds {max_lines} lines"
```

**Available Expression Variables:**
- `LineCount` - Number of lines in current node
- `ParameterCount` - Number of parameters (methods)
- `CyclomaticComplexity` - Complexity metric
- `NestingDepth` - Current nesting level
- `Node` - Current syntax node
- `Context` - Analysis context
- `Parameters` - Rule parameters dictionary

### 7. Script Rules

Full C# scripts for maximum flexibility:

```yaml
coding_standards:
  custom:
    rules:
      - id: CUSTOM-006
        name: "Custom Complexity Check"
        severity: high
        script:
          code: |
            var violations = new List<Violation>();
            foreach (var method in Context.GetMethods())
            {
                var complexity = CalculateComplexity(method);
                if (complexity > 10)
                {
                    violations.Add(CreateViolation(
                        method,
                        $"Cyclomatic complexity is {complexity} (max: 10)"
                    ));
                }
            }
            return violations;
          imports:
            - System.Linq
          references:
            - Microsoft.CodeAnalysis
```

**Script API:**
- `Context` - UnifiedAnalysisContext with AST and source
- `Context.GetMethods()` - Get all methods in file
- `Context.GetClasses()` - Get all classes in file
- `CreateViolation(node, message)` - Create violation
- `CalculateComplexity(node)` - Calculate cyclomatic complexity
- `GetLineCount(node)` - Get line count for node

### 8. Validator Reference/Inline

Reference existing validators or define inline:

```yaml
coding_standards:
  security:
    rules:
      # Reference to custom_validators section
      - id: CUSTOM-007
        name: "Use Console Validator"
        validator:
          reference: "no_console_output"
        parameters:
          severity: warning

      # Inline validator definition
      - id: CUSTOM-008
        name: "Inline Pattern"
        validator:
          inline:
            type: pattern
            pattern:
              regex: "TODO:"
              options: [ignorecase]
        message: "TODO comment found"
```

### 9. CustomValidator Reference

Direct reference to custom_validators section:

```yaml
coding_standards:
  maintenance:
    rules:
      - id: CUSTOM-009
        name: "Console Check"
        custom_validator: "no_console_output"
        severity: medium
```

---

## Configuration Sections

### metadata

Project metadata:

```yaml
metadata:
  name: "Project Name"
  description: "Code quality rules"
  author: "Team Name"
  created: "2024-01-01"
  updated: "2024-01-26"
```

### scan

Scan configuration:

```yaml
scan:
  languages: [csharp, python, javascript, typescript, java, go]

  include:
    - "**/*.cs"
    - "**/*.py"
    - "**/*.js"
    - "**/*.ts"

  exclude:
    - "**/bin/**"
    - "**/obj/**"
    - "**/node_modules/**"
    - "**/*.generated.cs"
    - "**/Migrations/**"

  parallel: true
  max_parallelism: 0  # 0 = auto (CPU count)
  cache: true
  max_file_size: 1048576  # 1MB

  baseline:
    enabled: false
    mode: git  # git, file, or date
    git_ref: main
    baseline_file: ".rulekeeper-baseline.json"
    since_date: "2024-01-01"
    include_uncommitted: true
    include_untracked: true
    filter_to_diff: true
    auto_update: false
    on_missing: warn  # warn, fail, or ignore
```

### output

Output configuration:

```yaml
output:
  format: console  # console, json, sarif, html
  min_severity: info  # info, low, medium, high, critical
  fail_on: high
  show_code: true
  show_hints: true
  colors: true
  visualization: true
  show_table: true
  max_per_rule: 10
  max_total: 100

  # Thresholds
  critical_threshold: 0
  high_threshold: 0
  total_threshold: 0
  threshold_percentage: 0
```

### prebuilt_policies

Enable/configure built-in policy sets:

```yaml
prebuilt_policies:
  security:
    enabled: true
    severity: critical
    skip: false
    exclude:
      - "**/Tests/**"
    skip_rules:
      - CS-SEC-005

  async_best_practices:
    enabled: true
    severity: high

  naming_conventions:
    enabled: true
    severity: medium

  design:
    enabled: true
    severity: medium

  exceptions:
    enabled: true
    severity: high

  dependency_injection:
    enabled: true
    severity: medium
```

### coding_standards

Custom rule categories:

```yaml
coding_standards:
  category_name:
    enabled: true
    skip: false
    severity: medium  # Default for category
    skip_rules:
      - RULE-TO-SKIP
    exclude:
      - "**/Tests/**"
    rules:
      - id: RULE-001
        name: "Rule Name"
        description: "What this rule checks"
        severity: medium
        enabled: true
        skip: false
        # ... rule definition (pattern, ast_query, etc.)
        message: "Violation message"
        fix_hint: "How to fix"
        parameters:
          key: value
        exclude:
          - "**/Exceptions/**"
        languages:
          - csharp
          - python
        file_pattern: "**/*Service.cs"
        applies_to:
          - methods
          - classes
```

### custom_validators

Reusable validators:

```yaml
custom_validators:
  no_console_output:
    enabled: true
    skip: false
    description: "Prevent Console.WriteLine in production"
    type: regex  # regex, pattern, roslyn, script
    regex: "Console\\.(WriteLine|Write|ReadLine|ReadKey)\\s*\\("
    options: [ignorecase]
    message_template: "Console I/O detected - use logging"
    severity: medium
    exclude:
      - "**/Program.cs"
      - "**/*.Test*.cs"

  todo_comments:
    enabled: true
    type: regex
    regex: "//\\s*(TODO|FIXME|HACK|XXX):"
    message_template: "Found {match} comment"
    severity: info

  custom_assembly_validator:
    enabled: true
    type: assembly
    assembly: "./plugins/CustomRules.dll"
    type_name: "MyCompany.Rules.CustomValidator"
```

### custom_rules

External rule assemblies:

```yaml
custom_rules:
  - path: "./plugins/MyRules.dll"
  - path: "./rules/"
  - nuget: "MyCompany.RuleKeeper.Rules"
  - nuget: "MyCompany.RuleKeeper.Rules:1.0.0"
```

---

## Complete Example

```yaml
# rulekeeper.yaml - Complete Example
version: "1.0"

metadata:
  name: "Enterprise Coding Standards"
  description: "Code quality rules for enterprise applications"
  author: "Platform Team"
  created: "2024-01-01"
  updated: "2024-01-26"

scan:
  languages: [csharp, typescript, python]
  include:
    - "src/**/*.cs"
    - "src/**/*.ts"
    - "src/**/*.py"
  exclude:
    - "**/bin/**"
    - "**/obj/**"
    - "**/node_modules/**"
    - "**/*.generated.cs"
    - "**/Migrations/**"
  parallel: true
  cache: true
  baseline:
    enabled: true
    mode: git
    git_ref: main
    filter_to_diff: true

output:
  format: console
  min_severity: info
  fail_on: high
  show_code: true
  show_hints: true
  colors: true

prebuilt_policies:
  security:
    enabled: true
    severity: critical
    exclude: ["**/Tests/**"]

  async_best_practices:
    enabled: true
    severity: high

  naming_conventions:
    enabled: true
    severity: medium

coding_standards:
  security:
    enabled: true
    severity: critical
    rules:
      # EnhancedPattern - SQL Injection
      - id: ENT-SEC-001
        name: "SQL Injection Prevention"
        severity: critical
        anti_pattern_match:
          regex: "FromSqlRaw\\s*\\(\\s*\\$|ExecuteSqlRaw\\s*\\(\\s*\\$"
          message_template: "SQL injection risk detected"
        fix_hint: "Use parameterized queries"

      # EnhancedPattern - Hardcoded Secrets
      - id: ENT-SEC-002
        name: "No Hardcoded Secrets"
        severity: critical
        anti_pattern_match:
          regex: "(password|secret|apikey)\\s*[=:]\\s*[\"'][^\"']{8,}[\"']"
          options: [ignorecase]
        fix_hint: "Use environment variables or secrets manager"
        exclude: ["**/appsettings.Development.json"]

  design:
    enabled: true
    rules:
      # AstQuery - Public Fields
      - id: ENT-DESIGN-001
        name: "No Public Fields"
        severity: high
        ast_query:
          node_kinds: [FieldDeclaration]
          properties:
            IsPublic: true
            IsConst: false
        message: "Use properties instead of public fields"

      # Expression - Method Length
      - id: ENT-DESIGN-002
        name: "Method Length"
        severity: medium
        expression:
          condition: "LineCount > 30"
        parameters:
          max_lines: 30
        message: "Method exceeds 30 lines"

  async:
    enabled: true
    rules:
      # MultiMatch - Blocking Async
      - id: ENT-ASYNC-001
        name: "No Blocking on Async"
        severity: critical
        match:
          all:
            - pattern:
                regex: "\\.(Result|Wait)\\s*[;\\)]"
          none:
            - ast_query:
                has_parent: [MainMethod]
        message: "Use await instead of .Result or .Wait()"

custom_validators:
  no_console:
    enabled: true
    type: regex
    regex: "Console\\.(WriteLine|Write)\\s*\\("
    message_template: "Use ILogger instead"
    severity: medium
    exclude: ["**/Program.cs"]

  todo_comments:
    enabled: true
    type: regex
    regex: "//\\s*(TODO|FIXME):"
    message_template: "Found {match}"
    severity: info

custom_rules:
  - path: "./plugins/EnterpriseRules.dll"
```

---

*RuleKeeper Documentation (c) 2024*
