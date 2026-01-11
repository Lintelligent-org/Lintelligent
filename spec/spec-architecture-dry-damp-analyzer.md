---
title: DRY vs DAMP Analyzer Architecture Specification
version: 1.0
date_created: 2026-01-11
owner: Lintelligent Team
tags: [architecture, analyzer, DRY, DAMP, code-quality, encapsulation]
---

# Introduction

This specification defines the requirements and architecture for a code analyzer that evaluates adherence to the DRY (Don't Repeat Yourself) and DAMP (Descriptive And Meaningful Phrases) principles, with a focus on encapsulating implementation details in methods. The analyzer is intended to help developers write code that clearly expresses WHAT it does at a high level, while encapsulating HOW it does it in well-named methods, as described in the referenced [Enterprise Craftsmanship blog post](https://enterprisecraftsmanship.com/posts/dry-damp-unit-tests/).

## 1. Purpose & Scope

The purpose of this specification is to guide the development of an analyzer that:
- Detects code that violates DRY by duplicating logic or implementation details.
- Encourages DAMP by promoting descriptive, intention-revealing method names and encapsulation of implementation details.
- Applies to both production and test code.

Intended audience: Developers, code reviewers, and tool integrators seeking to improve code maintainability and readability.

Assumptions:
- The analyzer will be integrated into the Lintelligent Roslyn analyzer framework.
- The codebase is primarily C#.

## 2. Definitions

- **DRY**: Don't Repeat Yourself – a principle aimed at reducing duplication of logic or implementation.
- **DAMP**: Descriptive And Meaningful Phrases – a principle encouraging code that is easy to read and understand, even at the cost of some duplication.
- **Encapsulation**: Hiding implementation details within methods or classes, exposing only high-level intentions.
- **Intention-Revealing Name**: A method or variable name that clearly describes its purpose or effect.
- **Implementation Detail**: The specific logic or steps required to achieve a higher-level goal.

## 3. Requirements, Constraints & Guidelines

- **REQ-001**: The analyzer shall detect repeated code blocks that could be abstracted into a method (DRY violations).
- **REQ-002**: The analyzer shall identify code where implementation details are not encapsulated in methods, leading to low-level logic in high-level code (DAMP violations).
- **REQ-003**: The analyzer shall encourage the use of intention-revealing method names for encapsulated logic.
- **REQ-004**: The analyzer shall support both production and test code.
- **REQ-005**: The analyzer shall provide actionable diagnostics and suggestions for refactoring.
- **CON-001**: The analyzer must not produce false positives for trivial or idiomatic code patterns (e.g., simple assignments, assertions).
- **SEC-001**: The analyzer must not expose or log any sensitive code or data.
- **GUD-001**: Prefer encapsulating complex or multi-step logic in private or local methods with descriptive names.
- **GUD-002**: Allow some duplication if it improves code clarity and intention (DAMP over DRY in tests).
- **PAT-001**: Use method extraction refactoring to encapsulate repeated or low-level logic.

## 4. Interfaces & Data Contracts

| Interface | Description |
|-----------|-------------|
| `ICodeAnalyzer` | Analyzes syntax trees for DRY/DAMP violations. |
| `DiagnosticResult` | Represents a detected issue, including location, message, and suggested fix. |
| `ICodeFix` | Provides code fix suggestions for detected violations. |

**Example DiagnosticResult:**
```json
{
  "Id": "LINTDRY001",
  "Message": "Duplicate logic detected. Consider extracting to a method.",
  "Location": "MyClass.cs:42-47",
  "Severity": "Warning"
}
```

## 5. Acceptance Criteria

- **AC-001**: Given two or more identical or near-identical code blocks, When analyzed, Then a DRY violation diagnostic is reported.
- **AC-002**: Given a method with low-level implementation details in its body, When analyzed, Then a DAMP violation diagnostic is reported with a suggestion to extract methods.
- **AC-003**: Given a method with a non-descriptive name (e.g., `DoStuff`), When analyzed, Then a diagnostic is reported suggesting a more intention-revealing name.
- **AC-004**: The analyzer shall not report diagnostics for idiomatic or trivial code patterns.

## 6. Test Automation Strategy

- **Test Levels**: Unit (analyzer logic), Integration (Roslyn integration), End-to-End (CI pipeline)
- **Frameworks**: xUnit, Microsoft.CodeAnalysis.Testing
- **Test Data Management**: Use code snippets with known DRY/DAMP violations and compliant examples
- **CI/CD Integration**: Automated analyzer tests in GitHub Actions
- **Coverage Requirements**: 90%+ code coverage for analyzer logic
- **Performance Testing**: Analyzer must process 10k+ LOC projects in under 30s

## 7. Rationale & Context

Encapsulating implementation details in methods with intention-revealing names improves code readability, maintainability, and testability. While DRY reduces duplication, DAMP prioritizes clarity, especially in tests. This analyzer aims to balance both principles, guiding developers to abstract the "how" and express the "what" in their code structure.

## 8. Dependencies & External Integrations

### External Systems
- **EXT-001**: Roslyn Compiler Platform – Required for syntax and semantic analysis

### Third-Party Services
- **SVC-001**: None required

### Infrastructure Dependencies
- **INF-001**: .NET SDK (for analyzer build and test)

### Data Dependencies
- **DAT-001**: None

### Technology Platform Dependencies
- **PLT-001**: C# 8.0+ required for modern syntax analysis

### Compliance Dependencies
- **COM-001**: Must not collect or transmit user code or data

## 9. Examples & Edge Cases

```csharp
// Example: DRY Violation
void Foo() {
    var x = Calculate();
    // ... 10 lines of logic ...
    Log(x);
}
void Bar() {
    var x = Calculate();
    // ... 10 lines of logic ...
    Log(x);
}
// Suggestion: Extract the repeated logic into a method

// Example: DAMP Violation
void ProcessOrder() {
    // low-level steps inline
    Validate();
    Save();
    Notify();
}
// Suggestion: Encapsulate steps in methods with intention-revealing names

// Edge Case: Allow trivial duplication
void Test() {
    Assert.True(a);
    Assert.True(b);
}
// No diagnostic should be reported
```

## 10. Validation Criteria

- Analyzer correctly identifies DRY and DAMP violations as per acceptance criteria
- No false positives for trivial or idiomatic code
- Diagnostics include actionable messages and locations
- Code fixes are offered where feasible
- Analyzer passes all automated tests and performance benchmarks

## 11. Related Specifications / Further Reading

- [Enterprise Craftsmanship: DRY vs DAMP in Unit Tests](https://enterprisecraftsmanship.com/posts/dry-damp-unit-tests/)
- [Lintelligent Analyzer Architecture](../docs/architecture.md)
- [Roslyn Analyzer Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
