# Lintelligent Core API Documentation

This document describes the main abstractions and types in the Lintelligent Core engine. It is intended for developers who want to extend or integrate with the core analysis logic.

---

## ICodeAnalyzer

```
public interface ICodeAnalyzer
{
    IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel semanticModel);
}
```
- **Purpose**: Contract for all analyzers. Implementations analyze a syntax tree and semantic model, returning a set of diagnostics.
- **Parameters**:
  - `SyntaxTree tree`: The syntax tree to analyze.
  - `SemanticModel semanticModel`: The semantic model for semantic analysis.
- **Returns**: `IEnumerable<DiagnosticResult>` – diagnostics found in the code.

## ICodeFix

```
public interface ICodeFix
{
    bool CanFix(DiagnosticResult diagnostic);
    CodeFixResult ApplyFix(DiagnosticResult diagnostic, SyntaxTree tree);
}
```
- **Purpose**: Contract for code fixes. Implementations determine if a diagnostic can be fixed and apply the fix.
- **Methods**:
  - `CanFix(DiagnosticResult diagnostic)`: Returns true if this code fix can handle the diagnostic.
  - `ApplyFix(DiagnosticResult diagnostic, SyntaxTree tree)`: Applies the fix and returns the updated syntax tree.

## DiagnosticResult

```
public sealed class DiagnosticResult
{
    public string Id { get; }
    public string Message { get; }
    public TextSpan Span { get; }
    public DiagnosticSeverity Severity { get; }
}
```
- **Purpose**: Represents a diagnostic found by an analyzer.
- **Properties**:
  - `Id`: Diagnostic ID (e.g., LINT001)
  - `Message`: Diagnostic message
  - `Span`: Location in the source code
  - `Severity`: Diagnostic severity (Info, Warning, Error)

## CodeFixResult

```
public sealed class CodeFixResult
{
    public SyntaxTree UpdatedTree { get; }
}
```
- **Purpose**: Represents the result of applying a code fix.
- **Properties**:
  - `UpdatedTree`: The updated syntax tree after the fix

---

For more details, see the source code in `src/Lintelligent.Core/Abstractions/`.
