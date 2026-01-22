using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuleKeeper.Sdk;
using RuleKeeper.Sdk.Abstractions;

namespace RuleKeeper.Languages.CSharp;

/// <summary>
/// Wraps a Roslyn SyntaxNode as a unified syntax node.
/// </summary>
public class CSharpUnifiedNode : UnifiedSyntaxNodeBase
{
    private readonly SyntaxNode _node;
    private readonly CSharpUnifiedNode? _parent;
    private readonly string _filePath;
    private SourceLocation? _location;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpUnifiedNode"/> class.
    /// </summary>
    /// <param name="node">The Roslyn syntax node.</param>
    /// <param name="parent">The parent unified node.</param>
    /// <param name="filePath">The file path.</param>
    public CSharpUnifiedNode(SyntaxNode node, CSharpUnifiedNode? parent, string filePath)
    {
        _node = node;
        _parent = parent;
        _filePath = filePath;
    }

    /// <inheritdoc />
    public override UnifiedSyntaxKind Kind => MapSyntaxKind(_node.Kind());

    /// <inheritdoc />
    public override SourceLocation Location
    {
        get
        {
            if (_location == null)
            {
                var lineSpan = _node.GetLocation().GetLineSpan();
                _location = new SourceLocation(
                    _filePath,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Character + 1,
                    _node.SpanStart,
                    _node.Span.End
                );
            }
            return _location.Value;
        }
    }

    /// <inheritdoc />
    public override string Text => _node.ToString();

    /// <inheritdoc />
    public override IUnifiedSyntaxNode? Parent => _parent;

    /// <inheritdoc />
    public override IEnumerable<IUnifiedSyntaxNode> Children
    {
        get
        {
            foreach (var child in _node.ChildNodes())
            {
                yield return new CSharpUnifiedNode(child, this, _filePath);
            }
        }
    }

    /// <inheritdoc />
    public override object? NativeNode => _node;

    /// <inheritdoc />
    public override Language Language => Language.CSharp;

    /// <inheritdoc />
    public override bool ContainsErrors => _node.ContainsDiagnostics;

    /// <summary>
    /// Gets the underlying Roslyn syntax node.
    /// </summary>
    public SyntaxNode RoslynNode => _node;

