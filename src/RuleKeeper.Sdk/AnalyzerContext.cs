using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RuleKeeper.Sdk;

/// <summary>
/// Context for analyzing a single C# file.
/// </summary>
public record AnalysisContext
{
    /// <summary>
    /// The syntax tree of the file being analyzed.
    /// </summary>
    public required SyntaxTree SyntaxTree { get; init; }

    /// <summary>
    /// The semantic model for the file, if available.
    /// </summary>
    public SemanticModel? SemanticModel { get; init; }

    /// <summary>
    /// The compilation containing the file, if available.
    /// </summary>
    public CSharpCompilation? Compilation { get; init; }

    /// <summary>
    /// The file path of the file being analyzed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The severity level to use for violations.
    /// </summary>
    public SeverityLevel Severity { get; init; } = SeverityLevel.Medium;

    /// <summary>
    /// Custom message override for violations.
    /// </summary>
    public string? CustomMessage { get; init; }

    /// <summary>
    /// Fix hint for violations.
    /// </summary>
    public string? FixHint { get; init; }

    /// <summary>
    /// Cancellation token for the analysis.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// The programming language of the source file.
    /// </summary>
    public Language Language { get; init; } = Language.CSharp;
}

/// <summary>
/// Helper methods for analyzing code in custom rules.
/// </summary>
public static class AnalyzerHelpers
{
    /// <summary>
    /// Gets all class declarations from the syntax tree.
    /// </summary>
    public static IEnumerable<ClassDeclarationSyntax> GetClasses(this AnalysisContext context)
    {
        return context.SyntaxTree.GetRoot(context.CancellationToken)
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();
    }

    /// <summary>
    /// Gets all method declarations from the syntax tree.
    /// </summary>
    public static IEnumerable<MethodDeclarationSyntax> GetMethods(this AnalysisContext context)
    {
        return context.SyntaxTree.GetRoot(context.CancellationToken)
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();
    }

    /// <summary>
    /// Gets all interface declarations from the syntax tree.
    /// </summary>
    public static IEnumerable<InterfaceDeclarationSyntax> GetInterfaces(this AnalysisContext context)
    {
        return context.SyntaxTree.GetRoot(context.CancellationToken)
            .DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>();
    }

    /// <summary>
    /// Gets all property declarations from the syntax tree.
    /// </summary>
    public static IEnumerable<PropertyDeclarationSyntax> GetProperties(this AnalysisContext context)
    {
        return context.SyntaxTree.GetRoot(context.CancellationToken)
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>();
    }

    /// <summary>
    /// Gets all field declarations from the syntax tree.
    /// </summary>
    public static IEnumerable<FieldDeclarationSyntax> GetFields(this AnalysisContext context)
    {
        return context.SyntaxTree.GetRoot(context.CancellationToken)
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>();
    }

    /// <summary>
    /// Gets all invocation expressions from the syntax tree.
    /// </summary>
    public static IEnumerable<InvocationExpressionSyntax> GetInvocations(this AnalysisContext context)
    {
        return context.SyntaxTree.GetRoot(context.CancellationToken)
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();
    }

    /// <summary>
    /// Gets the symbol for a syntax node using the semantic model.
    /// </summary>
    public static ISymbol? GetSymbol(this AnalysisContext context, SyntaxNode node)
    {
        if (context.SemanticModel == null)
            return null;

        return context.SemanticModel.GetSymbolInfo(node).Symbol;
    }

    /// <summary>
    /// Gets the type symbol for a syntax node using the semantic model.
    /// </summary>
    public static ITypeSymbol? GetTypeSymbol(this AnalysisContext context, SyntaxNode node)
    {
        if (context.SemanticModel == null)
            return null;

        return context.SemanticModel.GetTypeInfo(node).Type;
    }

    /// <summary>
    /// Checks if a method has the async modifier.
    /// </summary>
    public static bool IsAsync(this MethodDeclarationSyntax method)
    {
        return method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
    }

    /// <summary>
    /// Checks if a member is public.
    /// </summary>
    public static bool IsPublic(this MemberDeclarationSyntax member)
    {
        return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
    }

    /// <summary>
    /// Checks if a member is private.
    /// </summary>
    public static bool IsPrivate(this MemberDeclarationSyntax member)
    {
        return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
               (!member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                !member.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) &&
                !member.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)));
    }

    /// <summary>
    /// Checks if a member is static.
    /// </summary>
    public static bool IsStatic(this MemberDeclarationSyntax member)
    {
        return member.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
    }

    /// <summary>
    /// Gets the method name from an invocation expression.
    /// </summary>
    public static string? GetMethodName(this InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    /// Gets the full method name (including type) from an invocation expression.
    /// </summary>
    public static string? GetFullMethodName(this InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess =>
                $"{memberAccess.Expression}.{memberAccess.Name.Identifier.Text}",
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    /// Checks if a type inherits from or implements a specific type.
    /// </summary>
    public static bool InheritsFrom(this INamedTypeSymbol type, string baseTypeName)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.Name == baseTypeName || current.ToString() == baseTypeName)
                return true;
            current = current.BaseType;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == baseTypeName || iface.ToString() == baseTypeName)
                return true;
        }

        return false;
    }
}
