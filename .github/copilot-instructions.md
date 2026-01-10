# Lintelligent: Roslyn Analyzer Framework

## Project Architecture

Lintelligent is a **layered Roslyn analyzer framework** that separates core analysis logic from Roslyn-specific infrastructure using an **Adapter pattern**.

### Three-Layer Design

1. **`Lintelligent.Core`** (netstandard2.0): Framework-agnostic analyzer logic
   - `ICodeAnalyzer` and `ICodeFix` interfaces define contracts
   - Core analyzers (e.g., `AvoidEmptyCatchAnalyzer`) contain pure business logic
   - Returns framework-agnostic `DiagnosticResult` and `CodeFixResult` types
   - Depends only on Roslyn's semantic model APIs, not the analyzer infrastructure

2. **`Lintelligent.Analyzers.Basic`**: Roslyn adapter layer
   - Implements `DiagnosticAnalyzer` to bridge Core analyzers to Roslyn
   - Pattern: Each `*RoslynAdapter` class wraps a Core `ICodeAnalyzer`
   - Example: `AvoidEmptyCatchRoslynAdapter` delegates to `AvoidEmptyCatchAnalyzer`
   - Registers syntax node actions and converts `DiagnosticResult` → `Diagnostic`

3. **`Lintelligent.Analyzers.Basic.CodeFixes`**: Code fix adapter layer
   - Implements `CodeFixProvider` to bridge Core code fixes to Roslyn
   - Uses `Extensions.ToDiagnosticResult()` to convert Roslyn diagnostics
   - Applies Core `ICodeFix` implementations and returns modified documents

### Key Pattern: Adapter-Based Separation

```csharp
// Core layer - pure logic
public class AvoidEmptyCatchAnalyzer : ICodeAnalyzer {
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel model) { ... }
}

// Adapter layer - Roslyn integration
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AvoidEmptyCatchRoslynAdapter : DiagnosticAnalyzer {
    private readonly ICodeAnalyzer _analyzer = new AvoidEmptyCatchAnalyzer();
    
    public override void Initialize(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(ctx => {
            var diagnostics = _analyzer.Analyze(ctx.Node.SyntaxTree, ctx.SemanticModel);
            foreach (var d in diagnostics) {
                ctx.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(...)));
            }
        }, SyntaxKind.CatchClause);
    }
}
```

## Building & Testing

### Build Configuration
- Solution: `Lintelligent.slnx` (modern solution format)
- All analyzer projects target **netstandard2.0** for maximum compatibility
- Test projects use **net8.0** with xUnit + Microsoft.CodeAnalysis.Testing

### Running Tests
```powershell
dotnet test                                    # Run all tests
dotnet test --filter AvoidEmptyCatch          # Filter by name
```

Tests use Roslyn's testing framework with `[| |]` markup for expected diagnostics:
```csharp
var test = """
    try { } 
    catch { [| |] }  // [| |] marks expected diagnostic location
    """;
await VerifyCS.VerifyAnalyzerAsync(test);
```

### Packaging
- **Package project**: `Lintelligent.Analyzers.Basic.Package.csproj`
- Packs analyzers to `analyzers/dotnet/cs` NuGet folder
- Set `GeneratePackageOnBuild=true` for automatic packaging

## Critical Conventions

### Diagnostic IDs
- Format: `LINT###` (e.g., `LINT001` for AvoidEmptyCatch)
- Defined in both Core analyzer (for logic) and Roslyn adapter (for registration)
- Must match between analyzer, code fix, and tests

### Project References
- Core → No external references (only Roslyn semantic model)
- Analyzer projects → Reference `Lintelligent.Core`
- CodeFix projects → Reference `Lintelligent.Core` + corresponding analyzer
- Package project → Reference both analyzer and code fix projects

### File Naming
- Core analyzers: `{RuleName}Analyzer.cs`
- Core code fixes: `{RuleName}CodeFix.cs`
- Adapters: `{RuleName}RoslynAdapter.cs`
- Providers: `{RuleName}CodeFixProvider.cs`

## Adding a New Analyzer

1. **Create Core Analyzer** in `Lintelligent.Core/Analyzers/`:
   ```csharp
   public class MyNewAnalyzer : ICodeAnalyzer {
       public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel model) {
           // Pure analysis logic using SyntaxTree traversal
       }
   }
   ```

2. **Create Roslyn Adapter** in `Lintelligent.Analyzers.Basic/`:
   ```csharp
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   public class MyNewRoslynAdapter : DiagnosticAnalyzer {
       private readonly ICodeAnalyzer _analyzer = new MyNewAnalyzer();
       // Register with appropriate SyntaxKind
   }
   ```

3. **Create Code Fix** (if applicable):
   - Core fix in `Lintelligent.Core/CodeFixes/`
   - Provider in `Lintelligent.Analyzers.Basic.CodeFixes/`

4. **Add Tests** in `tests/Lintelligent.Analyzers.Basic.Test/`:
   - Use xUnit + Roslyn testing framework
   - Leverage `[| |]` markup for diagnostic locations

## Common Issues

- **Adapter not triggering**: Ensure `SyntaxKind` in `RegisterSyntaxNodeAction` matches target nodes
- **Code fix not appearing**: Verify `FixableDiagnosticIds` matches diagnostic ID exactly
- **Package not building**: Check analyzer DLLs are copied to `analyzers/dotnet/cs` in package output
- **Tests failing**: Confirm `[| |]` markup spans match `DiagnosticResult.Span` exactly

## Design Philosophy

This architecture enables:
- **Testability**: Core logic isolated from Roslyn infrastructure
- **Reusability**: Core analyzers could theoretically work with other compiler platforms
- **Maintainability**: Changes to analysis logic don't require touching Roslyn plumbing
- **Clarity**: Separation between "what to analyze" (Core) and "how to integrate" (Adapters)
