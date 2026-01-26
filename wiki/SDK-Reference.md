# RuleKeeper SDK Reference

Build custom rules for multi-language static code analysis

---

## Table of Contents

- [Overview](#overview)
- [SDK Packages](#sdk-packages)
- [Installation](#installation)
- [Core Abstractions](#core-abstractions)
  - [IUnifiedSyntaxNode](#iunifiedsyntaxnode)
  - [UnifiedSyntaxKind](#unifiedsyntaxkind)
  - [SourceLocation](#sourcelocation)
  - [ILanguageAdapter](#ilanguageadapter)
- [Rule Development](#rule-development)
  - [ICrossLanguageRule](#icrosslanguagerule)
  - [BaseCrossLanguageRule](#basecrosslanguagerule)
  - [BaseRuleAnalyzer (C# Specific)](#baseruleanalyzer-c-specific)
- [Attributes](#attributes)
  - [RuleAttribute](#ruleattribute)
  - [RuleParameterAttribute](#ruleparameterattribute)
  - [SupportedLanguagesAttribute](#supportedlanguagesattribute)
- [Context & Results](#context--results)
  - [UnifiedAnalysisContext](#unifiedanalysiscontext)
  - [Violation](#violation)
- [Examples](#examples)

---

## Overview

The RuleKeeper SDK provides the building blocks for creating custom static analysis rules. With the SDK, you can:

- Create **C#-specific rules** using Roslyn's full semantic analysis
- Create **cross-language rules** that work across all supported languages
- Configure rule parameters via YAML configuration
- Generate violations with code snippets and fix hints

> **Tip:** For rules that check structural patterns (method length, parameter count, naming), use cross-language rules. For language-specific semantic analysis (type checking, symbol resolution), use language-specific rules.

---

## SDK Packages

### RuleKeeper.Sdk

Core SDK with language-agnostic abstractions. Contains unified AST interfaces, rule interfaces, attributes, and violation types. Use this for cross-language rules.

```bash
dotnet add package RuleKeeper.Sdk
```

### RuleKeeper.Sdk.CSharp

C#-specific SDK with Roslyn integration. Provides full semantic analysis, type resolution, and access to Roslyn's rich API. Use this for C#-only rules.

```bash
dotnet add package RuleKeeper.Sdk.CSharp
```

---

## Installation

### Create a Custom Rules Project

```bash
dotnet new classlib -n MyCompany.RuleKeeper.Rules
cd MyCompany.RuleKeeper.Rules

# For cross-language rules
dotnet add package RuleKeeper.Sdk

# For C#-specific rules (includes RuleKeeper.Sdk)
dotnet add package RuleKeeper.Sdk.CSharp
```

### Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="RuleKeeper.Sdk" Version="2.0.0" />
    <!-- Or for C#-specific rules: -->
    <!-- <PackageReference Include="RuleKeeper.Sdk.CSharp" Version="2.0.0" /> -->
  </ItemGroup>
</Project>
```

---

## Core Abstractions

### IUnifiedSyntaxNode

The core interface representing a node in the unified abstract syntax tree. Works across all supported languages.

```csharp
interface IUnifiedSyntaxNode
{
    UnifiedSyntaxKind Kind { get; }
    SourceLocation Location { get; }
    string Text { get; }
    IUnifiedSyntaxNode? Parent { get; }
    IEnumerable<IUnifiedSyntaxNode> Children { get; }
    Language Language { get; }
    bool ContainsErrors { get; }
    object? NativeNode { get; }
}
```

#### Extension Methods

| Method | Description |
|--------|-------------|
| `Descendants()` | Gets all descendant nodes |
| `DescendantsOfKind(kind)` | Gets descendants of a specific kind |
| `FirstChildOfKind(kind)` | Gets the first child of a specific kind |
| `Ancestors()` | Gets all ancestor nodes |

---

### UnifiedSyntaxKind

Enumeration of common syntax node kinds across all languages.

**Type Declarations:**
```
ClassDeclaration, StructDeclaration, InterfaceDeclaration,
EnumDeclaration, RecordDeclaration, DelegateDeclaration
```

**Members:**
```
MethodDeclaration, ConstructorDeclaration, PropertyDeclaration,
FieldDeclaration, EventDeclaration, IndexerDeclaration,
Parameter, ParameterList, TypeParameter
```

**Statements:**
```
Block, IfStatement, ElseClause, SwitchStatement, SwitchCase,
ForStatement, ForEachStatement, WhileStatement, DoWhileStatement,
TryStatement, CatchClause, FinallyClause,
ReturnStatement, ThrowStatement, BreakStatement, ContinueStatement
```

**Expressions:**
```
InvocationExpression, MemberAccessExpression, ObjectCreationExpression,
BinaryExpression, UnaryExpression, ConditionalExpression,
LambdaExpression, AwaitExpression, AssignmentExpression
```

---

### SourceLocation

Represents a location in source code, independent of the underlying parser.

```csharp
struct SourceLocation
{
    string FilePath { get; }
    int StartLine { get; }
    int StartColumn { get; }
    int EndLine { get; }
    int EndColumn { get; }
    bool IsValid { get; }
}
```

**Constructor:**

```csharp
var location = new SourceLocation(
    filePath: "/path/to/file.cs",
    startLine: 10,
    startColumn: 5,
    endLine: 10,
    endColumn: 20
);
```

---

### ILanguageAdapter

Interface for language-specific parsers that produce unified AST nodes.

```csharp
interface ILanguageAdapter
{
    Language Language { get; }
    IEnumerable<string> SupportedExtensions { get; }
    IEnumerable<string> DefaultIncludePatterns { get; }
    IEnumerable<string> DefaultExcludePatterns { get; }
    Task<IUnifiedSyntaxNode> ParseAsync(source, filePath, ct);
    Task<ITypeResolver?> CreateTypeResolverAsync(files, ct);
    bool CanHandle(filePath);
}
```

---

## Rule Development

### ICrossLanguageRule

Interface for rules that work across multiple languages using the unified AST.

```csharp
interface ICrossLanguageRule
{
    string RuleId { get; }
    string RuleName { get; }
    string Category { get; }
    string Description { get; }
    SeverityLevel DefaultSeverity { get; }
    IEnumerable<Language> SupportedLanguages { get; }
    void Initialize(parameters);
    IEnumerable<Violation> Analyze(UnifiedAnalysisContext);
}
```

---

### BaseCrossLanguageRule

Abstract base class for cross-language rules. Handles attribute parsing, parameter initialization, and violation creation.

```csharp
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

[Rule("XL-CUSTOM-001",
    Name = "Custom Cross-Language Rule",
    Description = "Description of what this rule checks",
    Severity = SeverityLevel.Medium,
    Category = "custom")]
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript)]
public class MyCrossLanguageRule : BaseCrossLanguageRule
{
    [RuleParameter("threshold", Description = "Max threshold", DefaultValue = 10)]
    public int Threshold { get; set; } = 10;

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        foreach (var method in context.GetMethods())
        {
            // Analysis logic here
            if (/* violation detected */)
            {
                yield return CreateViolation(
                    method,
                    "Violation message",
                    context,
                    "Fix suggestion"
                );
            }
        }
    }
}
```

#### Helper Methods

| Method | Description |
|--------|-------------|
| `CreateViolation(node, message, context, fixHint)` | Creates a violation at a syntax node |
| `CreateViolation(location, message, context, fixHint)` | Creates a violation at a source location |
| `GetParameter<T>(name, defaultValue)` | Gets a parameter value with fallback |
| `HasParameter(name)` | Checks if a parameter exists |
| `SupportsLanguage(language)` | Checks if a language is supported |

---

### BaseRuleAnalyzer (C# Specific)

Abstract base class for C#-specific rules with full Roslyn integration.

```csharp
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Attributes;

[Rule("CS-CUSTOM-001",
    Name = "My C# Rule",
    Description = "C#-specific analysis",
    Severity = SeverityLevel.Medium,
    Category = "custom")]
public class MyCSharpRule : BaseRuleAnalyzer
{
    public override IEnumerable<Violation> Analyze(AnalysisContext context)
    {
        // Access Roslyn SyntaxTree and SemanticModel
        var tree = context.SyntaxTree;
        var model = context.SemanticModel;

        var methods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Full semantic analysis available
            var symbol = model.GetDeclaredSymbol(method);

            if (/* violation detected */)
            {
                yield return CreateViolation(
                    method.GetLocation(),
                    "Violation message",
                    context,
                    "Fix hint"
                );
            }
        }
    }
}
```

---

## Attributes

### RuleAttribute

Attribute to define rule metadata.

```csharp
[Rule("RULE-ID",
    Name = "Human-readable name",
    Description = "Detailed description",
    Severity = SeverityLevel.Medium,
    Category = "category-name")]
public class MyRule : BaseCrossLanguageRule { }
```

| Property | Type | Description |
|----------|------|-------------|
| `RuleId` | string | Unique identifier (constructor parameter) |
| `Name` | string | Display name for the rule |
| `Description` | string | Detailed description |
| `Severity` | SeverityLevel | Default severity (Critical, High, Medium, Low, Info) |
| `Category` | string | Rule category for grouping |

---

### RuleParameterAttribute

Attribute to define configurable parameters for rules.

```csharp
[RuleParameter("max_lines",
    Description = "Maximum lines allowed",
    DefaultValue = 50)]
public int MaxLines { get; set; } = 50;
```

Parameters can be configured in YAML:

```yaml
rules:
  XL-DESIGN-001:
    enabled: true
    parameters:
      max_lines: 100
```

---

### SupportedLanguagesAttribute

Attribute to specify which languages a rule supports.

```csharp
[SupportedLanguages(Language.CSharp, Language.Python, Language.JavaScript)]
public class MyRule : BaseCrossLanguageRule { }
```

#### Available Languages

| Language | Enum Value |
|----------|------------|
| C# | `Language.CSharp` |
| Python | `Language.Python` |
| JavaScript | `Language.JavaScript` |
| TypeScript | `Language.TypeScript` |
| Java | `Language.Java` |
| Go | `Language.Go` |

---

## Context & Results

### UnifiedAnalysisContext

Context provided to cross-language rules during analysis.

```csharp
class UnifiedAnalysisContext
{
    IUnifiedSyntaxNode Root { get; }
    string FilePath { get; }
    string? SourceText { get; }
    Language Language { get; }
    ITypeResolver? TypeResolver { get; }
    SeverityLevel Severity { get; }
    CancellationToken CancellationToken { get; }
}
```

#### Helper Methods

```csharp
// Get all methods in the file
var methods = context.GetMethods();

// Get all classes
var classes = context.GetClasses();

// Get a specific line of code
var line = context.GetLine(lineNumber);

// Get nodes of specific kind
var loops = context.GetNodesOfKind(UnifiedSyntaxKind.ForStatement);
```

---

### Violation

Represents a rule violation found during analysis.

#### Factory Methods

```csharp
// From a unified syntax node
var violation = Violation.FromUnifiedNode(
    node,
    ruleId: "XL-001",
    ruleName: "Rule Name",
    message: "Violation message",
    severity: SeverityLevel.Medium,
    fixHint: "How to fix",
    sourceText: context.SourceText
);

// From a source location
var violation = Violation.FromSourceLocation(
    location,
    ruleId: "XL-001",
    ruleName: "Rule Name",
    message: "Violation message",
    severity: SeverityLevel.Medium,
    language: Language.Python,
    fixHint: "How to fix"
);

// From a Roslyn location (C# only)
var violation = Violation.FromLocation(
    roslynLocation,
    ruleId: "CS-001",
    ruleName: "Rule Name",
    message: "Violation message",
    severity: SeverityLevel.Medium,
    fixHint: "How to fix"
);
```

---

## Examples

### Complete Cross-Language Rule Example

```csharp
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using RuleKeeper.Sdk.Attributes;
using RuleKeeper.Sdk.Rules;

namespace MyCompany.RuleKeeper.Rules;

/// <summary>
/// Detects TODO comments in code that should be addressed.
/// </summary>
[Rule("XL-TODO-001",
    Name = "TODO Comments",
    Description = "TODO comments indicate incomplete work that should be tracked",
    Severity = SeverityLevel.Low,
    Category = "documentation")]
[SupportedLanguages(
    Language.CSharp,
    Language.Python,
    Language.JavaScript,
    Language.TypeScript,
    Language.Java,
    Language.Go)]
public class TodoCommentAnalyzer : BaseCrossLanguageRule
{
    [RuleParameter("patterns",
        Description = "Comment patterns to detect",
        DefaultValue = "TODO,FIXME,HACK,XXX")]
    public string Patterns { get; set; } = "TODO,FIXME,HACK,XXX";

    [RuleParameter("case_sensitive",
        Description = "Use case-sensitive matching",
        DefaultValue = false)]
    public bool CaseSensitive { get; set; } = false;

    public override IEnumerable<Violation> Analyze(UnifiedAnalysisContext context)
    {
        var patternList = Patterns.Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var comparison = CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var lines = context.SourceText?.Split('\n') ?? Array.Empty<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];

            foreach (var pattern in patternList)
            {
                if (line.Contains(pattern, comparison))
                {
                    var location = new SourceLocation(
                        context.FilePath,
                        i + 1, 1,
                        i + 1, line.Length);

                    yield return Violation.FromSourceLocation(
                        location,
                        RuleId,
                        RuleName,
                        $"Found {pattern} comment that should be addressed",
                        DefaultSeverity,
                        context.Language,
                        "Create a work item to track this TODO"
                    );

                    break; // Only report once per line
                }
            }
        }
    }
}
```

### Configuration

```yaml
# rulekeeper.yaml
rules:
  XL-TODO-001:
    enabled: true
    severity: Medium
    parameters:
      patterns: "TODO,FIXME,HACK"
      case_sensitive: false
```

### Loading Custom Rules

```yaml
# rulekeeper.yaml
custom_rules:
  - path: "./rules/MyCompany.RuleKeeper.Rules.dll"
```

---

## YAML-Based Custom Rules

For simpler rules, you can define them directly in YAML without writing C# code.

### Validator Types

| Type | Description |
|------|-------------|
| `regex` | Pattern matching using regular expressions |
| `ast_query` | Query unified AST nodes by kind and properties |
| `expression` | Evaluate C# expressions |
| `script` | Execute full C# scripts |
| `assembly` | Load validators from external DLLs |

### Regex Validator

```yaml
custom_validators:
  no_console:
    type: regex
    description: "Detect Console.WriteLine usage"
    regex: "Console\\.(WriteLine|Write)\\s*\\("
    options: [ignorecase]
    message_template: "Use ILogger instead of {match}"
```

### AST Query Validator

```yaml
coding_standards:
  design:
    rules:
      no_public_fields:
        id: CUSTOM-001
        ast_query:
          node_kinds: [FieldDeclaration]
          properties:
            IsPublic: true
            IsConst: false
          exclude_parents: [InterfaceDeclaration]
```

### Expression Validator

```yaml
coding_standards:
  complexity:
    rules:
      method_length:
        id: CUSTOM-002
        expression:
          condition: "Node is IMethodNode m && m.GetLineCount() > Parameters.GetInt(\"max\")"
        parameters:
          max: 50
```

### Script Validator

```yaml
custom_validators:
  complex_check:
    type: script
    description: "Complex validation logic"
    script: |
      var violations = new List<Violation>();
      foreach (var method in GetMethods())
      {
          if (method.Parameters.Count > 5)
          {
              violations.Add(CreateViolation(method, "Too many parameters"));
          }
      }
      return violations;
```

### Plugin Providers

Implement `IRuleProvider` to create plugin assemblies:

```csharp
using RuleKeeper.Sdk.Plugins;

public class MyRuleProvider : IRuleProvider
{
    public RuleProviderMetadata Metadata => new()
    {
        Name = "MyCompany Rules",
        Version = "1.0.0",
        Author = "MyCompany"
    };

    public IEnumerable<ICrossLanguageRule> GetCrossLanguageRules()
    {
        yield return new MyCustomRule();
    }

    public IEnumerable<IRoslynRule> GetRoslynRules()
    {
        yield return new MyCSharpRule();
    }
}
```

Load in configuration:

```yaml
custom_rules:
  - path: "./plugins/MyCompany.Rules.dll"
```

---

*RuleKeeper SDK Documentation (c) 2024*
