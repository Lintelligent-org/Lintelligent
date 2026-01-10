# Lintelligent

> **A modern, extensible Roslyn analyzer framework for .NET, designed for clarity, testability, and real-world code quality.**

---

## Overview

Lintelligent is a layered, open-source framework for building custom Roslyn analyzers and code fixes. It separates core analysis logic from Roslyn infrastructure, making analyzers easier to test, maintain, and extend. Whether you're enforcing code standards, preventing bugs, or automating refactorings, Lintelligent provides a robust foundation for static analysis in .NET projects.

---

## Features

- **Layered architecture:** Clean separation between core logic and Roslyn adapters
- **Testable by design:** Core analyzers are framework-agnostic and easy to unit test
- **Extensible:** Add new rules, code fixes, and adapters with minimal boilerplate
- **NuGet packaging:** Ready for CI/CD and integration into your projects
- **Modern .NET support:** Analyzer core targets netstandard2.0 for broad compatibility; tests use .NET 8

---

## How It Works

Lintelligent uses a three-layer design:

1. **Core Layer (`Lintelligent.Core`)**
	- Pure analyzer logic, no Roslyn dependencies
	- Defines `ICodeAnalyzer` and `ICodeFix` contracts
	- Returns framework-agnostic diagnostic results

2. **Adapter Layer (`Lintelligent.Analyzers.Basic`)**
	- Bridges core analyzers to Roslyn's `DiagnosticAnalyzer`
	- Handles syntax node registration and diagnostic reporting

3. **Code Fix Layer (`Lintelligent.Analyzers.Basic.CodeFixes`)**
	- Bridges core code fixes to Roslyn's `CodeFixProvider`
	- Converts diagnostics and applies code transformations

> [!TIP]
> This separation means you can test analyzer logic without spinning up the Roslyn infrastructure, and reuse analyzers in other contexts (like CLI tools) in the future.

---

## Quickstart

### 1. Build the Solution

```sh
dotnet build
```

### 2. Run Tests

```sh
dotnet test
```

### 3. Add a New Analyzer

1. Implement your analyzer logic in `src/Lintelligent.Core/Analyzers/`.
2. Create a Roslyn adapter in `src/Lintelligent.Analyzers.Basic/`.
3. (Optional) Add a code fix in `src/Lintelligent.Core/CodeFixes/` and its provider in `src/Lintelligent.Analyzers.Basic.CodeFixes/`.
4. Add tests in `tests/Lintelligent.Analyzers.Basic.Test/`.

See [docs/architecture.md](docs/architecture.md) for detailed patterns and examples.

---

## Example: Avoid Empty Catch Analyzer

Detects and flags empty `catch` blocks that silently suppress exceptions.

```csharp
try { /* ... */ }
catch { /* [| |] triggers diagnostic here */ }
```

---

## Project Structure

```
src/
  Lintelligent.Core/                # Core analyzer logic (netstandard2.0)
  Lintelligent.Analyzers.Basic/      # Roslyn adapters
  Lintelligent.Analyzers.Basic.CodeFixes/ # Code fix providers
  Lintelligent.Analyzers.Basic.Package/   # NuGet packaging
tests/
  Lintelligent.Analyzers.Basic.Test/ # Analyzer and code fix tests (.NET 8)
  Lintelligent.Core.Test/            # Core logic tests
docs/                                # Architecture, contributing, roadmap
```

---

## Why Lintelligent?

- **Maintainable:** Core logic is decoupled from Roslyn, making updates and testing easier
- **Scalable:** Add new rules and code fixes without Roslyn boilerplate
- **Compatible:** Works with any .NET project supporting analyzers
- **Open:** Designed for extension, community contributions, and future CLI tooling

---

## Resources

- [Architecture Guide](docs/architecture.md)
- [How to Contribute](docs/contributing.md)
- [Roadmap](docs/roadmap.md)

---

> [!NOTE]
> Lintelligent is under active development. Feedback and contributions are welcome!