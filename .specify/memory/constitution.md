<!--
SYNC IMPACT REPORT (Constitution v1.0.0)
========================================
Version Change: INITIAL → 1.0.0
Ratification Date: 2026-01-11
Last Amendment: 2026-01-11

Modified Principles: N/A (Initial creation)
Added Sections:
  - Core Principles (5 principles)
  - Quality Gates
  - Development Workflow
  - Governance

Templates Status:
  ✅ plan-template.md - Aligned (Constitution Check section references this file)
  ✅ spec-template.md - Aligned (User stories support independent testing)
  ✅ tasks-template.md - Aligned (Phase structure supports layered development)
  ⚠️  checklist-template.md - Generic template, no specific alignment needed
  ⚠️  agent-file-template.md - No agent-specific references found

Follow-up TODOs:
  - None identified

Principles Rationale:
  1. Layered Architecture - Reflects the Core/Adapter/CodeFix separation
  2. Test-First Development - Aligns with TDD mandate in contributing guide
  3. Semantic Versioning - Codifies existing VERSIONING.md practices
  4. Framework Agnostic Core - Critical for architecture integrity
  5. Public API Stability - Supports long-term ecosystem trust
-->

# Lintelligent Constitution

## Core Principles

### I. Layered Architecture (NON-NEGOTIABLE)

All analyzer and code fix implementations MUST follow the three-layer design:

1. **Core Layer** (`Lintelligent.Core`): Framework-agnostic business logic implementing `ICodeAnalyzer` and `ICodeFix` interfaces. MUST NOT depend on Roslyn infrastructure, IDE APIs, IO, networking, or licensing.
2. **Adapter Layer** (`Lintelligent.Analyzers.Basic`): Roslyn `DiagnosticAnalyzer` adapters that wrap Core analyzers and bridge to Roslyn infrastructure.
3. **CodeFix Layer** (`Lintelligent.Analyzers.Basic.CodeFixes`): Roslyn `CodeFixProvider` adapters that wrap Core code fixes.

**Rationale**: This separation ensures testability without Roslyn dependencies, enables future platform portability, and maintains clear boundaries between business logic and integration concerns.

**Validation**: Every new analyzer MUST have a corresponding Core implementation, Roslyn adapter, and independent unit tests for the Core logic.

### II. Test-First Development (NON-NEGOTIABLE)

All features and bug fixes MUST follow Test-Driven Development (TDD):

1. Write failing tests first (Red)
2. Implement minimum code to pass tests (Green)
3. Refactor while keeping tests passing (Refactor)

**Rationale**: TDD ensures code correctness, prevents regressions, and documents expected behavior. For analyzers, tests define diagnostic expectations before implementation.

**Validation**: Pull requests MUST include test evidence showing the Red-Green-Refactor cycle. Tests MUST use xUnit and Microsoft.CodeAnalysis.Testing framework with `[| |]` markup for diagnostic locations.

### III. Semantic Versioning (STRICTLY ENFORCED)

All releases MUST follow [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR (X.0.0)**: Breaking changes to public API, analyzer behavior, or diagnostic IDs
- **MINOR (0.X.0)**: New analyzers, code fixes, or backward-compatible features
- **PATCH (0.0.X)**: Bug fixes, performance improvements, documentation updates

**Rationale**: Predictable versioning builds ecosystem trust and prevents breaking changes for consumers.

**Validation**: Version bumps MUST be justified in pull request descriptions with explicit categorization (MAJOR/MINOR/PATCH). Breaking changes require deprecation warnings for at least one MINOR version before removal.

### IV. Framework-Agnostic Core (NON-NEGOTIABLE)

`Lintelligent.Core` MUST remain independent of:

- Roslyn analyzer infrastructure (`DiagnosticAnalyzer`, `CodeFixProvider`)
- IDE-specific APIs (Visual Studio, VS Code, Rider)
- File system IO, networking, telemetry, or licensing
- Commercial or host-specific logic

**Rationale**: Core purity enables independent testing, CLI reuse, and future commercial expansion without architectural rewrites.

**Validation**: Code reviews MUST reject any Core dependencies on prohibited libraries. All Core tests MUST run without Roslyn infrastructure initialization.

### V. Public API Stability

Once a public API surface is released in a stable version (≥1.0.0):

- Public interfaces, classes, and methods are IMMUTABLE unless marked obsolete
- New members MAY be added (MINOR version bump)
- Obsolete members MUST remain functional for at least one MINOR version
- Breaking changes require MAJOR version bump and migration guide

**Rationale**: API stability is critical for ecosystem trust, NuGet package adoption, and long-term project credibility.

**Validation**: Public API changes MUST be reviewed by maintainers. Deprecation warnings MUST include alternative API recommendations and removal timeline.

## Quality Gates

All code MUST pass these gates before merging:

1. **Build**: Solution builds without errors on .NET SDK 8.0+
2. **Tests**: All unit and integration tests pass
3. **Coverage**: New code MUST have ≥80% test coverage
4. **Conventions**: Diagnostic IDs follow `LINT###` format; file naming follows project conventions
5. **Documentation**: Public APIs have XML documentation; non-obvious logic has inline comments
6. **Security**: Code follows OWASP best practices (see `.github/instructions/security-and-owasp.instructions.md`)

## Development Workflow

1. **Branch Naming**: `feature/###-description`, `bugfix/###-description`, `docs/###-description`
2. **Commits**: Use imperative mood (e.g., "Add empty catch analyzer"). Reference issues (`Fixes #42`)
3. **Pull Requests**:
   - Include clear description linking to issues/specs
   - Show test evidence (before/after)
   - Address all CI/CD checks and review feedback
4. **Code Review**: All code requires approval from at least one maintainer focusing on correctness, security, clarity, and architecture alignment
5. **Merging**: Squash or rebase to maintain clean history

## Governance

**Authority**: This constitution supersedes all other development practices, guidelines, and conventions. In case of conflict, this document prevails.

**Amendments**: Changes to this constitution require:
1. Documented rationale and impact analysis
2. Approval from project maintainers
3. Version bump following semantic versioning rules
4. Sync across dependent artifacts (templates, docs, copilot-instructions.md)

**Compliance**: All pull requests and code reviews MUST verify adherence to these principles. Violations MUST be documented and justified before merging, or rejected outright for NON-NEGOTIABLE principles.

**Runtime Guidance**: For AI-assisted development, see `.github/copilot-instructions.md` for detailed architectural context and coding patterns.

**Version**: 1.0.0 | **Ratified**: 2026-01-11 | **Last Amended**: 2026-01-11
