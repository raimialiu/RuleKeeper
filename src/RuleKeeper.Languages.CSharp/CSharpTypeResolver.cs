using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;
using SdkSymbolKind = RuleKeeper.Sdk.Abstractions.SymbolKind;
using RoslynSymbolKind = Microsoft.CodeAnalysis.SymbolKind;

namespace RuleKeeper.Languages.CSharp;

/// <summary>
/// Type resolver for C# using Roslyn semantic analysis.
/// </summary>
public class CSharpTypeResolver : ITypeResolver
{
    private readonly CSharpCompilation _compilation;

    /// <summary>
    /// Gets the underlying Roslyn compilation.
    /// </summary>
    public CSharpCompilation Compilation => _compilation;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.
    /// </summary>
    /// <param name="compilation">The Roslyn compilation.</param>
    public CSharpTypeResolver(CSharpCompilation compilation)
    {
        _compilation = compilation;
    }

    /// <inheritdoc />
    public Language Language => Language.CSharp;

    /// <inheritdoc />
    public ITypeInfo? GetType(IUnifiedSyntaxNode node)
    {
        if (node is CSharpUnifiedNode csNode && csNode.RoslynNode is SyntaxNode syntaxNode)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxNode.SyntaxTree);
            var typeInfo = semanticModel.GetTypeInfo(syntaxNode);

            if (typeInfo.Type != null)
            {
                return new CSharpTypeInfo(typeInfo.Type);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public ISymbolInfo? GetSymbol(IUnifiedSyntaxNode node)
    {
        if (node is CSharpUnifiedNode csNode && csNode.RoslynNode is SyntaxNode syntaxNode)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxNode.SyntaxTree);
            var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);

            if (symbolInfo.Symbol != null)
            {
                return new CSharpSymbolInfo(symbolInfo.Symbol);
            }

