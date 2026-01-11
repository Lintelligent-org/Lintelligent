# Data Model: Option Monad Analyzer

**Feature**: `001-option-monad-analyzer`  
**Date**: January 11, 2026  
**Purpose**: Define data structures used by Core analyzer and code fix implementations

---

## Core Analyzer Data Structures

### DiagnosticResult

**Purpose**: Represents a detected nullable return type that should use Option<T>

```csharp
namespace Lintelligent.Core.Analyzers;

/// <summary>
/// Diagnostic result for methods returning nullable types that should use Option&lt;T&gt;
/// </summary>
public class PreferOptionMonadDiagnosticResult : DiagnosticResult
{
    /// <summary>
    /// The name of the method with nullable return type
    /// </summary>
    public string MethodName { get; set; }
    
    /// <summary>
    /// The current return type (e.g., "string?", "int?", "Task&lt;string?&gt;")
    /// </summary>
    public string CurrentReturnType { get; set; }
    
    /// <summary>
    /// The suggested Option-based return type (e.g., "Option&lt;string&gt;", "Task&lt;Option&lt;string&gt;&gt;")
    /// </summary>
    public string SuggestedReturnType { get; set; }
    
    /// <summary>
    /// True if the nullable type is a value type (int?, bool?), false for reference types (string?)
    /// </summary>
    public bool IsValueType { get; set; }
    
    /// <summary>
    /// True if the return type is wrapped in Task&lt;T?&gt; or ValueTask&lt;T?&gt;
    /// </summary>
    public bool IsAsyncWrapped { get; set; }
    
    /// <summary>
    /// The text span of the return type in the source code
    /// </summary>
    public TextSpan ReturnTypeSpan { get; set; }
    
    /// <summary>
    /// The inner type (T in T?)
    /// </summary>
    public string InnerType { get; set; }
}
```

---

## Core Code Fix Data Structures

### CodeFixResult

**Purpose**: Represents the transformation needed to convert nullable returns to Option<T>

```csharp
namespace Lintelligent.Core.CodeFixes;

/// <summary>
/// Code fix result for converting nullable returns to Option&lt;T&gt;
/// </summary>
public class PreferOptionMonadCodeFixResult : CodeFixResult
{
    /// <summary>
    /// The updated method declaration with Option&lt;T&gt; return type
    /// </summary>
    public SyntaxNode UpdatedMethodDeclaration { get; set; }
    
    /// <summary>
    /// List of transformations for each return statement in the method
    /// </summary>
    public List<ReturnStatementTransformation> ReturnTransformations { get; set; }
    
    /// <summary>
    /// True if the using LanguageExt; directive needs to be added
    /// </summary>
    public bool RequiresUsingDirective { get; set; }
    
    /// <summary>
    /// The namespace to import (default: "LanguageExt")
    /// </summary>
    public string UsingNamespace { get; set; } = "LanguageExt";
    
    /// <summary>
    /// True if the method is async and returns Task&lt;Option&lt;T&gt;&gt;
    /// </summary>
    public bool IsAsyncMethod { get; set; }
}

/// <summary>
/// Represents the transformation of a single return statement
/// </summary>
public class ReturnStatementTransformation
{
    /// <summary>
    /// The original return statement syntax node
    /// </summary>
    public SyntaxNode OriginalReturn { get; set; }
    
    /// <summary>
    /// The transformed return statement (with Option.Some or Option.None)
    /// </summary>
    public SyntaxNode TransformedReturn { get; set; }
    
    /// <summary>
    /// True if the return statement returns null (becomes Option.None)
    /// </summary>
    public bool IsNullReturn { get; set; }
    
    /// <summary>
    /// True if the return statement returns a non-null value (becomes Option.Some)
    /// </summary>
    public bool IsSomeReturn => !IsNullReturn;
    
    /// <summary>
    /// The text span of the return statement
    /// </summary>
    public TextSpan Span { get; set; }
}
```

---

## Roslyn Adapter Entities

### DiagnosticDescriptor

**Purpose**: Roslyn diagnostic metadata for LINT003

```csharp
namespace Lintelligent.Analyzers.Basic;

public static class PreferOptionMonadDiagnosticDescriptor
{
    public const string DiagnosticId = "LINT003";
    public const string Title = "Prefer Option<T> over nullable types";
    public const string MessageFormat = "Method '{0}' returns nullable type '{1}'. Consider using '{2}' to make absence of value explicit.";
    public const string Description = "Using Option<T> from language-ext makes the possibility of absence explicit in the type system, reducing null reference exceptions.";
    public const string Category = "Design";
    
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/Lintelligent-org/Lintelligent/blob/main/docs/analyzers/LINT003.md"
    );
}
```

### AnalyzerContext

**Purpose**: Contextual data passed to Core analyzer

```csharp
namespace Lintelligent.Analyzers.Basic;

/// <summary>
/// Context data for Option Monad analyzer
/// </summary>
internal class PreferOptionMonadAnalyzerContext
{
    /// <summary>
    /// The method declaration syntax node being analyzed
    /// </summary>
    public MethodDeclarationSyntax MethodDeclaration { get; set; }
    
    /// <summary>
    /// Semantic model for type resolution
    /// </summary>
    public SemanticModel SemanticModel { get; set; }
    
    /// <summary>
    /// The syntax tree being analyzed
    /// </summary>
    public SyntaxTree SyntaxTree { get; set; }
    
    /// <summary>
    /// Compilation for type symbol resolution
    /// </summary>
    public Compilation Compilation { get; set; }
}
```

---

## Helper Types

### NullableTypeInfo

**Purpose**: Encapsulates information about a nullable type

