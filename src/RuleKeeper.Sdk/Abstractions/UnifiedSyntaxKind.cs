namespace RuleKeeper.Sdk.Abstractions;

/// <summary>
/// Defines common syntax node kinds that are applicable across multiple programming languages.
/// This provides a language-agnostic way to identify syntax constructs.
/// </summary>
public enum UnifiedSyntaxKind
{
    /// <summary>
    /// Unknown or unrecognized syntax kind.
    /// </summary>
    Unknown = 0,

    // ========== Compilation Units ==========

    /// <summary>
    /// The root node of a compilation unit (source file).
    /// </summary>
    CompilationUnit = 1,

    /// <summary>
    /// A module or namespace declaration.
    /// </summary>
    Module = 2,

    /// <summary>
    /// A package or namespace declaration.
    /// </summary>
    Package = 3,

    // ========== Type Declarations ==========

    /// <summary>
    /// A class declaration.
    /// </summary>
    ClassDeclaration = 100,

    /// <summary>
    /// An interface declaration.
    /// </summary>
    InterfaceDeclaration = 101,

    /// <summary>
    /// A struct or value type declaration.
    /// </summary>
    StructDeclaration = 102,

    /// <summary>
    /// An enum declaration.
    /// </summary>
    EnumDeclaration = 103,

    /// <summary>
    /// A record declaration (C#, Java).
    /// </summary>
    RecordDeclaration = 104,

    /// <summary>
    /// A trait or protocol declaration.
    /// </summary>
    TraitDeclaration = 105,

    /// <summary>
    /// A type alias or typedef.
    /// </summary>
    TypeAlias = 106,

    // ========== Member Declarations ==========

    /// <summary>
    /// A method or function declaration.
    /// </summary>
    MethodDeclaration = 200,

    /// <summary>
    /// A constructor declaration.
    /// </summary>
    ConstructorDeclaration = 201,

    /// <summary>
    /// A destructor or finalizer declaration.
    /// </summary>
    DestructorDeclaration = 202,

    /// <summary>
    /// A property declaration.
    /// </summary>
    PropertyDeclaration = 203,

    /// <summary>
    /// A field declaration.
    /// </summary>
    FieldDeclaration = 204,

    /// <summary>
    /// An event declaration.
    /// </summary>
    EventDeclaration = 205,

    /// <summary>
    /// An indexer declaration.
    /// </summary>
    IndexerDeclaration = 206,

    /// <summary>
    /// An operator overload declaration.
    /// </summary>
    OperatorDeclaration = 207,

    /// <summary>
    /// An enum member declaration.
    /// </summary>
    EnumMemberDeclaration = 208,

    // ========== Parameters and Arguments ==========

    /// <summary>
    /// A parameter declaration.
    /// </summary>
    Parameter = 300,

    /// <summary>
    /// A type parameter (generic).
    /// </summary>
    TypeParameter = 301,

    /// <summary>
    /// An argument passed to a function/method.
    /// </summary>
    Argument = 302,

    /// <summary>
    /// A parameter list.
    /// </summary>
    ParameterList = 303,

    /// <summary>
    /// An argument list.
    /// </summary>
    ArgumentList = 304,

    // ========== Statements ==========

    /// <summary>
    /// A block of statements.
    /// </summary>
    Block = 400,

    /// <summary>
    /// A local variable declaration statement.
    /// </summary>
    LocalDeclaration = 401,

    /// <summary>
    /// An expression statement.
    /// </summary>
    ExpressionStatement = 402,

    /// <summary>
    /// A return statement.
    /// </summary>
    ReturnStatement = 403,

    /// <summary>
    /// An if statement.
    /// </summary>
    IfStatement = 404,

    /// <summary>
    /// An else clause.
    /// </summary>
    ElseClause = 405,

    /// <summary>
    /// A switch statement.
    /// </summary>
    SwitchStatement = 406,

    /// <summary>
    /// A case clause in a switch.
    /// </summary>
    SwitchCase = 407,

    /// <summary>
    /// A while loop statement.
    /// </summary>
    WhileStatement = 408,

    /// <summary>
    /// A do-while loop statement.
    /// </summary>
    DoWhileStatement = 409,

    /// <summary>
    /// A for loop statement.
    /// </summary>
    ForStatement = 410,

    /// <summary>
    /// A foreach/for-in loop statement.
    /// </summary>
    ForEachStatement = 411,

