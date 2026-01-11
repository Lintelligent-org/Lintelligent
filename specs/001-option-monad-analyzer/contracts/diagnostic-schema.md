# LINT003 Diagnostic Contract

**Analyzer**: Prefer Option Monad Analyzer  
**Diagnostic ID**: LINT003  
**Feature**: `001-option-monad-analyzer`

---

## Diagnostic Metadata

| Property | Value |
|----------|-------|
| **ID** | LINT003 |
| **Title** | Prefer Option<T> over nullable types |
| **Category** | Design |
| **Default Severity** | Info |
| **Enabled by Default** | Yes |
| **Description** | Using Option<T> from language-ext makes the possibility of absence explicit in the type system, reducing null reference exceptions. |
| **Help Link** | https://github.com/Lintelligent-org/Lintelligent/blob/main/docs/analyzers/LINT003.md |

---

## Message Format

```
Method '{methodName}' returns nullable type '{currentType}'. Consider using '{suggestedType}' to make absence of value explicit.
```

### Message Parameters

1. **{methodName}** - The name of the method being analyzed (e.g., "GetUserName", "FindUser")
2. **{currentType}** - The current nullable return type (e.g., "string?", "int?", "Task<string?>")
3. **{suggestedType}** - The suggested Option-based type (e.g., "Option<string>", "Option<int>", "Task<Option<string>>")

### Message Examples

| Current Type | Method Name | Message |
|-------------|-------------|---------|
| `string?` | GetName | Method 'GetName' returns nullable type 'string?'. Consider using 'Option<string>' to make absence of value explicit. |
| `int?` | ParseNumber | Method 'ParseNumber' returns nullable type 'int?'. Consider using 'Option<int>' to make absence of value explicit. |
| `Task<User?>` | FindUserAsync | Method 'FindUserAsync' returns nullable type 'Task<User?>'. Consider using 'Task<Option<User>>' to make absence of value explicit. |

---

## Detection Rules

### Rule 1: Nullable Reference Types (C# 8.0+)

**Trigger**: Method return type has `NullableAnnotation.Annotated`

**Example**:
```csharp
public string? GetName() // ← LINT003 diagnostic
{
    return null;
}
```

**Detection Logic**:
```csharp
var typeSymbol = semanticModel.GetTypeInfo(returnType).Type;
if (typeSymbol?.NullableAnnotation == NullableAnnotation.Annotated)
{
    // Report LINT003
}
```

### Rule 2: Nullable Value Types

**Trigger**: Method return type is `Nullable<T>` (shorthand `T?` for value types)

**Example**:
```csharp
public int? GetAge() // ← LINT003 diagnostic
{
    return null;
}
```

**Detection Logic**:
```csharp
if (typeSymbol is INamedTypeSymbol namedType &&
    namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
{
    // Report LINT003
}
```

### Rule 3: Generic Methods with Nullable Type Parameters

**Trigger**: Generic method where type parameter has nullable annotation

**Example**:
```csharp
public T? GetValue<T>() where T : class // ← LINT003 diagnostic
{
    return null;
}
```

**Detection Logic**:
```csharp
if (typeSymbol is ITypeParameterSymbol typeParam &&
    typeParam.NullableAnnotation == NullableAnnotation.Annotated)
{
    // Report LINT003
}
```

### Rule 4: Wrapped Nullable Types (Task<T?>)

**Trigger**: Async method return type wraps a nullable type

**Example**:
```csharp
public async Task<string?> GetNameAsync() // ← LINT003 diagnostic
{
    return await Task.FromResult<string?>(null);
}
```

**Detection Logic**:
```csharp
if (typeSymbol is INamedTypeSymbol namedType &&
    (namedType.Name == "Task" || namedType.Name == "ValueTask") &&
    namedType.TypeArguments.Length == 1)
{
    var wrappedType = namedType.TypeArguments[0];
    if (IsNullable(wrappedType)) // Check using rules 1-3
    {
        // Report LINT003
    }
}
```

---

## Exclusion Rules

### Exclusion 1: Non-Nullable Types

**Skip**: Methods returning non-nullable types

**Example**:
```csharp
public string GetName() // ← NO diagnostic
{
    return "Alice";
}
```

### Exclusion 2: Void Methods

**Skip**: Methods with void return type

**Example**:
```csharp
public void DoSomething() // ← NO diagnostic
{
}
```