```csharp
namespace Lintelligent.Core.Utilities;

/// <summary>
/// Information about a nullable type detected by the analyzer
/// </summary>
public class NullableTypeInfo
{
    /// <summary>
    /// The full type symbol (e.g., string?, Nullable<int>)
    /// </summary>
    public ITypeSymbol TypeSymbol { get; set; }
    
    /// <summary>
    /// The inner type (e.g., string in string?, int in int?)
    /// </summary>
    public ITypeSymbol InnerType { get; set; }
    
    /// <summary>
    /// True if this is a nullable value type (int?, bool?)
    /// </summary>
    public bool IsNullableValueType { get; set; }
    
    /// <summary>
    /// True if this is a nullable reference type (string? in C# 8.0+)
    /// </summary>
    public bool IsNullableReferenceType { get; set; }
    
    /// <summary>
    /// True if the type is wrapped in Task or ValueTask
    /// </summary>
    public bool IsWrappedInTask { get; set; }
    
    /// <summary>
    /// The wrapper type if wrapped (Task, ValueTask)
    /// </summary>
    public INamedTypeSymbol WrapperType { get; set; }
    
    /// <summary>
    /// Creates an Option<T> type representation
    /// </summary>
    public string ToOptionTypeName()
    {
        var optionType = $"Option<{InnerType.ToDisplayString()}>";
        
        if (IsWrappedInTask)
        {
            return $"{WrapperType.Name}<{optionType}>";
        }
        
        return optionType;
    }
}
```

---

## State Diagram

```
Method Declaration
       ↓
   [Analyzer]
       ↓
   Check return type
       ↓
   ┌─────────────────────┐
   │  Is nullable?       │
   │  (T? or Nullable<T>)│
   └─────────────────────┘
       ↓ Yes              ↓ No
   Create Diagnostic    Skip
       ↓
   [Code Fix Triggered]
       ↓
   Transform return type
   (T? → Option<T>)
       ↓
   Find all return statements
       ↓
   ┌──────────────────────┐
   │ For each return:     │
   │ - null → None        │
   │ - value → Some(value)│
   └──────────────────────┘
       ↓
   Add using directive
   (if needed)
       ↓
   Apply changes to document
       ↓
   [Complete]
```

---

## Entity Relationships

```
PreferOptionMonadDiagnosticResult
    ↓ (used by)
PreferOptionMonadRoslynAdapter
    ↓ (creates)
Diagnostic (Roslyn type)
    ↓ (triggers)
PreferOptionMonadCodeFixProvider
    ↓ (delegates to)
PreferOptionMonadCodeFix (Core)
    ↓ (returns)
PreferOptionMonadCodeFixResult
    ↓ (contains)
ReturnStatementTransformation[]
    ↓ (applied to)
Document (updated)
```

---

## Validation Rules

### DiagnosticResult Validation
- `MethodName` must not be null or empty
- `CurrentReturnType` must match pattern `T?` or `Nullable<T>`
- `SuggestedReturnType` must be valid Option<T> format
- `ReturnTypeSpan` must be valid and within method declaration

### CodeFixResult Validation
- `UpdatedMethodDeclaration` must have Option<T> return type
- `ReturnTransformations` must cover all return statements in method
- Each transformation must have valid OriginalReturn and TransformedReturn nodes
- If `RequiresUsingDirective` is true, `UsingNamespace` must not be empty

### NullableTypeInfo Validation
- Either `IsNullableValueType` or `IsNullableReferenceType` must be true (not both)
- `InnerType` must not be null
- If `IsWrappedInTask` is true, `WrapperType` must not be null

---

## Data Flow Example

**Input**: Method with nullable return
```csharp
public string? GetName(int userId)
{
    if (userId == 0) return null;
    return "Alice";
}
```

**Analyzer Output (DiagnosticResult)**:
```csharp
{
    MethodName = "GetName",
    CurrentReturnType = "string?",
    SuggestedReturnType = "Option<string>",
    IsValueType = false,
    IsAsyncWrapped = false,
    InnerType = "string",
    ReturnTypeSpan = TextSpan(7, 7) // span of "string?"
}
```

**Code Fix Output (CodeFixResult)**:
```csharp
{
    UpdatedMethodDeclaration = /* SyntaxNode with Option<string> return */,
    RequiresUsingDirective = true,
    UsingNamespace = "LanguageExt",
    IsAsyncMethod = false,
    ReturnTransformations = [
        {
            OriginalReturn = /* return null; */,
            TransformedReturn = /* return Option<string>.None; */,
            IsNullReturn = true,
            Span = TextSpan(45, 12)
        },
        {
            OriginalReturn = /* return "Alice"; */,
            TransformedReturn = /* return Option.Some("Alice"); */,
            IsNullReturn = false,
            Span = TextSpan(70, 15)
        }
    ]
}
```

**Final Output**:
```csharp
using LanguageExt;

public Option<string> GetName(int userId)
{
    if (userId == 0) return Option<string>.None;
    return Option.Some("Alice");
}
```

---

## Performance Considerations

### Memory Efficiency
- Reuse `NullableTypeInfo` instances where possible
- Use `struct` for small, immutable data (consider for ReturnStatementTransformation if needed)
- Avoid creating intermediate collections during transformation

### Processing Efficiency
- Cache semantic model lookups
- Use batch replacement (`SyntaxNode.ReplaceNodes`) instead of sequential replacements
- Limit tree traversal to method scope only

### Scalability
- Expected memory per diagnostic: ~500 bytes (DiagnosticResult + context)
- Expected transformations per method: 1-10 return statements (average 2-3)
- No unbounded collections or recursive structures

---

**Status**: ✅ DATA MODEL COMPLETE
