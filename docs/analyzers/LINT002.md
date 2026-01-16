# LINT002: Complex Conditionals

**Category**: Maintainability  
**Severity**: Info  
**Code Fix**: Yes

## Summary

Flags overly complex conditional expressions that reduce readability, increase the risk of logic errors, and make maintenance harder. The rule highlights nested boolean expressions, large combined conditions, and convoluted use of logical operators that can be simplified.

## Description

Complex conditionals are often hard to reason about and easy to get wrong. This analyzer detects conditional expressions that exceed a reasonable complexity threshold (e.g., many `&&`/`||` segments, deeply nested ternaries, or mixed null checks and comparisons) and suggests refactorings to increase clarity.

Problematic examples:

```csharp
if ((a != null && a.IsValid()) || (b != null && (b.Count > 0 && !b.IsEmpty())) && !flag)
{
    // complicated branch
}
```

Or large chained ternaries:

```csharp
var result = cond1 ? (cond2 ? v1 : v2) : (cond3 ? v3 : v4);
```

## Why this matters

- Readability: Simpler conditions are easier to review and reason about.  
- Fewer bugs: Complex expressions are more likely to contain logic errors.  
- Testability: Simple guards and small methods are easier to test individually.

## Suggested Fixes

Recommended refactorings include:

- Extract boolean sub-expressions into well-named local variables or methods (guard clauses).  
- Use early returns / guard clauses to flatten nesting.  
- Replace complex ternaries with clear `if`/`else` blocks or small helper methods.  
- Use pattern matching where it makes intent clearer.

Before:

```csharp
if (user != null && user.IsActive && (user.Role == Role.Admin || user.Role == Role.Manager) && !user.IsLocked)
    DoWork();
```

After (refactored):

```csharp
bool IsPrivileged(User u) => u.Role == Role.Admin || u.Role == Role.Manager;

if (user == null) return;
if (!user.IsActive) return;
if (user.IsLocked) return;
if (!IsPrivileged(user)) return;

DoWork();
```

## Code Fix

The code fix suggests common refactorings such as extracting sub-expressions to local variables or proposing a named local function. It will not automatically perform large structural refactors but can convert some inline boolean segments into named locals to improve readability.

## Configuration

Control the severity in `.editorconfig`:

```ini
# Treat as info (default)
dotnet_diagnostic.LINT002.severity = info

# Treat as warning
dotnet_diagnostic.LINT002.severity = warning

# Disable
dotnet_diagnostic.LINT002.severity = none
```

Suppress for specific instances:

```csharp
#pragma warning disable LINT002
// complex legacy logic left intentionally
#pragma warning restore LINT002
```

## Tests and Examples

Tests should include examples of nested conditions that trigger diagnostics and equivalent refactored code that does not trigger the rule. Use the Roslyn test harness to assert diagnostic placements and code fix behavior where extraction is supported.

## Related Rules

- **LINT001**: Avoid Empty Catch — both rules improve maintainability and correctness.  
- **LINT003**: Prefer Option Monad — reduces conditional complexity when dealing with optional values.

## Further Reading

- Clean Code: Functions and small helpers  
- Refactoring patterns: Extract Method, Introduce Explaining Variable

## Version History

- v0.1.0: Initial documentation
