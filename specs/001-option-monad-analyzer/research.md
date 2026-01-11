# Research: Option Monad Analyzer

**Feature**: `001-option-monad-analyzer`  
**Date**: January 11, 2026  
**Status**: Complete  

## RT-001: language-ext Option<T> Patterns

### Key Findings

**Namespace**: `LanguageExt`

**Type Signature**:
```csharp
public readonly struct Option<A>
{
    // Primary constructors
    public static Option<A> None { get; }
    public static Option<A> Some(A value) { }
    
    // Extension methods
    public static Option<A> ToSome<A>(this A value);
    public static Option<A> ToOption<A>(this A? value);
}
```

**Creating Option Values**:
1. **Some**: `Option.Some(value)` or `value.ToSome()` or `Option<T>.Some(value)`
2. **None**: `Option<T>.None` or `Option.None<T>()`

**Consuming Option Values**:
1. **Match**: `option.Match(Some: val => ..., None: () => ...)`
2. **Map**: `option.Map(x => x.ToUpper())`
3. **Bind**: `option.Bind(x => GetAnother(x))`
4. **IfNone**: `option.IfNone("default")`
5. **IfSome**: `option.IfSome(val => Console.WriteLine(val))`

**Required Using**:
```csharp
using LanguageExt;
using static LanguageExt.Prelude; // Optional, for helper methods
```

**Best Practices**:
- Prefer `Option.Some(value)` over `new Option<T>(value)` for clarity
- Use `Option<T>.None` for type inference clarity
- Extension method `.ToSome()` is idiomatic for simple conversions
- Always use pattern matching (Match) for safe consumption

---

## RT-002: Roslyn Nullable Type Detection

### Detection Strategy