### Exclusion 3: Already Using Option<T>

**Skip**: Methods already returning Option<T>

**Example**:
```csharp
public Option<string> GetName() // ← NO diagnostic
{
    return Option<string>.None;
}
```

**Detection Logic**:
```csharp
if (typeSymbol is INamedTypeSymbol namedType &&
    namedType.Name == "Option" &&
    namedType.ContainingNamespace.ToString() == "LanguageExt")
{
    // Skip - already using Option
}
```

### Exclusion 4: Interface Implementations

**Skip** (Optional): Explicit interface implementations that must return nullable types

**Example**:
```csharp
public interface IRepository
{
    string? FindUser(int id); // Interface contract
}

public class Repository : IRepository
{
    public string? FindUser(int id) // ← Could be excluded (configurable)
    {
        return null;
    }
}
```

**Rationale**: Changing interface implementations may require changing the interface itself, which could be out of the developer's control.

**Configuration**:
```ini
# .editorconfig
dotnet_diagnostic.LINT003.exclude_interface_implementations = true
```

### Exclusion 5: Partial Methods and Extern Methods

**Skip**: Partial method declarations without implementation and extern methods

**Example**:
```csharp
partial string? GetName(); // ← NO diagnostic (no implementation)

[DllImport("native.dll")]
extern string? GetNativeName(); // ← NO diagnostic (extern)
```

---

## Diagnostic Location

The diagnostic is reported on the **return type** of the method declaration.

**Example**:
```csharp
public [|string?|] GetName() // Diagnostic spans "string?"
{
    return null;
}
```

**Location Calculation**:
```csharp
var location = returnTypeSyntax.GetLocation();
var span = returnTypeSyntax.Span;
```

---

## Severity Configuration

### Default Severity: Info

Rationale: Option monad usage is a code style/design preference, not a correctness issue. Developers can escalate if desired.

### Configurable Severities

**Via .editorconfig**:
```ini
# Treat as warning
dotnet_diagnostic.LINT003.severity = warning

# Treat as error (strict enforcement)
dotnet_diagnostic.LINT003.severity = error

# Disable
dotnet_diagnostic.LINT003.severity = none

# Suggestion only (no squiggle)
dotnet_diagnostic.LINT003.severity = suggestion
```

**Per-File Suppression**:
```csharp
#pragma warning disable LINT003
public string? GetName() => null; // No diagnostic
#pragma warning restore LINT003
```

**Attribute Suppression**:
```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "LINT003")]
public string? GetName() => null; // No diagnostic
```

---

## Related Diagnostics

| Diagnostic | Relationship |
|-----------|--------------|
| CS8600 (Converting null literal) | LINT003 prevents null assignments that would trigger CS8600 |
| CS8603 (Possible null reference return) | LINT003 addresses the same issue with Option<T> pattern |
| CA1062 (Validate arguments) | Both promote null safety, but LINT003 focuses on return types |

---

## Code Fix Association

**Code Fix ID**: `Lintelligent.ConvertToOptionMonad`  
**Title**: "Convert to Option<T>"  
**Equivalence Key**: `LINT003_ConvertToOption`

The code fix is offered when LINT003 is reported. See [codefix-examples.md](codefix-examples.md) for transformation details.

---

## Testing Contract

### Test Categories

1. **Detection Tests**: Verify diagnostic is reported correctly
2. **Exclusion Tests**: Verify no diagnostic for excluded scenarios
3. **Location Tests**: Verify diagnostic location is precise
4. **Severity Tests**: Verify configurable severity works
5. **Edge Case Tests**: Verify handling of complex scenarios

### Test Markup

Use `[| |]` to mark expected diagnostic location:

```csharp
var test = """
    public [|string?|] GetName() => null;
    """;
```

### Test Matrix

| Scenario | Expected Result |
|----------|----------------|
| `string? GetName()` | ✅ Report LINT003 |
| `int? GetAge()` | ✅ Report LINT003 |
| `Task<string?>` | ✅ Report LINT003 |
| `T? GetValue<T>() where T : class` | ✅ Report LINT003 |
| `string GetName()` | ❌ No diagnostic |
| `Option<string> GetName()` | ❌ No diagnostic |
| `void DoSomething()` | ❌ No diagnostic |
| `partial string? GetName();` | ❌ No diagnostic |

---

**Status**: ✅ DIAGNOSTIC CONTRACT COMPLETE
