# Quickstart: Prefer Option Monad Analyzer (LINT003)

**Feature**: `001-option-monad-analyzer`  
**Analyzer**: Prefer Option<T> over nullable types  
**Code Fix**: Convert to Option<T>

---

## Installation

### 1. Install Lintelligent Analyzers

```bash
dotnet add package Lintelligent.Analyzers.Basic
```

### 2. Install language-ext

The analyzer requires the language-ext library for the `Option<T>` type:

```bash
dotnet add package LanguageExt.Core
```

**Minimum Version**: language-ext 4.4.0+

### 3. Enable Nullable Reference Types (C# 8.0+)

In your `.csproj` file:

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

---

## Quick Example

### The Problem

Returning `null` to represent the absence of a value can lead to null reference exceptions:

```csharp
public string? GetUserName(int userId)
{
    if (userId == 0)
        return null;  // ⚠️ LINT003: Consider using Option<string>
    
    return "Alice";
}

// Calling code - easy to forget null check
var name = GetUserName(0);
Console.WriteLine(name.Length); // 💥 NullReferenceException!
```

### The Solution

Use `Option<T>` to make the possibility of absence explicit:

```csharp
using LanguageExt;

public Option<string> GetUserName(int userId)
{
    if (userId == 0)
        return Option<string>.None;  // ✅ Explicit absence
    
    return Option.Some("Alice");
}

// Calling code - compiler forces you to handle both cases
var name = GetUserName(0).Match(
    Some: value => value,
    None: () => "Unknown"  // Can't forget to handle None!
);
Console.WriteLine(name.Length); // ✅ Safe - never null
```

---

## Using the Analyzer

### Detection

The analyzer automatically detects methods returning nullable types:

```csharp
// ⚠️ LINT003 on each method
public string? GetName() => null;
public int? GetAge() => null;
public async Task<User?> GetUserAsync(int id) => null;
```

You'll see a blue squiggle (Info severity) under the return type with the message:

> Method 'GetName' returns nullable type 'string?'. Consider using 'Option<string>' to make absence of value explicit.

### Applying the Code Fix

**In Visual Studio**:
1. Click on the lightbulb/screwdriver icon (or press `Ctrl+.`)
2. Select **"Convert to Option<T>"**
3. The analyzer transforms the method automatically

**In Rider**:
1. Press `Alt+Enter` on the diagnostic
2. Select **"Convert to Option<T>"**
3. The analyzer transforms the method automatically