    /// <summary>
    /// Maps a C# syntax kind to a unified syntax kind.
    /// </summary>
    private static UnifiedSyntaxKind MapSyntaxKind(SyntaxKind kind)
    {
        return kind switch
        {
            // Compilation units
            SyntaxKind.CompilationUnit => UnifiedSyntaxKind.CompilationUnit,
            SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration => UnifiedSyntaxKind.Module,

            // Type declarations
            SyntaxKind.ClassDeclaration => UnifiedSyntaxKind.ClassDeclaration,
            SyntaxKind.InterfaceDeclaration => UnifiedSyntaxKind.InterfaceDeclaration,
            SyntaxKind.StructDeclaration => UnifiedSyntaxKind.StructDeclaration,
            SyntaxKind.EnumDeclaration => UnifiedSyntaxKind.EnumDeclaration,
            SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration => UnifiedSyntaxKind.RecordDeclaration,

            // Member declarations
            SyntaxKind.MethodDeclaration => UnifiedSyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration => UnifiedSyntaxKind.ConstructorDeclaration,
            SyntaxKind.DestructorDeclaration => UnifiedSyntaxKind.DestructorDeclaration,
            SyntaxKind.PropertyDeclaration => UnifiedSyntaxKind.PropertyDeclaration,
            SyntaxKind.FieldDeclaration => UnifiedSyntaxKind.FieldDeclaration,
            SyntaxKind.EventDeclaration or SyntaxKind.EventFieldDeclaration => UnifiedSyntaxKind.EventDeclaration,
            SyntaxKind.IndexerDeclaration => UnifiedSyntaxKind.IndexerDeclaration,
            SyntaxKind.OperatorDeclaration => UnifiedSyntaxKind.OperatorDeclaration,
            SyntaxKind.EnumMemberDeclaration => UnifiedSyntaxKind.EnumMemberDeclaration,

            // Parameters
            SyntaxKind.Parameter => UnifiedSyntaxKind.Parameter,
            SyntaxKind.TypeParameter => UnifiedSyntaxKind.TypeParameter,
            SyntaxKind.Argument => UnifiedSyntaxKind.Argument,
            SyntaxKind.ParameterList => UnifiedSyntaxKind.ParameterList,
            SyntaxKind.ArgumentList or SyntaxKind.BracketedArgumentList => UnifiedSyntaxKind.ArgumentList,

            // Statements
            SyntaxKind.Block => UnifiedSyntaxKind.Block,
            SyntaxKind.LocalDeclarationStatement => UnifiedSyntaxKind.LocalDeclaration,
            SyntaxKind.ExpressionStatement => UnifiedSyntaxKind.ExpressionStatement,
            SyntaxKind.ReturnStatement => UnifiedSyntaxKind.ReturnStatement,
            SyntaxKind.IfStatement => UnifiedSyntaxKind.IfStatement,
            SyntaxKind.ElseClause => UnifiedSyntaxKind.ElseClause,
            SyntaxKind.SwitchStatement => UnifiedSyntaxKind.SwitchStatement,
            SyntaxKind.SwitchSection => UnifiedSyntaxKind.SwitchCase,
            SyntaxKind.WhileStatement => UnifiedSyntaxKind.WhileStatement,
            SyntaxKind.DoStatement => UnifiedSyntaxKind.DoWhileStatement,
            SyntaxKind.ForStatement => UnifiedSyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement => UnifiedSyntaxKind.ForEachStatement,
            SyntaxKind.TryStatement => UnifiedSyntaxKind.TryStatement,
            SyntaxKind.CatchClause => UnifiedSyntaxKind.CatchClause,
            SyntaxKind.FinallyClause => UnifiedSyntaxKind.FinallyClause,
            SyntaxKind.ThrowStatement => UnifiedSyntaxKind.ThrowStatement,
            SyntaxKind.BreakStatement => UnifiedSyntaxKind.BreakStatement,
            SyntaxKind.ContinueStatement => UnifiedSyntaxKind.ContinueStatement,
            SyntaxKind.UsingStatement => UnifiedSyntaxKind.UsingStatement,
            SyntaxKind.LockStatement => UnifiedSyntaxKind.LockStatement,
            SyntaxKind.YieldReturnStatement or SyntaxKind.YieldBreakStatement => UnifiedSyntaxKind.YieldStatement,
            SyntaxKind.GotoStatement => UnifiedSyntaxKind.GotoStatement,
            SyntaxKind.LabeledStatement => UnifiedSyntaxKind.LabeledStatement,
            SyntaxKind.EmptyStatement => UnifiedSyntaxKind.EmptyStatement,

            // Expressions
            SyntaxKind.NumericLiteralExpression or SyntaxKind.StringLiteralExpression or
            SyntaxKind.CharacterLiteralExpression or SyntaxKind.TrueLiteralExpression or
            SyntaxKind.FalseLiteralExpression or SyntaxKind.NullLiteralExpression =>
                UnifiedSyntaxKind.LiteralExpression,
            SyntaxKind.IdentifierName => UnifiedSyntaxKind.Identifier,
            SyntaxKind.AddExpression or SyntaxKind.SubtractExpression or SyntaxKind.MultiplyExpression or
            SyntaxKind.DivideExpression or SyntaxKind.ModuloExpression or
            SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression or
            SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression or
            SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression or
            SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression or
            SyntaxKind.BitwiseAndExpression or SyntaxKind.BitwiseOrExpression or
            SyntaxKind.ExclusiveOrExpression =>
                UnifiedSyntaxKind.BinaryExpression,
            SyntaxKind.UnaryMinusExpression or SyntaxKind.UnaryPlusExpression or
            SyntaxKind.LogicalNotExpression or SyntaxKind.BitwiseNotExpression or
            SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression or
            SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression =>
                UnifiedSyntaxKind.UnaryExpression,
            SyntaxKind.InvocationExpression => UnifiedSyntaxKind.InvocationExpression,
            SyntaxKind.SimpleMemberAccessExpression or SyntaxKind.PointerMemberAccessExpression =>
                UnifiedSyntaxKind.MemberAccessExpression,
            SyntaxKind.ElementAccessExpression => UnifiedSyntaxKind.ElementAccessExpression,
            SyntaxKind.ObjectCreationExpression or SyntaxKind.ImplicitObjectCreationExpression =>
                UnifiedSyntaxKind.ObjectCreationExpression,
            SyntaxKind.ArrayCreationExpression or SyntaxKind.ImplicitArrayCreationExpression =>
                UnifiedSyntaxKind.ArrayCreationExpression,
            SyntaxKind.SimpleAssignmentExpression or SyntaxKind.AddAssignmentExpression or
            SyntaxKind.SubtractAssignmentExpression or SyntaxKind.MultiplyAssignmentExpression or
            SyntaxKind.DivideAssignmentExpression =>
                UnifiedSyntaxKind.AssignmentExpression,
            SyntaxKind.ConditionalExpression => UnifiedSyntaxKind.ConditionalExpression,
            SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression =>
                UnifiedSyntaxKind.LambdaExpression,
            SyntaxKind.AnonymousMethodExpression => UnifiedSyntaxKind.AnonymousFunction,
            SyntaxKind.CastExpression => UnifiedSyntaxKind.CastExpression,
            SyntaxKind.IsExpression or SyntaxKind.AsExpression => UnifiedSyntaxKind.TypeCheckExpression,
            SyntaxKind.CoalesceExpression => UnifiedSyntaxKind.CoalesceExpression,
            SyntaxKind.AwaitExpression => UnifiedSyntaxKind.AwaitExpression,
            SyntaxKind.ParenthesizedExpression => UnifiedSyntaxKind.ParenthesizedExpression,
            SyntaxKind.InterpolatedStringExpression => UnifiedSyntaxKind.InterpolatedString,
            SyntaxKind.TupleExpression => UnifiedSyntaxKind.TupleExpression,
            SyntaxKind.RangeExpression => UnifiedSyntaxKind.RangeExpression,
            SyntaxKind.ThisExpression => UnifiedSyntaxKind.ThisExpression,
            SyntaxKind.BaseExpression => UnifiedSyntaxKind.BaseExpression,
            SyntaxKind.QueryExpression => UnifiedSyntaxKind.QueryExpression,

            // Types
            SyntaxKind.PredefinedType or SyntaxKind.QualifiedName => UnifiedSyntaxKind.TypeName,
            SyntaxKind.GenericName => UnifiedSyntaxKind.GenericType,
            SyntaxKind.ArrayType => UnifiedSyntaxKind.ArrayType,
            SyntaxKind.NullableType => UnifiedSyntaxKind.NullableType,
            SyntaxKind.TupleType => UnifiedSyntaxKind.TupleType,
            SyntaxKind.PointerType => UnifiedSyntaxKind.PointerType,
            SyntaxKind.RefType => UnifiedSyntaxKind.ReferenceType,

            // Imports
            SyntaxKind.UsingDirective => UnifiedSyntaxKind.ImportDeclaration,
            SyntaxKind.ExternAliasDirective => UnifiedSyntaxKind.ExternDeclaration,

            // Attributes
            SyntaxKind.AttributeList or SyntaxKind.Attribute => UnifiedSyntaxKind.Attribute,
            SyntaxKind.AttributeArgument => UnifiedSyntaxKind.AttributeArgument,

            // Comments
            SyntaxKind.SingleLineCommentTrivia => UnifiedSyntaxKind.SingleLineComment,
            SyntaxKind.MultiLineCommentTrivia => UnifiedSyntaxKind.MultiLineComment,
            SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia =>
                UnifiedSyntaxKind.DocumentationComment,

            // Preprocessor
            SyntaxKind.IfDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia or
            SyntaxKind.DefineDirectiveTrivia or SyntaxKind.RegionDirectiveTrivia =>
                UnifiedSyntaxKind.PreprocessorDirective,

            _ => UnifiedSyntaxKind.Unknown
        };
    }
}