            var declaredSymbol = semanticModel.GetDeclaredSymbol(syntaxNode);
            if (declaredSymbol != null)
            {
                return new CSharpSymbolInfo(declaredSymbol);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool InheritsFrom(ITypeInfo type, string baseTypeName)
    {
        if (type is not CSharpTypeInfo { Symbol: INamedTypeSymbol namedType }) return false;
        var current = namedType.BaseType;
        while (current != null)
        {
            if (current.Name == baseTypeName || current.ToString() == baseTypeName)
                return true;
            current = current.BaseType;
        }

        foreach (var iface in namedType.AllInterfaces)
        {
            if (iface.Name == baseTypeName || iface.ToString() == baseTypeName)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Type information wrapper for Roslyn type symbols.
/// </summary>
public class CSharpTypeInfo : ITypeInfo
{
    private readonly ITypeSymbol _symbol;

    /// <summary>
    /// Gets the underlying Roslyn type symbol.
    /// </summary>
    public ITypeSymbol Symbol => _symbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpTypeInfo"/> class.
    /// </summary>
    /// <param name="symbol">The Roslyn type symbol.</param>
    public CSharpTypeInfo(ITypeSymbol symbol)
    {
        _symbol = symbol;
    }

    /// <inheritdoc />
    public string Name => _symbol.Name;

    /// <inheritdoc />
    public string FullName => _symbol.ToDisplayString();

    /// <inheritdoc />
    public bool IsPrimitive => _symbol.SpecialType != SpecialType.None;

    /// <inheritdoc />
    public bool IsNullable => _symbol.NullableAnnotation == NullableAnnotation.Annotated ||
                              (_symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T });

    /// <inheritdoc />
    public bool IsArray => _symbol is IArrayTypeSymbol;

    /// <inheritdoc />
    public bool IsGeneric => _symbol is INamedTypeSymbol { IsGenericType: true };

    /// <inheritdoc />
    public ITypeInfo? ElementType
    {
        get
        {
            if (_symbol is IArrayTypeSymbol arrayType)
                return new CSharpTypeInfo(arrayType.ElementType);
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ITypeInfo> TypeArguments
    {
        get
        {
            if (_symbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                return namedType.TypeArguments
                    .Select(t => (ITypeInfo)new CSharpTypeInfo(t))
                    .ToList();
            }
            return Array.Empty<ITypeInfo>();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ITypeInfo> BaseTypes
    {
        get
        {
            var types = new List<ITypeInfo>();
            if (_symbol is INamedTypeSymbol namedType)
            {
                if (namedType.BaseType != null)
                    types.Add(new CSharpTypeInfo(namedType.BaseType));
                types.AddRange(namedType.Interfaces.Select(i => (ITypeInfo)new CSharpTypeInfo(i)));
            }
            return types;
        }
    }
}

/// <summary>
/// Symbol information wrapper for Roslyn symbols.
/// </summary>
public class CSharpSymbolInfo : ISymbolInfo
{
    private readonly ISymbol _symbol;

    /// <summary>
    /// Gets the underlying Roslyn symbol.
    /// </summary>
    public ISymbol Symbol => _symbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpSymbolInfo"/> class.
    /// </summary>
    /// <param name="symbol">The Roslyn symbol.</param>
    public CSharpSymbolInfo(ISymbol symbol)
    {
        _symbol = symbol;
    }

    /// <inheritdoc />
    public string Name => _symbol.Name;

    /// <inheritdoc />
    public SdkSymbolKind Kind => MapSymbolKind(_symbol.Kind);

    /// <inheritdoc />
    public ITypeInfo? Type
    {
        get
        {
            var typeSymbol = _symbol switch
            {
                ILocalSymbol local => local.Type,
                IParameterSymbol param => param.Type,
                IFieldSymbol field => field.Type,
                IPropertySymbol prop => prop.Type,
                IMethodSymbol method => method.ReturnType,
                IEventSymbol evt => evt.Type,
                _ => null
            };
            return typeSymbol != null ? new CSharpTypeInfo(typeSymbol) : null;
        }
    }

    /// <inheritdoc />
    public bool IsPublic => _symbol.DeclaredAccessibility == Accessibility.Public;

    /// <inheritdoc />
    public bool IsPrivate => _symbol.DeclaredAccessibility == Accessibility.Private;

    /// <inheritdoc />
    public bool IsProtected => _symbol.DeclaredAccessibility is Accessibility.Protected or
                                Accessibility.ProtectedOrInternal or Accessibility.ProtectedAndInternal;

    /// <inheritdoc />
    public bool IsStatic => _symbol.IsStatic;

    /// <inheritdoc />
    public bool IsAbstract => _symbol.IsAbstract;

    /// <inheritdoc />
    public bool IsVirtual => _symbol.IsVirtual;

    /// <inheritdoc />
    public bool IsAsync => _symbol is IMethodSymbol { IsAsync: true };

    /// <inheritdoc />
    public ISymbolInfo? ContainingType =>
        _symbol.ContainingType != null ? new CSharpSymbolInfo(_symbol.ContainingType) : null;

    /// <inheritdoc />
    public SourceLocation? DeclaringLocation
    {
        get
        {
            var location = _symbol.Locations.FirstOrDefault();
            if (location == null || !location.IsInSource)
                return null;

            var lineSpan = location.GetLineSpan();
            return new SourceLocation(
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                lineSpan.EndLinePosition.Line + 1,
                lineSpan.EndLinePosition.Character + 1,
                location.SourceSpan.Start,
                location.SourceSpan.End
            );
        }
    }

    private static SdkSymbolKind MapSymbolKind(RoslynSymbolKind kind)
    {
        return kind switch
        {
            RoslynSymbolKind.Namespace => SdkSymbolKind.Namespace,
            RoslynSymbolKind.NamedType => SdkSymbolKind.Type,
            RoslynSymbolKind.Method => SdkSymbolKind.Method,
            RoslynSymbolKind.Property => SdkSymbolKind.Property,
            RoslynSymbolKind.Field => SdkSymbolKind.Field,
            RoslynSymbolKind.Event => SdkSymbolKind.Event,
            RoslynSymbolKind.Local => SdkSymbolKind.Local,
            RoslynSymbolKind.Parameter => SdkSymbolKind.Parameter,
            RoslynSymbolKind.TypeParameter => SdkSymbolKind.TypeParameter,
            RoslynSymbolKind.Label => SdkSymbolKind.Label,
            RoslynSymbolKind.Alias => SdkSymbolKind.Alias,
            _ => SdkSymbolKind.Unknown
        };
    }
}
