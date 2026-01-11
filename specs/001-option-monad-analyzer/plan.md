# Implementation Plan: Option Monad Analyzer

**Branch**: `001-option-monad-analyzer` | **Date**: January 11, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-option-monad-analyzer/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Create a Roslyn analyzer that detects methods returning nullable types (`T?`) and suggests using `Option<T>` from the language-ext library instead. The analyzer will follow Lintelligent's three-layer architecture (Core → Adapter → CodeFix) to provide both diagnostic detection and automatic code fixes that transform nullable returns to Option-based patterns with `Some()` and `None` constructors.

## Technical Context

**Language/Version**: C# 12.0 / .NET 8.0 (netstandard2.0 for analyzer projects)  
**Primary Dependencies**: 
- Microsoft.CodeAnalysis.CSharp (Roslyn SDK) 4.8.0+
- Microsoft.CodeAnalysis.CSharp.Workspaces 4.8.0+
- language-ext (LanguageExt.Core) 4.4.0+ (for Option<T> type reference)
- xUnit 2.6.0+ with Microsoft.CodeAnalysis.Testing.XUnit for analyzer tests

**Storage**: N/A (analyzer operates on syntax trees in-memory)  
**Testing**: xUnit with Microsoft.CodeAnalysis.CSharp.Analyzer.Testing for Roslyn analyzer verification  
**Target Platform**: Visual Studio 2022+, Rider 2023+, VS Code with C# Dev Kit (any IDE supporting Roslyn analyzers)  
**Project Type**: Roslyn analyzer library (three-layer architecture per Lintelligent constitution)  
**Performance Goals**: 
- Diagnostic analysis: <100ms per method on average
- Code fix application: <500ms per method (per SC-004)
- Zero UI blocking in IDE during analysis

**Constraints**: 
- Must use netstandard2.0 for maximum IDE compatibility
- Core analyzer logic must remain Roslyn infrastructure-free
- 95%+ detection accuracy for nullable returns (per SC-001)
- 90%+ successful code fix transformation rate (per SC-002)

**Scale/Scope**: 
- Single analyzer rule (LINT003)
- 3-4 code fix providers (nullable reference, nullable value, generic methods, Task<T?>)
- Estimated 500-800 LOC for Core analyzer
- Estimated 300-500 LOC per adapter/code fix provider

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Layered Architecture ✅ PASS
- **Compliance**: Feature will implement three-layer design
  - Core: `PreferOptionMonadAnalyzer` in `Lintelligent.Core/Analyzers/`
  - Adapter: `PreferOptionMonadRoslynAdapter` in `Lintelligent.Analyzers.Basic/`
  - CodeFix: `PreferOptionMonadCodeFixProvider` in `Lintelligent.Analyzers.Basic.CodeFixes/`
- **Validation**: Each layer will have independent unit tests; Core tests will not depend on Roslyn infrastructure

### Principle II: Test-First Development ✅ PASS
- **Compliance**: Development will follow TDD workflow
  - Red: Write failing tests using `[| |]` markup for diagnostic locations
  - Green: Implement Core analyzer to pass tests
  - Refactor: Extract helper methods, optimize pattern matching
- **Validation**: Pull request will include test evidence showing Red-Green-Refactor cycle

### Principle III: Semantic Versioning ✅ PASS
- **Compliance**: New analyzer is a MINOR version bump (backward-compatible feature)
  - Current: 1.0.0 → Target: 1.1.0
  - No breaking changes to existing analyzers
  - New diagnostic ID LINT003 does not conflict with existing rules
- **Validation**: Version bump will be documented in pull request

### Principle IV: Framework-Agnostic Core ✅ PASS
- **Compliance**: Core analyzer will use only:
  - `SyntaxTree` and `SemanticModel` from Roslyn (allowed per constitution)
  - Standard .NET types (IEnumerable, LINQ)
  - No dependencies on DiagnosticAnalyzer, CodeFixProvider, or IDE APIs
- **Validation**: Core project references will be audited in code review

### Principle V: Public API Stability ✅ PASS
- **Compliance**: New public interfaces will be added to Core:
  - `ICodeAnalyzer.Analyze()` - already exists, no changes
  - `ICodeFix.Apply()` - already exists, no changes
  - New analyzer implements existing interfaces (no API surface changes)
- **Validation**: No modifications to existing public APIs; only new implementations

### Quality Gates Status

