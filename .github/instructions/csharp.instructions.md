---
description: 'C# development guidelines for Lintelligent Roslyn analyzer framework'
applyTo: '**/*.cs'
---

# C# Development for Lintelligent

## Project Context

Lintelligent is a **layered Roslyn analyzer framework** with strict target framework constraints:
- **Lintelligent.Core**: netstandard2.0 (C# 7.3 max) - Framework-agnostic analyzer logic
- **Adapter/CodeFix projects**: netstandard2.0 (C# 7.3 max) - Roslyn integration
- **Test projects**: net8.0 (modern C# features allowed)
- **Future**: CLI tooling and other expansions planned

## C# Language Feature Constraints

### For netstandard2.0 Projects (Core, Analyzers, CodeFixes)
**Maximum C# Version: 7.3**

**Allowed Features:**
- Expression-bodied members
- Tuples and tuple deconstruction
- Pattern matching (basic `is` patterns)
- Local functions
- `out` variables
- Ref returns and locals
- Discards (`_`)
- Default literal expressions

**NOT Allowed (C# 8.0+):**
- ❌ Nullable reference types (`string?`)
- ❌ Switch expressions
- ❌ Using declarations (non-block `using`)
- ❌ Range operators (`..`)
- ❌ Indices (`^`)
- ❌ Pattern matching enhancements (property patterns, positional patterns)
- ❌ Records
- ❌ Init-only setters
- ❌ Top-level statements
- ❌ File-scoped namespaces

**Null Handling in netstandard2.0:**
- Use traditional null checks: `if (value == null)` or `value != null`
- Check for null at entry points and validate parameters
- Use `nameof()` for parameter names in exception messages

### For net8.0 Projects (Tests)
**Maximum C# Version: 12+**

**Recommended Modern Features:**
- File-scoped namespaces
- Using declarations
- Switch expressions
- Pattern matching (all forms)
- Target-typed `new` expressions
- Collection expressions
- Primary constructors (in test helpers/fixtures)

**Note:** Test projects should still work with netstandard2.0 libraries they reference.

## General Instructions

- Write code with maintainability in mind - include comments explaining non-obvious design decisions
- Handle edge cases explicitly, especially in analyzer logic where syntax trees can be malformed
- Document why certain Roslyn APIs are used over alternatives
- Make high-confidence suggestions that respect the netstandard2.0 constraint
- When in doubt about API availability, verify against netstandard2.0 compatibility

## Naming Conventions

- **PascalCase**: Classes, interfaces, methods, properties, public/internal fields
- **camelCase**: Private fields, local variables, parameters
- **Interface prefix**: All interfaces start with "I" (e.g., `ICodeAnalyzer`, `ICodeFix`)
- **Roslyn naming**: Follow Roslyn patterns for adapters/providers (e.g., `*RoslynAdapter`, `*CodeFixProvider`)
- **Test naming**: Match existing style in test files (typically `MethodName_Scenario_ExpectedResult`)

## Formatting

- Apply code-formatting style defined in `.editorconfig`
- **For netstandard2.0 projects**: Use traditional block-scoped namespaces
  ```csharp
  namespace Lintelligent.Core.Analyzers
  {
      public class MyAnalyzer : ICodeAnalyzer
      {
          // Implementation
      }
  }
  ```
- **For net8.0 test projects**: Use file-scoped namespaces
  ```csharp
  namespace Lintelligent.Analyzers.Basic.Test;
  
  public class MyAnalyzerTests
  {
      // Tests
  }
  ```
- Insert a newline before opening braces of code blocks
- Use `nameof` for member name references (available in C# 6.0+)
- XML doc comments required for all public APIs with `<summary>`, `<param>`, `<returns>` as applicable
- Include `<example>` sections for complex analyzers showing trigger patterns

## Roslyn Analyzer Development

### Core Analyzer Pattern (Lintelligent.Core)
```csharp
/// <summary>
/// Analyzes code for [specific pattern/issue].
/// </summary>
public class MyAnalyzer : ICodeAnalyzer
{
    public const string DiagnosticId = "LINT###";
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel model)
    {
        // Pure analysis logic - no Roslyn infrastructure dependencies
        var root = tree.GetRoot();
        var nodes = root.DescendantNodes().OfType<TargetSyntaxNode>();
        
        foreach (var node in nodes)
        {
            // Check conditions and yield diagnostics
            if (ShouldReportDiagnostic(node, model))
            {
                yield return new DiagnosticResult
                {
                    Id = DiagnosticId,
                    Span = node.Span,
                    // ... other properties
                };
            }
        }
    }
}
```

### Roslyn Adapter Pattern (Lintelligent.Analyzers.Basic)
```csharp
/// <summary>
/// Roslyn adapter for <see cref="MyAnalyzer"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MyRoslynAdapter : DiagnosticAnalyzer
{
    private readonly ICodeAnalyzer _analyzer = new MyAnalyzer();
    
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        MyAnalyzer.DiagnosticId,
        title: "Title",
        messageFormat: "Message format",
        category: "Category",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(Rule);
    
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        // Register appropriate syntax kind
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.TargetSyntaxKind);
    }
    
    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var diagnostics = _analyzer.Analyze(context.Node.SyntaxTree, context.SemanticModel);
        
        foreach (var diagnostic in diagnostics)
        {
            var location = Location.Create(context.Node.SyntaxTree, diagnostic.Span);
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }
}
```

### Code Fix Pattern
```csharp
/// <summary>
/// Provides code fixes for <see cref="MyAnalyzer"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MyCodeFixProvider))]
public class MyCodeFixProvider : CodeFixProvider
{
    private readonly ICodeFix _codeFix = new MyCodeFix();
    
    public sealed override ImmutableArray<string> FixableDiagnosticIds => 
        ImmutableArray.Create(MyAnalyzer.DiagnosticId);
    
    public sealed override FixAllProvider GetFixAllProvider() => 
        WellKnownFixAllProviders.BatchFixer;
    
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Implementation following established pattern
    }
}
```

## Roslyn API Best Practices

### Syntax Tree Navigation
- Use `DescendantNodes()` for deep traversal, `ChildNodes()` for shallow
- Prefer LINQ filters (`OfType<T>()`) for type-specific node queries
- Cache `tree.GetRoot()` if used multiple times
- Use `SyntaxKind` enums for node type checking

### Semantic Model Usage
- Always check if semantic model is available before use
- Use `GetSymbolInfo()` for symbol resolution
- Use `GetTypeInfo()` for type information
- Cache semantic model queries when analyzing multiple nodes

### Performance Considerations
- Avoid expensive operations in hot paths (e.g., inside loops over all nodes)
- Use `yield return` for lazy evaluation of diagnostics
- Prefer immutable data structures (ImmutableArray, ImmutableDictionary)
- Don't allocate unnecessarily - reuse collections where safe

### Error Handling
- Handle malformed syntax trees gracefully - users may have incomplete code
- Check for null before accessing semantic model results
- Validate node structure before assumptions (e.g., check parent exists)

## Testing

### Test Structure
- Use xUnit framework with Roslyn's `Microsoft.CodeAnalysis.Testing` library
- Place tests in corresponding test project (`Lintelligent.Analyzers.Basic.Test` or `Lintelligent.Core.Test`)
- **Do NOT use** "Arrange", "Act", "Assert" comments
- Follow existing test method naming patterns in the codebase

### Test Patterns
```csharp
[Fact]
public async Task AnalyzerName_DetectsIssue_WhenCondition()
{
    var test = """
        using System;
        
        class TestClass
        {
            void Method()
            {
                // Code that should trigger diagnostic
                {|#0:var problematicCode = true;|}
            }
        }
        """;
    
    var expected = VerifyCS.Diagnostic(MyAnalyzer.DiagnosticId)
        .WithLocation(0)
        .WithArguments("arguments if needed");
    
    await VerifyCS.VerifyAnalyzerAsync(test, expected);
}
```

### Testing Guidelines
- Test the happy path (diagnostic triggers correctly)
- Test that non-problematic code doesn't trigger
- Test edge cases (empty blocks, nested structures, etc.)
- Test code fixes produce expected output
- Use `[| |]` or `{|#0:|}` markup for expected diagnostic locations
- Include test cases for code that should NOT trigger diagnostics

## Documentation

### XML Documentation
```csharp
/// <summary>
/// Analyzes catch blocks to detect empty catch blocks that suppress exceptions without logging.
/// </summary>
/// <remarks>
/// This analyzer identifies catch blocks with no statements, which can hide errors and make debugging difficult.
/// It does not flag catch blocks that contain comments or logging statements.
/// </remarks>
/// <example>
/// <code>
/// // Triggers diagnostic:
/// try { DoWork(); }
/// catch { }
/// 
/// // Does not trigger:
/// try { DoWork(); }
/// catch { Logger.LogError("Error occurred"); }
/// </code>
/// </example>
```

### Inline Comments
- Explain **why** not **what** (code should be self-documenting)
- Document workarounds for Roslyn API limitations
- Explain non-obvious algorithm choices
- Note performance trade-offs

## Project Organization

### Follow Established Patterns
- **Core analyzers**: `src/Lintelligent.Core/Analyzers/{RuleName}Analyzer.cs`
- **Core fixes**: `src/Lintelligent.Core/CodeFixes/{RuleName}CodeFix.cs`
- **Roslyn adapters**: `src/Lintelligent.Analyzers.Basic/{RuleName}RoslynAdapter.cs`
- **Code fix providers**: `src/Lintelligent.Analyzers.Basic.CodeFixes/{RuleName}CodeFixProvider.cs`
- **Tests**: `tests/Lintelligent.Analyzers.Basic.Test/{RuleName}AdapterTests.cs`

### Dependency Rules
- **Lintelligent.Core**: References only Roslyn semantic model APIs (no analyzer infrastructure)
- **Adapter projects**: Reference Lintelligent.Core
- **CodeFix projects**: Reference Core + corresponding analyzer project
- **Package project**: References all runtime projects
- **Test projects**: Reference projects under test + testing frameworks

## Common Pitfalls

### netstandard2.0 Constraint Violations
- ❌ Using nullable reference types syntax (`string?`)
- ❌ Using switch expressions
- ❌ Using file-scoped namespaces in Core/Adapter projects
- ❌ Using C# 8+ pattern matching features
- ✅ Verify language features against [C# 7.3 spec](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7-3)

### Roslyn API Misuse
- ❌ Not checking if semantic model is null
- ❌ Assuming syntax tree is well-formed
- ❌ Using wrong `SyntaxKind` in node registration
- ❌ Expensive operations in `RegisterSyntaxNodeAction` callbacks
- ✅ Handle incomplete/malformed code gracefully

### Testing Issues
- ❌ Using index-based diagnostic IDs incorrectly
- ❌ Not testing non-triggering cases
- ❌ Hardcoding spans instead of using markup
- ✅ Use `{|#0:|}` markup for precise diagnostic locations

## Build and Validation

### Local Development
```powershell
# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "AvoidEmptyCatch"

# Pack for NuGet
dotnet pack src/Lintelligent.Analyzers.Basic.Package/Lintelligent.Analyzers.Basic.Package.csproj
```

### Before Committing
- Ensure all tests pass
- Verify analyzer triggers in sample code
- Check that diagnostic IDs are unique and follow LINT### pattern
- Confirm XML documentation is complete for public APIs
- Validate no C# 8+ features in netstandard2.0 projects