    /// <summary>
    /// A try statement.
    /// </summary>
    TryStatement = 412,

    /// <summary>
    /// A catch clause.
    /// </summary>
    CatchClause = 413,

    /// <summary>
    /// A finally clause.
    /// </summary>
    FinallyClause = 414,

    /// <summary>
    /// A throw statement.
    /// </summary>
    ThrowStatement = 415,

    /// <summary>
    /// A break statement.
    /// </summary>
    BreakStatement = 416,

    /// <summary>
    /// A continue statement.
    /// </summary>
    ContinueStatement = 417,

    /// <summary>
    /// A using/with statement for resource management.
    /// </summary>
    UsingStatement = 418,

    /// <summary>
    /// A lock/synchronized statement.
    /// </summary>
    LockStatement = 419,

    /// <summary>
    /// A yield return statement.
    /// </summary>
    YieldStatement = 420,

    /// <summary>
    /// An await statement.
    /// </summary>
    AwaitStatement = 421,

    /// <summary>
    /// An assert statement.
    /// </summary>
    AssertStatement = 422,

    /// <summary>
    /// A pass/empty statement.
    /// </summary>
    EmptyStatement = 423,

    /// <summary>
    /// A defer/cleanup statement (Go).
    /// </summary>
    DeferStatement = 424,

    /// <summary>
    /// A goto statement.
    /// </summary>
    GotoStatement = 425,

    /// <summary>
    /// A labeled statement.
    /// </summary>
    LabeledStatement = 426,

    // ========== Expressions ==========

    /// <summary>
    /// A literal expression (string, number, etc.).
    /// </summary>
    LiteralExpression = 500,

    /// <summary>
    /// An identifier/name reference.
    /// </summary>
    Identifier = 501,

    /// <summary>
    /// A binary expression (a + b, a == b, etc.).
    /// </summary>
    BinaryExpression = 502,

    /// <summary>
    /// A unary expression (!a, -a, etc.).
    /// </summary>
    UnaryExpression = 503,

    /// <summary>
    /// A method/function invocation.
    /// </summary>
    InvocationExpression = 504,

    /// <summary>
    /// A member access expression (a.b).
    /// </summary>
    MemberAccessExpression = 505,

    /// <summary>
    /// An element access expression (a[b]).
    /// </summary>
    ElementAccessExpression = 506,

    /// <summary>
    /// An object creation expression (new T()).
    /// </summary>
    ObjectCreationExpression = 507,

    /// <summary>
    /// An array creation expression.
    /// </summary>
    ArrayCreationExpression = 508,

    /// <summary>
    /// An assignment expression.
    /// </summary>
    AssignmentExpression = 509,

    /// <summary>
    /// A conditional/ternary expression (a ? b : c).
    /// </summary>
    ConditionalExpression = 510,

    /// <summary>
    /// A lambda/arrow function expression.
    /// </summary>
    LambdaExpression = 511,

    /// <summary>
    /// An anonymous function/delegate expression.
    /// </summary>
    AnonymousFunction = 512,

    /// <summary>
    /// A cast expression.
    /// </summary>
    CastExpression = 513,

    /// <summary>
    /// A type check expression (is, instanceof).
    /// </summary>
    TypeCheckExpression = 514,

    /// <summary>
    /// A null coalescing expression (??).
    /// </summary>
    CoalesceExpression = 515,

    /// <summary>
    /// An await expression.
    /// </summary>
    AwaitExpression = 516,

    /// <summary>
    /// A parenthesized expression.
    /// </summary>
    ParenthesizedExpression = 517,

    /// <summary>
    /// An interpolated string expression.
    /// </summary>
    InterpolatedString = 518,

    /// <summary>
    /// A tuple expression.
    /// </summary>
    TupleExpression = 519,

    /// <summary>
    /// A range expression.
    /// </summary>
    RangeExpression = 520,

    /// <summary>
    /// A pattern matching expression.
    /// </summary>
    PatternExpression = 521,

    /// <summary>
    /// A LINQ query expression.
    /// </summary>
    QueryExpression = 522,

    /// <summary>
    /// A list/array comprehension.
    /// </summary>
    ComprehensionExpression = 523,

    /// <summary>
    /// A dictionary/map comprehension.
    /// </summary>
    DictionaryComprehension = 524,

    /// <summary>
    /// A this/self reference.
    /// </summary>
    ThisExpression = 525,

