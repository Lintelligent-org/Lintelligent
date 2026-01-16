# LINT003: Prefer Option Monad

**Category**: Maintainability  
**Severity**: Info  
**Code Fix**: Yes  

## Summary

Detects methods returning nullable types (`T?` or `Nullable<T>`) and suggests using the `Option<T>` monad pattern from LanguageExt to make the absence of value explicit and type-safe.

## Description

Nullable reference types and nullable value types (`T?`) can lead to null reference exceptions if not handled carefully. The Option monad pattern provides a safer, more functional approach to representing optional values by making the "absence of value" explicit in the type system.

This analyzer identifies methods that return nullable types and suggests transforming them to return `Option<T>` instead, which forces callers to handle both the "Some" and "None" cases explicitly.

## Problem

```csharp
// Nullable reference type
string? GetUserName(int userId)
{
    if (userId == 0) return null;
    return "John Doe";
}

// Nullable value type
int? GetAge(int userId)
{
    if (userId == 0) return null;
    return 25;
}
```

**Issues with this approach:**
- Callers may forget to check for null
- NullReferenceException can occur at runtime
- The "absence of value" is not explicit in the API contract
- Defensive null checks clutter the codebase

## Solution

```csharp
using LanguageExt;

// Using Option<T> monad
Option<string> GetUserName(int userId)
{
    if (userId == 0) return Option<string>.None;
    return Option<string>.Some("John Doe");
}

// Using Option<T> for value types
Option<int> GetAge(int userId)
{
    if (userId == 0) return Option<int>.None;
    return Option<int>.Some(25);
}
```

**Benefits:**
- **Type Safety**: The absence of value is encoded in the type system
- **Explicit Handling**: Callers must handle both Some and None cases
- **No Null Exceptions**: Impossible to get NullReferenceException from Option
- **Functional Composition**: Options can be mapped, filtered, and composed functionally
- **Clear Intent**: The API contract explicitly states the value may be absent

## When to Use

Use `Option<T>` when:
- A method may legitimately not return a value (not an error condition)
- You want to force callers to handle the "no value" case explicitly
- You're adopting a functional programming style in your codebase
- You want to eliminate null reference exceptions

## When NOT to Use

Avoid `Option<T>` when:
- You're working with existing APIs that expect nullable types
- Performance is critical (Option adds minimal overhead but is not zero-cost)
- Your team is unfamiliar with functional programming concepts
- You're interfacing with third-party libraries that don't support Option

## Code Fix

The analyzer provides an automatic code fix that performs the following transformations:

1. **Changes return type** from `T?` to `Option<T>`
2. **Transforms null returns** to `Option<T>.None`
3. **Transforms value returns** to `Option<T>.Some(value)`
4. **Adds using directive** for `LanguageExt` if not present

### Before

```csharp
class UserService
{
    string? FindUserEmail(int userId)
    {
        if (userId <= 0)
            return null;
        
        return "user@example.com";
    }
}
```

### After (with code fix applied)

```csharp
using LanguageExt;

class UserService
{
    Option<string> FindUserEmail(int userId)
    {
        if (userId <= 0)
            return Option<string>.None;
        
        return Option<string>.Some("user@example.com");
    }
}
```

## Working with Option<T>

### Pattern Matching

```csharp
var email = userService.FindUserEmail(userId);

var result = email.Match(
    Some: addr => $"Email: {addr}",
    None: () => "No email found"
);
```

### Map and Bind

```csharp
var upperEmail = userService.FindUserEmail(userId)
    .Map(email => email.ToUpper())
    .Match(
        Some: e => e,
        None: () => "NO EMAIL"
    );
```

### Default Value

```csharp
var email = userService.FindUserEmail(userId)
    .IfNone("default@example.com");
```

## Configuration

### Severity

You can configure the severity in `.editorconfig`:

```ini
# Treat as warning
dotnet_diagnostic.LINT003.severity = warning

# Treat as error
dotnet_diagnostic.LINT003.severity = error

# Disable
dotnet_diagnostic.LINT003.severity = none
```

### Suppression

Suppress for specific instances:

```csharp
#pragma warning disable LINT003
string? GetLegacyValue() => null;
#pragma warning restore LINT003
```

Or use attribute:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Lintelligent", "LINT003")]
string? GetLegacyValue() => null;
```

## Dependencies

This analyzer requires the [LanguageExt.Core](https://www.nuget.org/packages/LanguageExt.Core/) package:

```bash
dotnet add package LanguageExt.Core
```

## Examples

### Example 1: Simple Transformation

**Before:**
```csharp
public class ProductService
{
    decimal? GetPrice(int productId)
    {
        if (productId == 0) return null;
        return 29.99m;
    }
}
```

**After:**
```csharp
using LanguageExt;

public class ProductService
{
    Option<decimal> GetPrice(int productId)
    {
        if (productId == 0) return Option<decimal>.None;
        return Option<decimal>.Some(29.99m);
    }
}
```

### Example 2: Multiple Returns

**Before:**
```csharp
public DateTime? GetLastLoginDate(int userId)
{
    if (userId <= 0) return null;
    if (!IsActiveUser(userId)) return null;
    return DateTime.Now.AddDays(-7);
}
```

**After:**
```csharp
using LanguageExt;

public Option<DateTime> GetLastLoginDate(int userId)
{
    if (userId <= 0) return Option<DateTime>.None;
    if (!IsActiveUser(userId)) return Option<DateTime>.None;
    return Option<DateTime>.Some(DateTime.Now.AddDays(-7));
}
```

### Example 3: Consuming Option Values

**Before (unsafe):**
```csharp
var price = productService.GetPrice(productId);
if (price.HasValue)
{
    Console.WriteLine($"Price: ${price.Value}");
}
else
{
    Console.WriteLine("Price not available");
}
```

**After (safe):**
```csharp
var price = productService.GetPrice(productId);
price.Match(
    Some: p => Console.WriteLine($"Price: ${p}"),
    None: () => Console.WriteLine("Price not available")
);
```

## Related Rules

- **LINT001**: Avoid Empty Catch - Encourages proper exception handling
- **LINT002**: Complex Conditionals - Suggests simplifying complex conditional logic

## Further Reading

- [LanguageExt Documentation](https://github.com/louthy/language-ext)
- [Option Type on Wikipedia](https://en.wikipedia.org/wiki/Option_type)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Functional Error Handling in C#](https://mikhail.io/2016/01/functional-error-handling/)

## Version History

- **v0.2.0** (2026-01): Initial release with code fix support
  - Transforms nullable reference types to Option<T>
  - Transforms nullable value types to Option<T>
  - Automatic using directive injection
  - Comprehensive return statement transformation