1. **Build**: ✅ Will verify solution builds on .NET SDK 8.0+
2. **Tests**: ✅ TDD ensures tests pass before merge
3. **Coverage**: ✅ Target ≥80% coverage for new analyzer code
4. **Conventions**: ✅ Diagnostic ID LINT003, file names follow `PreferOptionMonad*` pattern
5. **Documentation**: ✅ XML docs for public methods, inline comments for complex logic
6. **Security**: ✅ No user input processing, no OWASP concerns (read-only syntax analysis)

**Overall Assessment**: ✅ ALL GATES PASS - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-option-monad-analyzer/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output - language-ext patterns, Roslyn nullable detection
├── data-model.md        # Phase 1 output - DiagnosticResult, CodeFixResult structures
├── quickstart.md        # Phase 1 output - How to use the analyzer
├── contracts/           # Phase 1 output - Diagnostic message formats, code fix examples
│   ├── diagnostic-schema.md
│   └── codefix-examples.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Lintelligent.Core/
│   ├── Analyzers/
│   │   ├── AvoidEmptyCatchAnalyzer.cs
│   │   ├── ComplexConditionalAnalyzer.cs
│   │   └── PreferOptionMonadAnalyzer.cs          # NEW: Core analyzer logic
│   ├── CodeFixes/
│   │   ├── AvoidEmptyCatchCodeFix.cs
│   │   ├── ComplexConditionalCodeFix.cs
│   │   └── PreferOptionMonadCodeFix.cs            # NEW: Core code fix logic
│   ├── Abstractions/
│   │   ├── ICodeAnalyzer.cs
│   │   └── ICodeFix.cs
│   └── Utilities/
│       └── NullableTypeHelper.cs                   # NEW: Helper for nullable type detection
│
├── Lintelligent.Analyzers.Basic/
│   ├── AvoidEmptyCatchRoslynAdapter.cs
│   ├── ComplexConditionalRoslynAdapter.cs
│   ├── PreferOptionMonadRoslynAdapter.cs          # NEW: Roslyn adapter
│   ├── Resources.resx
│   └── Roslyn/
│       ├── AnalyzerAdapter.cs
│       └── CodeFixAdapter.cs
│
└── Lintelligent.Analyzers.Basic.CodeFixes/
    ├── AvoidEmptyCatchCodeFixProvider.cs
    ├── ComplexConditionalCodeFixProvider.cs
    └── PreferOptionMonadCodeFixProvider.cs        # NEW: Roslyn code fix provider

tests/
├── Lintelligent.Core.Test/
│   ├── Analyzers/
│   │   └── PreferOptionMonadAnalyzerTests.cs      # NEW: Core analyzer tests
│   └── CodeFixes/
│       └── PreferOptionMonadCodeFixTests.cs       # NEW: Core code fix tests
│
└── Lintelligent.Analyzers.Basic.Test/
    └── PreferOptionMonadAdapterTests.cs           # NEW: Roslyn adapter integration tests
```

**Structure Decision**: Using existing Lintelligent three-layer architecture. New files will be added to existing projects following the established pattern (Core → Adapter → CodeFix). No new projects needed as this is a single analyzer rule within the Basic analyzer suite.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: ✅ NO VIOLATIONS

All constitution principles are satisfied without exceptions. No complexity justification required.

---

## Phase 0: Research & Discovery

### Research Tasks

#### RT-001: language-ext Option<T> Patterns
**Objective**: Understand how language-ext implements the Option monad and what patterns consumers use

**Questions to Answer**:
1. What are the canonical ways to create Option<T> (Some, None, constructors)?
2. How do developers typically consume Option values (Match, Map, Bind, IfNone)?
3. What are the type signatures for Option<T> in language-ext?
4. Are there extension methods like `.ToSome()` or `.ToOption()`?
5. What namespace is required (`using LanguageExt;`)?

**Research Method**: Review language-ext GitHub repository, NuGet package documentation, and common usage patterns

#### RT-002: Roslyn Nullable Type Detection
**Objective**: Determine how to reliably detect nullable reference and value types using Roslyn's semantic model

**Questions to Answer**:
1. How to distinguish between `T?` for reference types vs value types?
2. How to handle generic methods with nullable type parameters?
3. How to detect `Task<T?>` and other wrapped nullable types?
4. What SyntaxKind nodes represent method return types?
5. How to use ITypeSymbol to check for nullability?

**Research Method**: Review Microsoft Roslyn documentation, existing Lintelligent analyzers, and Roslyn analyzer samples

#### RT-003: Code Fix Transformation Patterns
**Objective**: Identify best practices for transforming return statements and method signatures

**Questions to Answer**:
1. How to safely replace method return type in syntax tree?
2. How to identify and transform all return statements in a method body?
3. How to add using directives without duplicates?
4. How to handle edge cases (lambda returns, expression-bodied members)?
5. What are the performance implications of document transformations?

**Research Method**: Review existing Lintelligent code fixes, Roslyn Workspaces API documentation, and code fix patterns

#### RT-004: Roslyn Testing Framework
**Objective**: Understand how to write effective tests for analyzers and code fixes

**Questions to Answer**:
1. How does the `[| |]` markup syntax work for diagnostic locations?
2. How to test code fixes with before/after comparisons?
3. How to test multiple diagnostics in a single file?
4. How to configure test compilation options (nullable reference types)?
5. What are best practices for test organization?

**Research Method**: Review existing Lintelligent tests, Microsoft.CodeAnalysis.Testing documentation

### Expected Research Outputs

**Deliverable**: `research.md` documenting:
- language-ext Option<T> API surface and usage patterns
- Roslyn nullable type detection algorithms
- Code fix transformation strategies with examples
- Testing framework patterns and best practices
- Any blockers or clarifications needed before design

---

## Phase 1: Design & Contracts

### Design Artifacts

#### DA-001: Data Model (`data-model.md`)

**Core Analyzer Data Structures**:

```csharp
// DiagnosticResult from Core analyzer
public class PreferOptionMonadDiagnosticResult : DiagnosticResult
{
    public string MethodName { get; set; }
    public string CurrentReturnType { get; set; }  // e.g., "string?"
    public string SuggestedReturnType { get; set; } // e.g., "Option<string>"
    public bool IsValueType { get; set; }
    public TextSpan ReturnTypeSpan { get; set; }
}

