# LINT001: Avoid Empty Catch

**Category**: Reliability  
**Severity**: Warning  
**Code Fix**: Yes

## Summary

Detects empty `catch` blocks that swallow exceptions without handling, logging, or rethrowing. Empty catch blocks can hide issues, making debugging difficult and allowing errors to go unnoticed in production.

## Description

Empty `catch` blocks silently consume exceptions and break the observable contract of the code. This rule flags `catch` blocks that contain no statements (or only comments) so that the developer can either handle the exception explicitly, log it, or rethrow it.

Examples of problematic code:

```csharp
try
{
    DoWork();
}
catch
{
    // swallowed
}
```

Or with an empty typed catch:

```csharp
try { DoWork(); }
catch (Exception) { }
```

## Why this matters

- Hidden failures: Exceptions can indicate real runtime problems that should be visible.  
- Silent behavior: Swallowing exceptions can cause incorrect application state and subtle bugs.  
- Observability: Proper logging or rethrowing helps incident investigation and monitoring.

## Solution

Handle the exception explicitly: log it, take corrective action, or rethrow. If you intentionally want to ignore an exception, annotate the intent (and consider logging at a debug level) or use a narrow catch and document the reason.

```csharp
// Log and rethrow
try
{
    DoWork();
}
catch (Exception ex)
{
    Logger.LogError(ex, "DoWork failed");
    throw;
}

// Or handle specific cases explicitly
try
{
    ParseUserInput(input);
}
catch (FormatException ex)
{
    // recoverable: provide default or notify caller
    return DefaultValue;
}
```

## Code Fix

The analyzer ships a code fix that offers to:
- Add a logging statement inside the catch and rethrow (`throw;`).
- Narrow the catch type (if possible) and add an explanatory comment and/or logging.

Apply the code fix when you want a safe and explicit handling strategy instead of silently swallowing exceptions.

## Configuration

Configure severity in `.editorconfig`:

```ini
# Treat as warning
dotnet_diagnostic.LINT001.severity = warning

# Disable
dotnet_diagnostic.LINT001.severity = none
```

Suppress for specific instances:

```csharp
#pragma warning disable LINT001
// intentionally ignoring failure in this rare case
try { DangerousOp(); } catch { }
#pragma warning restore LINT001
```

Or use the suppression attribute:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Lintelligent", "LINT001")]
void LegacyMethod() { try { /*...*/ } catch { } }
```

## Tests and Examples

Unit tests for this rule should show triggering cases (empty catches) and non-triggering cases (catches with logging, rethrowing, or handling). Use the Roslyn testing markup to mark expected diagnostic locations.

## Related Rules

- **LINT002**: Complex Conditionals — related in the sense of maintainability and clarity.  
- **LINT003**: Prefer Option Monad — different focus (absence of value) but similar maintainability goals.

## Further Reading

- Best practices for exception handling in .NET: https://learn.microsoft.com/dotnet/standard/exceptions
- Logging and observability patterns

## Version History

- v0.1.0: Initial documentation
