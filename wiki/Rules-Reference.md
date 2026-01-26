# RuleKeeper Rules Reference

Complete documentation of all built-in rules across all supported languages

---

## Table of Contents

- [Overview](#overview)
- [Severity Levels](#severity-levels)
- [Cross-Language Rules](#cross-language-rules)
- [C# Rules](#c-rules)
  - [Naming](#c-naming-rules)
  - [Security](#c-security-rules)
  - [Async](#c-async-rules)
  - [Design](#c-design-rules)
  - [Exceptions](#c-exception-rules)
  - [Dependency Injection](#c-dependency-injection-rules)
- [Python Rules](#python-rules)
  - [Naming](#python-naming-rules)
  - [Documentation](#python-documentation-rules)
- [JavaScript Rules](#javascript-rules)
  - [Debugging](#javascript-debugging-rules)
  - [Best Practices](#javascript-best-practices)
- [Custom Rules](#custom-rules)
  - [YAML-Based Rules](#yaml-based-rules)
  - [External DLL Plugins](#external-dll-plugins)

---

## Overview

RuleKeeper provides a comprehensive set of rules for static code analysis across multiple programming languages. Rules are categorized by type and can be configured individually.

| Metric | Count |
|--------|-------|
| **Total Rules** | 28+ |
| **Languages** | 6 |
| **Cross-Language Rules** | 4 |
| **Categories** | 8 |

---

## Severity Levels

| Level | Description | Typical Use |
|-------|-------------|-------------|
| **Critical** | Must be fixed immediately | Security vulnerabilities, blocking async calls |
| **High** | Should be fixed soon | Async void, empty catch blocks, service locator |
| **Medium** | Should be addressed | Naming conventions, design issues, complexity |
| **Low** | Nice to fix | Style issues, minor conventions |
| **Info** | Informational | Suggestions, best practices |

---

## Cross-Language Rules

These rules work across all supported languages using the unified AST.

**Supported:** C#, Python, JavaScript, TypeScript, Java, Go

### XL-DESIGN-001: Method Length

**Severity:** Medium

Methods should not exceed a maximum number of lines. Long methods are harder to understand and maintain.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `max_lines` | Maximum lines per method | 50 |

---

### XL-DESIGN-002: Cyclomatic Complexity

**Severity:** Medium

Methods should not have high cyclomatic complexity. Complex methods are harder to test and maintain.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `max_complexity` | Maximum complexity score | 10 |

---

### XL-DESIGN-003: Parameter Count

**Severity:** Medium

Methods should not have too many parameters. Consider using a parameter object instead.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `max_parameters` | Maximum parameters allowed | 5 |

---

### XL-NAME-001: Naming Conventions

**Severity:** Low

Checks naming conventions for classes, methods, and variables according to language-specific standards.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `class_style` | Naming style for classes | pascal |
| `method_style` | Naming style for methods | auto |
| `variable_style` | Naming style for variables | auto |

---

## C# Rules

### C# Naming Rules

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| `CS-NAME-001` | Class Naming | Medium | Classes must use PascalCase naming |
| `CS-NAME-002` | Method Naming | Medium | Methods must use PascalCase naming |
| `CS-NAME-003` | Async Method Naming | Low | Async methods should end with "Async" suffix |
| `CS-NAME-004` | Private Field Naming | Medium | Private fields should use _camelCase |
| `CS-NAME-005` | Constant Naming | Medium | Constants should use PascalCase |
| `CS-NAME-006` | Property Naming | Medium | Properties must use PascalCase |
| `CS-NAME-007` | Interface Naming | Medium | Interfaces must start with "I" prefix |

---

### C# Security Rules

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| `CS-SEC-001` | SQL Injection | **Critical** | Detects potential SQL injection vulnerabilities |
| `CS-SEC-002` | Hardcoded Secrets | **Critical** | Detects hardcoded passwords, API keys, and secrets |
| `CS-SEC-003` | Sensitive Data Logging | High | Prevents logging of sensitive data |
| `CS-SEC-004` | XSS Prevention | **Critical** | Detects potential XSS vulnerabilities |
| `CS-SEC-005` | Path Traversal | High | Detects path traversal vulnerabilities |

---

### C# Async Rules

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| `CS-ASYNC-001` | Async Void | High | Avoid async void methods (except event handlers) |
| `CS-ASYNC-002` | Blocking Async | **Critical** | Avoid .Result and .Wait() on tasks |
| `CS-ASYNC-003` | ConfigureAwait | Low | Use ConfigureAwait(false) in library code |

---

### C# Design Rules

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| `CS-DESIGN-001` | Method Length | Medium | Methods should not exceed max lines |
| `CS-DESIGN-002` | Parameter Count | Medium | Methods should not have too many parameters |
| `CS-DESIGN-003` | Cyclomatic Complexity | Medium | Methods should have low complexity |
| `CS-DESIGN-004` | Class Length | Medium | Classes should not exceed max lines |

---

### C# Exception Rules

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| `CS-EXC-001` | Empty Catch | High | Catch blocks should not be empty |
| `CS-EXC-002` | Catch Base Exception | Medium | Avoid catching System.Exception |
| `CS-EXC-003` | Throw in Finally | High | Avoid throwing in finally blocks |

---

### C# Dependency Injection Rules

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| `CS-DI-001` | Concrete Type Injection | Medium | Prefer interface injection over concrete types |
| `CS-DI-002` | Service Locator | High | Avoid service locator anti-pattern |

---

## Python Rules

### Python Naming Rules

#### PY-NAME-001: PEP 8 Naming Conventions

**Severity:** Low

Enforces Python PEP 8 naming conventions: snake_case for functions and variables, PascalCase for classes, UPPER_CASE for constants.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `allow_private_underscore` | Allow single leading underscore for private | true |

---

### Python Documentation Rules

#### PY-DOC-001: Missing Docstring

**Severity:** Low

Public functions and classes should have docstrings describing their purpose, parameters, and return values.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `require_class_docstring` | Require docstrings for classes | true |
| `require_function_docstring` | Require docstrings for functions | true |
| `min_function_length` | Min lines before requiring docstring | 5 |
| `skip_private` | Skip private functions | true |

---

## JavaScript Rules

### JavaScript Debugging Rules

**Applies to:** JavaScript, TypeScript

#### JS-DEBUG-001: No Console Log

**Severity:** Low

console.log statements should not be committed to production code. Use a proper logging framework instead.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `allow_error_warn` | Allow console.error and console.warn | true |
| `exclude_patterns` | File patterns to exclude | *.test.js,*.spec.js |

---

### JavaScript Best Practices

**Applies to:** JavaScript, TypeScript

#### JS-VAR-001: Prefer Const/Let

**Severity:** Medium

Use const or let instead of var for variable declarations. var has function scope which can lead to unexpected behavior.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `prefer_const` | Suggest const as preferred | true |

---

## Custom Rules

RuleKeeper supports defining custom rules directly in YAML configuration without writing C# code.

### YAML-Based Rules

#### Rule Types

| Type | Description | Use Case |
|------|-------------|----------|
| **Pattern Match** | Regex-based text matching | Find specific code patterns |
| **Anti-Pattern** | Regex to detect violations | Detect forbidden patterns |
| **AST Query** | Query syntax tree nodes | Check code structure |
| **Multi-Match** | Combine conditions (AND/OR/NOT) | Complex rule logic |
| **Expression** | C# expression evaluation | Dynamic conditions |
| **Script** | Full C# script execution | Complex analysis |

#### Pattern Match Rule

```yaml
coding_standards:
  security:
    rules:
      detect_hardcoded_ip:
        id: CUSTOM-SEC-010
        name: "Hardcoded IP Address"
        severity: medium
        anti_pattern_match:
          regex: "\\b(?:[0-9]{1,3}\\.){3}[0-9]{1,3}\\b"
          scope: string_literals  # Only in strings
          message_template: "Hardcoded IP address detected: {match}"
        fix_hint: "Use configuration or environment variables"
```

#### AST Query Rule

```yaml
coding_standards:
  design:
    rules:
      no_nested_loops:
        id: CUSTOM-DESIGN-010
        name: "No Nested Loops"
        severity: medium
        ast_query:
          node_kinds: [ForStatement, WhileStatement, ForEachStatement]
          has_children: [ForStatement, WhileStatement, ForEachStatement]
        message: "Avoid nested loops - consider extracting to a method"
```

#### Multi-Pattern Rule

```yaml
coding_standards:
  async:
    rules:
      async_without_await:
        id: CUSTOM-ASYNC-001
        name: "Async Without Await"
        severity: warning
        match:
          all:
            - ast_query:
                node_kinds: [MethodDeclaration]
                properties:
                  IsAsync: true
          none:
            - ast_query:
                has_children: [AwaitExpression]
        message: "Async method contains no await expressions"
```

### External DLL Plugins

Load custom rules from external DLL assemblies:

```yaml
custom_rules:
  - path: "./plugins/MyCompany.Rules.dll"
  - nuget: "MyCompany.RuleKeeper.Rules:1.0.0"

custom_validators:
  company_standards:
    type: assembly
    assembly: "./plugins/CompanyRules.dll"
    type_name: "Company.Rules.StandardsValidator"
```

See the [[SDK Reference]] for building custom rule assemblies.

---

*RuleKeeper Documentation (c) 2024*