// CodeFixResult from Core code fix
public class PreferOptionMonadCodeFixResult : CodeFixResult
{
    public SyntaxNode UpdatedMethodDeclaration { get; set; }
    public List<ReturnStatementTransformation> ReturnTransformations { get; set; }
    public bool RequiresUsingDirective { get; set; }
}

public class ReturnStatementTransformation
{
    public SyntaxNode OriginalReturn { get; set; }
    public SyntaxNode TransformedReturn { get; set; }
    public bool IsNullReturn { get; set; }
}
```

**Roslyn Adapter Entities**:
- DiagnosticDescriptor for LINT003
- SyntaxKind.MethodDeclaration node registration
- Location mapping from Core DiagnosticResult

#### DA-002: Contracts (`contracts/` directory)

**File**: `contracts/diagnostic-schema.md`
```markdown
# LINT003 Diagnostic Contract

**Diagnostic ID**: LINT003
**Title**: Prefer Option<T> over nullable types
**Severity**: Info (configurable to Warning/Error)
**Category**: Design

**Message Format**:
"Method '{methodName}' returns nullable type '{currentType}'. Consider using 'Option<{innerType}>' to make absence of value explicit."

**Example**:
```csharp
// Triggers LINT003
public string? GetUserName(int userId)
{
    if (userId == 0) return null;
    return "Alice";
}

// Suggested refactoring
public Option<string> GetUserName(int userId)
{
    if (userId == 0) return Option<string>.None;
    return Option.Some("Alice");
}
```
```

**File**: `contracts/codefix-examples.md`
```markdown
# Code Fix Transformation Examples

## Example 1: Nullable Reference Type

**Before**:
```csharp
public string? FindUser(int id)
{
    if (id < 0) return null;
    return database.GetUser(id);
}
```

**After**:
```csharp
using LanguageExt;

public Option<string> FindUser(int id)
{
    if (id < 0) return Option<string>.None;
    return Option.Some(database.GetUser(id));
}
```

## Example 2: Nullable Value Type

**Before**:
```csharp
public int? ParseNumber(string input)
{
    if (string.IsNullOrEmpty(input)) return null;
    return int.Parse(input);
}
```

**After**:
```csharp
using LanguageExt;

public Option<int> ParseNumber(string input)
{
    if (string.IsNullOrEmpty(input)) return Option<int>.None;
    return Option.Some(int.Parse(input));
}
```

## Example 3: Generic Method

**Before**:
```csharp
public T? GetValue<T>(string key) where T : class
{
    return cache.TryGet(key, out var value) ? value as T : null;
}
```

**After**:
```csharp
using LanguageExt;

public Option<T> GetValue<T>(string key) where T : class
{
    return cache.TryGet(key, out var value) 
        ? Option.Some(value as T) 
        : Option<T>.None;
}
```
```

#### DA-003: Quickstart Guide (`quickstart.md`)

```markdown
# Quickstart: Using the Prefer Option Monad Analyzer

