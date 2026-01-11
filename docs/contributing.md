# Contributing to Lintelligent

Thank you for your interest in contributing to **Lintelligent**! We welcome all contributions—code, documentation, tests, and ideas. Please read these guidelines to help us maintain a high-quality, secure, and collaborative project.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Commit Messages](#commit-messages)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Code Review](#code-review)
- [Troubleshooting](#troubleshooting)
- [Resources](#resources)

## Code of Conduct

By participating, you agree to follow our [Code of Conduct](https://github.com/Lintelligent-org/Lintelligent/blob/main/CODE_OF_CONDUCT.md).

## Getting Started

1. **Fork the repository** and clone it locally.
2. **Install .NET SDK 8.0+** ([Download here](https://dotnet.microsoft.com/download)).
3. **Restore dependencies:**
	 ```sh
	 dotnet restore
	 ```
4. **Build the solution:**
	 ```sh
	 dotnet build
	 ```
5. **Run tests:**
	 ```sh
	 dotnet test
	 ```

See [docs/architecture.md](architecture.md) for project structure and design.

## How to Contribute

- **Bug Reports:** Open an issue with clear steps to reproduce.
- **Feature Requests:** Suggest enhancements via issues.
- **Pull Requests:**
	- Branch from `main`.
	- Follow the [Coding Standards](#coding-standards).
	- Add or update tests for your changes.
	- Ensure all tests pass before submitting.

## Coding Standards

- Follow C# best practices and [project conventions](architecture.md).
- Write secure code (see [security-and-owasp.instructions.md](../.github/instructions/security-and-owasp.instructions.md)).
- Use clear, descriptive names and comments.
- Keep code modular and well-documented.
- For analyzers, follow the [Adapter pattern](architecture.md#three-layer-design).

## Commit Messages

- Use concise, imperative language (e.g., "Fix analyzer bug").
- Reference issues when relevant (e.g., `Fixes #42`).
- Group related changes in a single commit.

## Testing

- Add or update unit tests for all new features and bug fixes.
- Use xUnit and Roslyn testing framework.
- Run all tests with:
	```sh
	dotnet test
	```
- Mark expected diagnostics with `[| |]` in test code.

## Pull Request Process

1. Ensure your branch is up to date with `main`.
2. Open a pull request with a clear description of your changes.
3. Link related issues.
4. The CI/CD pipeline will run tests and checks automatically.
5. Address any review feedback promptly.

## Code Review

- Reviews focus on correctness, security, clarity, and maintainability.
- Be open to feedback and iterate as needed.
- All code must pass tests and adhere to project standards before merging.

## Troubleshooting

- If you encounter build or test issues, check:
	- .NET SDK version
	- Dependency restores
	- [docs/architecture.md](architecture.md) for project layout
- For help, open an issue or start a discussion.

## Resources

- [Project Architecture](architecture.md)
- [Roadmap](roadmap.md)
- [Performance Guidelines](../.github/instructions/performance-optimization.instructions.md)
- [Security Guidelines](../.github/instructions/security-and-owasp.instructions.md)
- [AI Prompt Engineering](../.github/instructions/ai-prompt-engineering-safety-best-practices.instructions.md)

---

Thank you for helping make Lintelligent better!
