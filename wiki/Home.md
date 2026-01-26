# RuleKeeper

**Multi-language policy-as-code static analysis for modern development teams**

| Version | Languages | Rules | Platform |
|---------|-----------|-------|----------|
| v2.0 | 6 | 28+ | Cross-Platform |

---

## Documentation

### [[CLI Reference]]
Complete command-line interface documentation. Learn all available commands, options, and usage examples.

**Tags:** Commands, Options, CI/CD

---

### [[Rules Reference]]
Documentation for all built-in rules across all supported languages. Includes severity levels and parameters.

**Tags:** 28+ Rules, Cross-Language, Configurable

---

### [[SDK Reference]]
Build custom rules with the RuleKeeper SDK. Create C#-specific or cross-language rules for your team.

**Tags:** Custom Rules, API Reference, Examples

---

### [[Usage Examples]]
Practical examples for common scenarios: CI/CD integration, multi-language analysis, configuration, and custom rules.

**Tags:** Quick Start, CI/CD, Workflows

---

## Supported Languages

| Language | Implementation | Extensions |
|----------|----------------|------------|
| **C#** | Full Roslyn analysis | .cs |
| **Python** | ANTLR-based | .py, .pyw |
| **JavaScript** | ANTLR-based | .js, .jsx, .mjs |
| **TypeScript** | ANTLR-based | .ts, .tsx, .mts |
| **Java** | ANTLR-based | .java |
| **Go** | ANTLR-based | .go |

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Multi-Language** | Analyze code across C#, Python, JavaScript, TypeScript, Java, and Go in a single scan |
| **Extensible** | Create custom rules using the SDK or define rules directly in YAML without code |
| **SARIF Output** | Generate SARIF reports for GitHub Code Scanning, Azure DevOps, and other CI/CD tools |
| **Configurable** | Fine-tune every rule with YAML configuration. Set thresholds, severities, and parameters |
| **Baseline/Incremental** | Add to existing projects without breaking builds - only scan new additions |
| **Fast** | Parallel analysis with intelligent caching for fast feedback in large codebases |
| **Detailed Reports** | Console, JSON, SARIF, and HTML reports with code snippets and fix suggestions |
| **YAML Custom Rules** | Define regex patterns, AST queries, and multi-pattern rules directly in configuration |

---

*RuleKeeper Documentation (c) 2024*
