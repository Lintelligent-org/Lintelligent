
## Lintelligent Architecture Overview

Lintelligent is a modular, layered Roslyn analyzer framework designed for extensibility, testability, and maintainability. It separates core analysis logic from Roslyn-specific infrastructure using an Adapter pattern, enabling clean boundaries between business logic and integration layers.

---

### Layered Design

#### 1. Core Layer (`Lintelligent.Core`)
- **Target:** `netstandard2.0` for maximum compatibility
- **Purpose:** Contains all framework-agnostic analyzer and code fix logic
- **Key Components:**
	- `ICodeAnalyzer` and `ICodeFix` interfaces define contracts for analyzers and code fixes
	- Core analyzers (e.g., `AvoidEmptyCatchAnalyzer`) implement pure analysis logic using Roslyn's semantic model APIs
	- Returns framework-agnostic `DiagnosticResult` and `CodeFixResult` types
- **No dependency on Roslyn analyzer infrastructure**—only uses semantic model APIs

#### 2. Analyzer Adapter Layer (`Lintelligent.Analyzers.Basic`)
- **Purpose:** Bridges Core analyzers to Roslyn's `DiagnosticAnalyzer` infrastructure
- **Pattern:** Each `*RoslynAdapter` class wraps a Core `ICodeAnalyzer`
- **Responsibilities:**
	- Registers Roslyn syntax node actions (e.g., for `SyntaxKind.CatchClause`)
	- Converts `DiagnosticResult` from Core to Roslyn `Diagnostic` objects
	- Reports diagnostics to the Roslyn engine

#### 3. Code Fix Adapter Layer (`Lintelligent.Analyzers.Basic.CodeFixes`)
- **Purpose:** Bridges Core code fixes to Roslyn's `CodeFixProvider` infrastructure
- **Pattern:** Each `*CodeFixProvider` wraps a Core `ICodeFix` implementation
- **Responsibilities:**
	- Converts Roslyn diagnostics to Core `DiagnosticResult`
	- Applies Core code fixes and returns modified documents

---

### Key Architectural Patterns

- **Adapter Pattern:** Cleanly separates business logic from Roslyn integration, allowing analyzers and code fixes to be tested independently of the Roslyn infrastructure.
- **Testability:** Core logic is isolated and can be unit tested without Roslyn dependencies. Adapters are tested with Roslyn's analyzer testing framework.
- **Reusability:** Core analyzers could be adapted to other compiler platforms in the future.
- **Maintainability:** Changes to analysis logic do not require changes to Roslyn plumbing.

---

### Project Structure

```
src/
	Lintelligent.Core/                # Core analysis logic (framework-agnostic)
		Analyzers/
		CodeFixes/
		Abstractions/
		Utilities/
	Lintelligent.Analyzers.Basic/      # Roslyn analyzer adapters
		Roslyn/
	Lintelligent.Analyzers.Basic.CodeFixes/ # Roslyn code fix adapters
	Lintelligent.Analyzers.Basic.Package/   # NuGet packaging project
tests/
	Lintelligent.Core.Test/            # Core logic unit tests
	Lintelligent.Analyzers.Basic.Test/ # Roslyn integration tests
```

---

### Diagnostic and Code Fix Flow

1. **Source code** is parsed by Roslyn.
2. **Roslyn Adapter** (`*RoslynAdapter`) receives syntax node events and delegates to the Core analyzer.
3. **Core Analyzer** (`ICodeAnalyzer`) analyzes the syntax tree and semantic model, returning `DiagnosticResult` objects.
4. **Adapter** converts `DiagnosticResult` to Roslyn `Diagnostic` and reports it.
5. **Code Fix Adapter** (`*CodeFixProvider`) receives diagnostics, converts them to Core format, and applies the Core code fix logic.

---

### Adding a New Analyzer or Code Fix

1. **Implement Core Analyzer** in `Lintelligent.Core/Analyzers/` (pure logic, no Roslyn dependencies)
2. **Create Roslyn Adapter** in `Lintelligent.Analyzers.Basic/` (wraps Core analyzer)
3. **(Optional) Implement Core Code Fix** in `Lintelligent.Core/CodeFixes/`
4. **(Optional) Create Code Fix Provider** in `Lintelligent.Analyzers.Basic.CodeFixes/`
5. **Add Tests** in `tests/` projects using xUnit and Roslyn's analyzer testing framework

---

### Build, Test, and Packaging

- **Build:** All projects target `netstandard2.0` (except test projects, which use `net8.0`)
- **Testing:**
	- Core logic: xUnit tests in `Lintelligent.Core.Test`
	- Roslyn integration: Analyzer and code fix tests in `Lintelligent.Analyzers.Basic.Test`
- **Packaging:**
	- `Lintelligent.Analyzers.Basic.Package` packs analyzers and code fixes into a NuGet package
	- Analyzers are placed in `analyzers/dotnet/cs` in the package output

---

### Conventions

- **Diagnostic IDs:** `LINT###` (e.g., `LINT001`)
- **File Naming:**
	- Core analyzers: `{RuleName}Analyzer.cs`
	- Core code fixes: `{RuleName}CodeFix.cs`
	- Adapters: `{RuleName}RoslynAdapter.cs`
	- Providers: `{RuleName}CodeFixProvider.cs`
- **Project References:**
	- Core has no external references
	- Analyzer projects reference Core
	- CodeFix projects reference Core and corresponding analyzer
	- Package project references both analyzer and code fix projects

---

### Design Philosophy

- **Separation of Concerns:** Business logic is decoupled from infrastructure
- **Extensibility:** New analyzers and code fixes can be added with minimal boilerplate
- **Clarity:** Clear boundaries between analysis logic and Roslyn integration

---

### Example: AvoidEmptyCatch Rule

**Core Analyzer:** Implements logic to detect empty catch blocks

**Roslyn Adapter:** Registers for `SyntaxKind.CatchClause`, delegates to Core, and reports diagnostics

**Code Fix:** (If applicable) Suggests adding a comment or rethrowing the exception

---

For more details, see the [README.md](../README.md) and inline code comments.
