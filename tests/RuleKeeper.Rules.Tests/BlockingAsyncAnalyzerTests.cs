using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Rules.Async;
using RuleKeeper.Sdk;
using Xunit;

namespace RuleKeeper.Rules.Tests;

public class BlockingAsyncAnalyzerTests
{
    private readonly BlockingAsyncAnalyzer _analyzer;

    public BlockingAsyncAnalyzerTests()
    {
        _analyzer = new BlockingAsyncAnalyzer();
        _analyzer.Initialize(new Dictionary<string, object>());
    }

    [Fact]
    public void Analyze_WithTaskResult_ReturnsViolation()
    {
        var code = @"
using System.Threading.Tasks;

public class TestClass
{
    public void Method()
    {
        var task = Task.Run(() => ""Hello"");
        var result = task.Result;
    }
}
";
        var violations = AnalyzeCode(code);

        violations.Should().NotBeEmpty();
        violations.Should().Contain(v => v.Message.Contains(".Result"));
    }

    [Fact]
    public void Analyze_WithTaskWait_ReturnsViolation()
    {
        var code = @"
using System.Threading.Tasks;

public class TestClass
{
    public void Method()
    {
        var task = Task.Run(() => { });
        task.Wait();
    }
}
";
        var violations = AnalyzeCode(code);

        violations.Should().NotBeEmpty();
        violations.Should().Contain(v => v.Message.Contains(".Wait"));
    }

    [Fact]
    public void Analyze_WithGetAwaiterGetResult_ReturnsViolation()
    {
        var code = @"
using System.Threading.Tasks;

public class TestClass
{
    public void Method()
    {
        var task = Task.Run(() => ""Hello"");
        var result = task.GetAwaiter().GetResult();
    }
}
";
        var violations = AnalyzeCode(code);

        violations.Should().NotBeEmpty();
        violations.Should().Contain(v => v.Message.Contains("GetAwaiter().GetResult()"));
    }

    [Fact]
    public void Analyze_WithAwait_ReturnsNoViolations()
    {
        var code = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task<string> Method()
    {
        var result = await Task.Run(() => ""Hello"");
        return result;
    }
}
";
        var violations = AnalyzeCode(code);

        // Should not flag proper async/await usage
        violations.Should().NotContain(v => v.Message.Contains("await"));
    }

    [Fact]
    public void Analyze_WithAllowInMain_SkipsMainMethod()
    {
        var analyzer = new BlockingAsyncAnalyzer();
        analyzer.Initialize(new Dictionary<string, object> { ["allow_in_main"] = true });

        var code = @"
using System.Threading.Tasks;

public class Program
{
    public static void Main()
    {
        var task = Task.Run(() => ""Hello"");
        var result = task.Result;
    }
}
";
        var tree = CSharpSyntaxTree.ParseText(code);
        var context = new AnalysisContext
        {
            SyntaxTree = tree,
            FilePath = "test.cs",
            Severity = SeverityLevel.High
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
            Severity = SeverityLevel.High
        };

        return _analyzer.Analyze(context).ToList();
    }
}