    /// <summary>
    /// A base/super reference.
    /// </summary>
    BaseExpression = 526,

    // ========== Type Syntax ==========

    /// <summary>
    /// A simple type name.
    /// </summary>
    TypeName = 600,

    /// <summary>
    /// A generic type name.
    /// </summary>
    GenericType = 601,

    /// <summary>
    /// An array type.
    /// </summary>
    ArrayType = 602,

    /// <summary>
    /// A nullable type.
    /// </summary>
    NullableType = 603,

    /// <summary>
    /// A tuple type.
    /// </summary>
    TupleType = 604,

    /// <summary>
    /// A function/delegate type.
    /// </summary>
    FunctionType = 605,

    /// <summary>
    /// A pointer type.
    /// </summary>
    PointerType = 606,

    /// <summary>
    /// A reference type.
    /// </summary>
    ReferenceType = 607,

    // ========== Imports and Exports ==========

    /// <summary>
    /// An import/using directive.
    /// </summary>
    ImportDeclaration = 700,

    /// <summary>
    /// An export declaration.
    /// </summary>
    ExportDeclaration = 701,

    /// <summary>
    /// An extern/native declaration.
    /// </summary>
    ExternDeclaration = 702,

    // ========== Attributes and Decorators ==========

    /// <summary>
    /// An attribute or annotation.
    /// </summary>
    Attribute = 800,

    /// <summary>
    /// A decorator (Python, TypeScript).
    /// </summary>
    Decorator = 801,

    /// <summary>
    /// An attribute argument.
    /// </summary>
    AttributeArgument = 802,

    // ========== Comments and Documentation ==========

    /// <summary>
    /// A single-line comment.
    /// </summary>
    SingleLineComment = 900,

    /// <summary>
    /// A multi-line comment.
    /// </summary>
    MultiLineComment = 901,

    /// <summary>
    /// A documentation comment (///, /**, etc.).
    /// </summary>
    DocumentationComment = 902,

    // ========== Miscellaneous ==========

    /// <summary>
    /// A preprocessor directive.
    /// </summary>
    PreprocessorDirective = 1000,

    /// <summary>
    /// Trivia (whitespace, comments not attached to nodes).
    /// </summary>
    Trivia = 1001,

    /// <summary>
    /// An error node for malformed syntax.
    /// </summary>
    Error = 1002
}

/// <summary>
/// Extension methods for <see cref="UnifiedSyntaxKind"/>.
/// </summary>
public static class UnifiedSyntaxKindExtensions
{
    /// <summary>
    /// Determines if the kind represents a type declaration.
    /// </summary>
    public static bool IsTypeDeclaration(this UnifiedSyntaxKind kind) =>
        kind is >= UnifiedSyntaxKind.ClassDeclaration and <= UnifiedSyntaxKind.TypeAlias;

    /// <summary>
    /// Determines if the kind represents a member declaration.
    /// </summary>
    public static bool IsMemberDeclaration(this UnifiedSyntaxKind kind) =>
        kind is >= UnifiedSyntaxKind.MethodDeclaration and <= UnifiedSyntaxKind.EnumMemberDeclaration;

    /// <summary>
    /// Determines if the kind represents a statement.
    /// </summary>
    public static bool IsStatement(this UnifiedSyntaxKind kind) =>
        kind is >= UnifiedSyntaxKind.Block and <= UnifiedSyntaxKind.LabeledStatement;

    /// <summary>
    /// Determines if the kind represents an expression.
    /// </summary>
    public static bool IsExpression(this UnifiedSyntaxKind kind) =>
        kind is >= UnifiedSyntaxKind.LiteralExpression and <= UnifiedSyntaxKind.BaseExpression;

    /// <summary>
    /// Determines if the kind represents a loop statement.
    /// </summary>
    public static bool IsLoop(this UnifiedSyntaxKind kind) =>
        kind is UnifiedSyntaxKind.WhileStatement or UnifiedSyntaxKind.DoWhileStatement or
               UnifiedSyntaxKind.ForStatement or UnifiedSyntaxKind.ForEachStatement;

    /// <summary>
    /// Determines if the kind represents a branching statement.
    /// </summary>
    public static bool IsBranching(this UnifiedSyntaxKind kind) =>
        kind is UnifiedSyntaxKind.IfStatement or UnifiedSyntaxKind.SwitchStatement or
               UnifiedSyntaxKind.ConditionalExpression;
}