## Installation

1. Install the Lintelligent.Analyzers.Basic NuGet package:
   ```bash
   dotnet add package Lintelligent.Analyzers.Basic
   ```

2. Install language-ext for Option<T> support:
   ```bash
   dotnet add package LanguageExt.Core
   ```

## Using the Analyzer

The analyzer automatically detects methods returning nullable types:

```csharp
// This will show LINT003 diagnostic
public string? GetName() => null;
```

## Applying the Code Fix

1. In Visual Studio/Rider, you'll see a lightbulb or screwdriver icon
2. Click it and select "Convert to Option<T>"
3. The analyzer will:
   - Change the return type from `T?` to `Option<T>`
   - Add `using LanguageExt;` if needed
   - Transform `return null;` to `Option<T>.None`
   - Wrap non-null returns in `Option.Some()`

## Configuration

Customize analyzer behavior in `.editorconfig`:

```ini
# Change severity to warning
dotnet_diagnostic.LINT003.severity = warning

# Disable for test files
[**Tests.cs]
dotnet_diagnostic.LINT003.severity = none
```

## Best Practices

1. **Consume Option values safely**:
   ```csharp
   var name = GetName().Match(
       Some: value => value,
       None: () => "Unknown"
   );
   ```

2. **Chain operations with Map/Bind**:
   ```csharp
   var upperName = GetName()
       .Map(n => n.ToUpper())
       .IfNone("UNKNOWN");
   ```

3. **Use extension methods for clarity**:
   ```csharp
   return user?.Name.ToSome() ?? Option<string>.None;
   ```
```

### Agent Context Update

After Phase 1 design completion, run:

```powershell
.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot
```

This will add to `.github/copilot-instructions.md`:
- language-ext Option<T> patterns
- LINT003 diagnostic ID and behavior
- Three-layer implementation for PreferOptionMonad analyzer
- Code fix transformation strategies

### Phase 1 Constitution Re-check

**Re-validation after design**:

- ✅ **Layered Architecture**: Design maintains Core/Adapter/CodeFix separation
- ✅ **Framework-Agnostic Core**: Core uses only SyntaxTree and SemanticModel
- ✅ **No API Surface Changes**: Implements existing ICodeAnalyzer and ICodeFix
- ✅ **Test Coverage**: Design includes unit tests for all three layers
- ✅ **Documentation**: XML docs for public methods, inline comments for complex logic

**Overall**: ✅ ALL GATES STILL PASS

---

## Phase 2: Implementation Tasks

**Note**: Phase 2 task generation is handled by the `/speckit.tasks` command, which:
1. Reads this plan.md
2. Generates fine-grained implementation tasks in `tasks.md`
3. Organizes tasks by priority and dependencies
4. Provides acceptance criteria for each task

The implementation will follow TDD:
1. **Red**: Write failing test for nullable return detection
2. **Green**: Implement Core analyzer to pass test
3. **Refactor**: Extract helpers, optimize
4. Repeat for code fix, adapter, and integration

**Expected Task Categories**:
- Core analyzer implementation (PreferOptionMonadAnalyzer.cs)
- Core code fix implementation (PreferOptionMonadCodeFix.cs)
- Roslyn adapter implementation (PreferOptionMonadRoslynAdapter.cs)
- Roslyn code fix provider (PreferOptionMonadCodeFixProvider.cs)
- Unit tests for all layers
- Integration tests
- Documentation updates
- NuGet package updates

---

## Summary

**Branch**: `001-option-monad-analyzer`  
**Implementation Plan**: `specs/001-option-monad-analyzer/plan.md` (this file)  
**Status**: ✅ READY FOR PHASE 0 RESEARCH

**Next Steps**:
1. Execute Phase 0 research tasks (RT-001 through RT-004)
2. Create `research.md` with findings
3. Execute Phase 1 design (DA-001 through DA-003)
4. Create `data-model.md`, `contracts/`, and `quickstart.md`
5. Run `/speckit.tasks` to generate Phase 2 implementation tasks

**Artifacts Generated by This Plan**:
- ✅ `plan.md` (this file)
- ⏳ `research.md` (Phase 0 output - pending)
- ⏳ `data-model.md` (Phase 1 output - pending)
- ⏳ `quickstart.md` (Phase 1 output - pending)
- ⏳ `contracts/diagnostic-schema.md` (Phase 1 output - pending)
- ⏳ `contracts/codefix-examples.md` (Phase 1 output - pending)
- ⏳ `tasks.md` (Phase 2 output - generated by `/speckit.tasks`)
