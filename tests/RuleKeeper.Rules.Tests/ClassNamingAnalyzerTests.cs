using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Rules.Naming;
using RuleKeeper.Sdk;
using Xunit;

namespace RuleKeeper.Rules.Tests;

public class ClassNamingAnalyzerTests
{
    private readonly ClassNamingAnalyzer _analyzer;

    public ClassNamingAnalyzerTests()
    {
        _analyzer = new ClassNamingAnalyzer();
        _analyzer.Initialize(new Dictionary<string, object>());
    }

    [Fact]
    public void Analyze_WithPascalCaseClass_ReturnsNoViolations()
    {
        var code = @"
public class MyClassName
{
}
";
        var violations = AnalyzeCode(code);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_WithLowerCaseClass_ReturnsViolation()
    {
        var code = @"
public class myClassName
{
}
";
        var violations = AnalyzeCode(code);

        violations.Should().HaveCount(1);
        violations[0].RuleId.Should().Be("CS-NAME-001");
        violations[0].Message.Should().Contain("myClassName");
    }

    [Fact]
    public void Analyze_WithMultipleClasses_ReturnsViolationsForInvalid()
    {
        var code = @"
public class ValidClass { }
public class invalidClass { }
public class AnotherValidClass { }
public class another_invalid { }
";
        var violations = AnalyzeCode(code);

        violations.Should().HaveCount(2);
        violations.Select(v => v.Message).Should().Contain(m => m.Contains("invalidClass"));
        violations.Select(v => v.Message).Should().Contain(m => m.Contains("another_invalid"));
    }

    [Fact]
    public void Analyze_WithAllowUnderscores_AcceptsUnderscores()
    {
        var analyzer = new ClassNamingAnalyzer();
        analyzer.Initialize(new Dictionary<string, object> { ["allow_underscores"] = true });

        var code = @"
public class My_Class_Name
{
}
";
        var tree = CSharpSyntaxTree.ParseText(code);
        var context = new AnalysisContext
        {
            SyntaxTree = tree,
            FilePath = "test.cs",
            Severity = SeverityLevel.Medium
        };

        var violations = analyzer.Analyze(context).ToList();

        violations.Should().BeEmpty();
    }

    private List<Violation> AnalyzeCode(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var context = new AnalysisContext
        {
            SyntaxTree = tree,
            FilePath = "test.cs",
            Severity = SeverityLevel.Medium
        };

        return _analyzer.Analyze(context).ToList();
    }
}