**For Nullable Reference Types (C# 8.0+)**:
```csharp
var typeSymbol = semanticModel.GetTypeInfo(returnTypeSyntax).Type;
if (typeSymbol?.NullableAnnotation == NullableAnnotation.Annotated)
{
    // This is a nullable reference type (string?)
    var innerType = typeSymbol; // string
}
```

**For Nullable Value Types**:
```csharp
var typeSymbol = semanticModel.GetTypeInfo(returnTypeSyntax).Type;
if (typeSymbol is INamedTypeSymbol namedType && 
    namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
{
    // This is Nullable<T> (int?)
    var innerType = namedType.TypeArguments[0]; // int
}
```

**Method Return Type Location**:
```csharp
// For method declarations
var methodDeclaration = node as MethodDeclarationSyntax;
var returnType = methodDeclaration?.ReturnType;

// For expression-bodied members
var arrowExpression = node as ArrowExpressionClauseSyntax;
// Infer from parent method
```

**Generic Method Handling**:
```csharp
// Check if return type is a type parameter
if (typeSymbol is ITypeParameterSymbol typeParam)
{
    // Check constraints for nullable reference types
    if (typeParam.HasNotNullConstraint == false && 
        typeParam.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
    {
        // T? where T : class
    }
}
```

**Wrapped Nullable Types (Task<T?>)**:
```csharp
if (typeSymbol is INamedTypeSymbol namedType)
{
    if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
    {
        var wrappedType = namedType.TypeArguments[0];
        // Check if wrappedType is nullable
    }
}
```

**SyntaxKind for Registration**:
- `SyntaxKind.MethodDeclaration` for regular methods
- `SyntaxKind.LocalFunctionStatement` for local functions (optional)
- `SyntaxKind.SimpleLambdaExpression` / `SyntaxKind.ParenthesizedLambdaExpression` (P2 - out of scope)

---

## RT-003: Code Fix Transformation Patterns

### Transformation Strategy

**Step 1: Replace Method Return Type**
```csharp
var originalMethod = methodDeclaration;
var originalReturnType = originalMethod.ReturnType;

// Create new return type: Option<T>
var optionType = SyntaxFactory.GenericName(
    SyntaxFactory.Identifier("Option"),
    SyntaxFactory.TypeArgumentList(
        SyntaxFactory.SingletonSeparatedList(innerType)
    )
);

var updatedMethod = originalMethod.WithReturnType(optionType);
```

**Step 2: Transform Return Statements**
```csharp
// Find all return statements in method body
var returnStatements = methodDeclaration.DescendantNodes()
    .OfType<ReturnStatementSyntax>();

foreach (var returnStmt in returnStatements)
{
    if (returnStmt.Expression == null || 
        returnStmt.Expression.IsKind(SyntaxKind.NullLiteralExpression))
    {
        // return null; → return Option<T>.None;
        var noneExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.GenericName(/* Option<T> */),
            SyntaxFactory.IdentifierName("None")
        );
        newReturn = returnStmt.WithExpression(noneExpression);
    }
    else
    {
        // return value; → return Option.Some(value);
        var someInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Option"),
                SyntaxFactory.IdentifierName("Some")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(returnStmt.Expression)
                )
            )
        );
        newReturn = returnStmt.WithExpression(someInvocation);
    }
}
```

**Step 3: Add Using Directive**
```csharp
var root = await document.GetSyntaxRootAsync();
var compilationUnit = root as CompilationUnitSyntax;

// Check if using LanguageExt; already exists
if (!compilationUnit.Usings.Any(u => u.Name.ToString() == "LanguageExt"))
{
    var usingDirective = SyntaxFactory.UsingDirective(
        SyntaxFactory.IdentifierName("LanguageExt")
    );
    
    var newRoot = compilationUnit.AddUsings(usingDirective);
    document = document.WithSyntaxRoot(newRoot);
}
```

**Edge Cases**:
1. **Expression-bodied members**: `public string? Name => null;`
   - Transform arrow clause to block body or keep arrow with Option.None
2. **Multiple nested returns**: Use SyntaxRewriter to recursively replace
3. **Try-catch with returns**: Handle returns in try/catch/finally blocks
4. **Lambda returns**: Out of scope for P1 (focus on methods only)

**Performance Considerations**:
- Use `SyntaxNode.ReplaceNodes()` for batch replacements instead of multiple `WithX()` calls
- Cache semantic model lookups
- Limit transformation scope to method body only

---

## RT-004: Roslyn Testing Framework

### Testing Patterns

**Diagnostic Location Markup**:
```csharp
var test = """
    public class Test
    {
        public [|string?|] GetName() => null;
    }
    """;

await VerifyCS.VerifyAnalyzerAsync(test);
```

- `[| |]` marks where diagnostic should be reported
- Span must match exactly (start and end positions)
- Multiple diagnostics: Use multiple `[| |]` pairs

**Code Fix Testing**:
```csharp
var before = """
    public string? GetName() => null;
    """;

var after = """
    using LanguageExt;

    public Option<string> GetName() => Option<string>.None;
    """;

await VerifyCS.VerifyCodeFixAsync(before, after);
```

**Multiple Diagnostics**:
```csharp
var test = """
    public [|string?|] GetName() => null;
    public [|int?|] GetAge() => null;
    """;
```

**Test Compilation Options**:
```csharp
var test = new VerifyCS.Test
{
    TestCode = sourceCode,
    LanguageVersion = LanguageVersion.CSharp10, // Enable nullable reference types
    ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
    TestState =
    {
        AdditionalReferences =
        {
            MetadataReference.CreateFromFile(typeof(Option<>).Assembly.Location)
        }
    }
};
```

**Test Organization Best Practices**:
1. One test class per analyzer/code fix
2. Test methods named by scenario: `DetectsNullableReferenceType`, `DetectsNullableValueType`
3. Use `[Theory]` with `[InlineData]` for parameterized tests
4. Separate integration tests for Roslyn adapter from Core unit tests

**Example Test Structure**:
```csharp
[Fact]
public async Task DetectsNullableReferenceType()
{
    var test = """
        public [|string?|] GetName() => null;
        """;
    
    await VerifyCS.VerifyAnalyzerAsync(test);
}

[Fact]
public async Task IgnoresNonNullableTypes()
{
    var test = """
        public string GetName() => "Alice";
        """;
    
    await VerifyCS.VerifyAnalyzerAsync(test);
}

[Fact]
public async Task CodeFixTransformsNullableToOption()
{
    var before = """
        public string? GetName() => null;
        """;
    
    var after = """
        using LanguageExt;
        
        public Option<string> GetName() => Option<string>.None;
        """;
    
    await VerifyCS.VerifyCodeFixAsync(before, after);
}
```

---

## Decisions Made

### Decision 1: Diagnostic Severity
**Choice**: Default to `Info` severity  
**Rationale**: Option monad usage is a style preference, not a bug. Developers can escalate to Warning/Error via `.editorconfig` if desired.  
**Alternatives Considered**: Warning (rejected - too aggressive for first release)

### Decision 2: Option Creation Pattern
**Choice**: Use `Option.Some(value)` for code fixes  
**Rationale**: More readable than `new Option<T>(value)` and consistent with language-ext conventions  
**Alternatives Considered**: `value.ToSome()` (rejected - requires extension method import, less explicit)

### Decision 3: Null Return Transformation
**Choice**: Use `Option<T>.None` instead of `Option.None<T>()`  
**Rationale**: Type inference is clearer with generic type on the left  
**Alternatives Considered**: `Option.None<T>()` (equivalent, but less idiomatic)

### Decision 4: Scope for P1
**Choice**: Focus on method-level nullable return types only  
**Rationale**: Properties, indexers, and delegates add complexity without providing proportional value for MVP  
**Alternatives Considered**: Include properties (deferred to P2)

### Decision 5: Generic Method Support
**Choice**: Support `T?` in generic methods where `T : class`  
**Rationale**: Common pattern in repository/service layer methods  
**Alternatives Considered**: Defer to P2 (rejected - too common to exclude)

### Decision 6: Task<T?> Handling
**Choice**: Detect and suggest `Task<Option<T>>` for async methods  
**Rationale**: Async methods are prevalent in modern C# codebases  
**Alternatives Considered**: Defer to P2 (rejected - too important for async-first codebases)

---

## Blockers and Clarifications

### No Blockers Identified

All research questions have been answered with concrete solutions. No blocking issues found for implementation.

### Minor Clarifications
1. **language-ext version**: Recommend 4.4.0+ for stable Option API (documented in dependencies)
2. **C# version**: Require C# 8.0+ for nullable reference types (documented in constraints)
3. **Test coverage target**: 80% per constitution (confirmed)

---

## Research Completion Checklist

- [x] RT-001: language-ext Option<T> Patterns - COMPLETE
- [x] RT-002: Roslyn Nullable Type Detection - COMPLETE
- [x] RT-003: Code Fix Transformation Patterns - COMPLETE
- [x] RT-004: Roslyn Testing Framework - COMPLETE
- [x] All decisions documented with rationale
- [x] No blockers identified
- [x] Ready for Phase 1 design

**Status**: ✅ RESEARCH PHASE COMPLETE - Proceed to Phase 1