**In VS Code** (with C# Dev Kit):
1. Click on the lightbulb icon
2. Select **"Convert to Option<T>"**
3. The analyzer transforms the method automatically

---

## What the Code Fix Does

The code fix performs the following transformations:

1. **Changes return type**: `T?` → `Option<T>`
2. **Adds using directive**: `using LanguageExt;` (if not present)
3. **Transforms null returns**: `return null;` → `return Option<T>.None;`
4. **Wraps non-null returns**: `return value;` → `return Option.Some(value);`

**Before**:
```csharp
public string? FindUser(int id)
{
    if (id < 0) return null;
    return database.GetUser(id);
}
```

**After** (one click):
```csharp
using LanguageExt;

public Option<string> FindUser(int id)
{
    if (id < 0) return Option<string>.None;
    return Option.Some(database.GetUser(id));
}
```

---

## Working with Option<T>

### Creating Option Values

```csharp
// Create Some
var some = Option.Some("Alice");
var some2 = "Alice".ToSome(); // Extension method

// Create None
var none = Option<string>.None;

// From nullable
string? nullableValue = GetNullableString();
var option = nullableValue.ToOption(); // Some if not null, None otherwise
```

### Consuming Option Values

#### Pattern Matching (Recommended)

```csharp
var result = GetUserName(userId).Match(
    Some: name => $"Hello, {name}!",
    None: () => "Hello, guest!"
);
```

#### With Default Value

```csharp
var name = GetUserName(userId).IfNone("Unknown");
```

#### Checking if Value Exists

```csharp
var option = GetUserName(userId);

if (option.IsSome)
{
    var value = option.IfNone(""); // Safe - we checked IsSome
    Console.WriteLine(value);
}
```

#### Mapping

```csharp
var upperName = GetUserName(userId)
    .Map(name => name.ToUpper())
    .IfNone("UNKNOWN");
```

#### Chaining (Bind)

```csharp
var userDetails = GetUserId(email)
    .Bind(id => GetUser(id))
    .Bind(user => GetUserDetails(user.Id))
    .IfNone(new UserDetails());
```

---

## Configuration

### Adjusting Severity

Create or edit `.editorconfig` in your project root:

```ini
[*.cs]

# Treat as warning (yellow squiggle)
dotnet_diagnostic.LINT003.severity = warning

# Treat as error (red squiggle, breaks build)
dotnet_diagnostic.LINT003.severity = error

# Disable analyzer
dotnet_diagnostic.LINT003.severity = none

# Suggestion only (no squiggle, visible in Error List)
dotnet_diagnostic.LINT003.severity = suggestion
```

### Per-File Suppression

```csharp
#pragma warning disable LINT003
public string? GetName() => null; // No diagnostic
#pragma warning restore LINT003
```

### Per-Method Suppression

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "LINT003")]
public string? GetName() => null; // No diagnostic
```

### Excluding Test Files

```ini
# .editorconfig
[**/*Tests.cs]
dotnet_diagnostic.LINT003.severity = none
```

---

## Common Scenarios

### Scenario 1: Repository Pattern

```csharp
using LanguageExt;

public class UserRepository
{
    public Option<User> FindById(int id)
    {
        var user = database.Users.FirstOrDefault(u => u.Id == id);
        return user == null 
            ? Option<User>.None 
            : Option.Some(user);
    }
    
    public async Task<Option<User>> FindByEmailAsync(string email)
    {
        var user = await database.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        
        return user.ToOption(); // Extension method
    }
}
```

### Scenario 2: Parsing

```csharp
using LanguageExt;

public Option<int> ParseInt(string input)
{
    return int.TryParse(input, out var result)
        ? Option.Some(result)
        : Option<int>.None;
}

// Usage
var value = ParseInt("123").IfNone(0);
```

### Scenario 3: Configuration/Settings

```csharp
using LanguageExt;

public Option<string> GetConfigValue(string key)
{
    return configuration.TryGetValue(key, out var value)
        ? Option.Some(value)
        : Option<string>.None;
}

// Usage
var apiKey = GetConfigValue("ApiKey").Match(
    Some: key => key,
    None: () => throw new InvalidOperationException("API key not configured")
);
```

---

## Best Practices

### ✅ DO: Use Match for Safe Consumption

```csharp
var greeting = GetUserName(id).Match(
    Some: name => $"Hello, {name}!",
    None: () => "Hello, guest!"
);
```

### ✅ DO: Chain Operations with Map/Bind

```csharp
var result = GetUser(id)
    .Map(user => user.Email)
    .Map(email => email.ToLower())
    .IfNone("no-email@example.com");
```

### ✅ DO: Use IfNone for Simple Defaults

```csharp
var count = GetCount().IfNone(0);
```

### ❌ DON'T: Access .Value Directly (Unsafe)

```csharp
var option = GetUserName(id);
var name = ((Some<string>)option).Value; // ⚠️ Throws if None!
```

### ❌ DON'T: Mix Option with Null

```csharp
// Bad - defeats the purpose of Option
public Option<string> GetName()
{
    return null; // ⚠️ Compiler error - Option is a struct
}
```

---

## Integration with Existing Code

### Gradual Migration

You don't need to convert your entire codebase at once. Start with:

1. New code: Use `Option<T>` for all nullable returns
2. High-risk code: Convert methods prone to null reference exceptions
3. Public APIs: Gradually convert public method signatures

### Interoperability with Nullable Types

```csharp
// Convert nullable to Option
string? nullableString = GetNullableString();
var option = nullableString.ToOption();

// Convert Option to nullable
Option<string> option = GetOptionString();
string? nullable = option.IfNoneUnsafe(null); // Returns null if None
```

---

## Performance Considerations

- `Option<T>` is a **struct** (value type) - no heap allocation
- Minimal overhead compared to `T?`
- Encourages functional patterns that often perform better (no null checks)

---

## Troubleshooting

### Issue: "LanguageExt namespace not found"

**Solution**: Ensure language-ext NuGet package is installed:
```bash
dotnet add package LanguageExt.Core
```

### Issue: "Option<T> not recognized in code fix"

**Solution**: Ensure you have nullable reference types enabled:
```xml
<Nullable>enable</Nullable>
```

### Issue: "Calling code breaks after applying code fix"

**Solution**: Update calling code to handle `Option<T>`:

**Before**:
```csharp
var name = GetName();
if (name != null) { /* ... */ }
```

**After**:
```csharp
GetName().IfSome(name => { /* ... */ });
// or
var name = GetName().IfNone("default");
```

---

## Further Reading

- [language-ext Documentation](https://github.com/louthy/language-ext)
- [Option Monad Explained](https://github.com/louthy/language-ext/wiki/Option)
- [Lintelligent LINT003 Reference](../../docs/analyzers/LINT003.md)

---

**Status**: ✅ QUICKSTART COMPLETE

Ready to start using Option<T> instead of nullable types! 🎉
