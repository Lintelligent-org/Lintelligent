# Code Fix Transformation Examples

**Code Fix ID**: `Lintelligent.ConvertToOptionMonad`  
**Feature**: `001-option-monad-analyzer`  
**Diagnostic**: LINT003

---

## Overview

The Prefer Option Monad code fix automatically transforms methods returning nullable types (`T?`) to return `Option<T>` from the language-ext library. The transformation includes:

1. Changing the method return type from `T?` to `Option<T>`
2. Adding `using LanguageExt;` directive if not present
3. Transforming `return null;` to `return Option<T>.None;`
4. Wrapping non-null returns with `Option.Some(...)`

---

## Example 1: Simple Nullable Reference Type

### Before
```csharp
public string? GetUserName(int userId)
{
    if (userId == 0)
    {
        return null;
    }
    return "Alice";
}
```

### After
```csharp
using LanguageExt;

public Option<string> GetUserName(int userId)
{
    if (userId == 0)
    {
        return Option<string>.None;
    }
    return Option.Some("Alice");
}
```

### Transformations Applied
1. Return type: `string?` → `Option<string>`
2. Using directive: Added `using LanguageExt;`
3. First return: `return null;` → `return Option<string>.None;`
4. Second return: `return "Alice";` → `return Option.Some("Alice");`

---

## Example 2: Nullable Value Type

### Before
```csharp
public int? ParseNumber(string input)
{
    if (string.IsNullOrEmpty(input))
        return null;
    
    return int.Parse(input);
}
```

### After
```csharp
using LanguageExt;

public Option<int> ParseNumber(string input)
{
    if (string.IsNullOrEmpty(input))
        return Option<int>.None;
    
    return Option.Some(int.Parse(input));
}
```

### Transformations Applied
1. Return type: `int?` → `Option<int>`
2. Using directive: Added `using LanguageExt;`
3. First return: `return null;` → `return Option<int>.None;`
4. Second return: `return int.Parse(input);` → `return Option.Some(int.Parse(input));`

---

## Example 3: Generic Method

### Before
```csharp
public T? GetValue<T>(string key) where T : class
{
    if (!cache.TryGetValue(key, out var value))
    {
        return null;
    }
    return value as T;
}
```

### After
```csharp
using LanguageExt;

public Option<T> GetValue<T>(string key) where T : class
{
    if (!cache.TryGetValue(key, out var value))
    {
        return Option<T>.None;
    }
    return Option.Some(value as T);
}
```

### Transformations Applied
1. Return type: `T?` → `Option<T>`
2. Using directive: Added `using LanguageExt;`
3. Type parameter constraint unchanged: `where T : class`
4. First return: `return null;` → `return Option<T>.None;`
5. Second return: `return value as T;` → `return Option.Some(value as T);`

---

## Example 4: Async Method (Task<T?>)

### Before
```csharp
public async Task<string?> GetUserNameAsync(int userId)
{
    var user = await database.GetUserAsync(userId);
    if (user == null)
        return null;
    
    return user.Name;
}
```

### After
```csharp
using LanguageExt;

public async Task<Option<string>> GetUserNameAsync(int userId)
{
    var user = await database.GetUserAsync(userId);
    if (user == null)
        return Option<string>.None;
    
    return Option.Some(user.Name);
}
```

### Transformations Applied
1. Return type: `Task<string?>` → `Task<Option<string>>`
2. Using directive: Added `using LanguageExt;`
3. `async` keyword preserved
4. First return: `return null;` → `return Option<string>.None;`
5. Second return: `return user.Name;` → `return Option.Some(user.Name);`

---

## Example 5: Expression-Bodied Member

### Before
```csharp
public string? GetDefaultName() => null;
```

### After
```csharp
using LanguageExt;

public Option<string> GetDefaultName() => Option<string>.None;
```

### Transformations Applied
1. Return type: `string?` → `Option<string>`
2. Using directive: Added `using LanguageExt;`
3. Arrow expression: `null` → `Option<string>.None`

---

## Example 6: Multiple Return Paths

### Before
```csharp
public int? Divide(int numerator, int denominator)
{
    if (denominator == 0)
        return null;
    
    if (numerator == 0)
        return 0;
    
    return numerator / denominator;
}
```

### After
```csharp
using LanguageExt;

public Option<int> Divide(int numerator, int denominator)
{
    if (denominator == 0)
        return Option<int>.None;
    
    if (numerator == 0)
        return Option.Some(0);
    
    return Option.Some(numerator / denominator);
}
```

### Transformations Applied
1. Return type: `int?` → `Option<int>`
2. Using directive: Added `using LanguageExt;`
3. First return: `return null;` → `return Option<int>.None;`
4. Second return: `return 0;` → `return Option.Some(0);`
5. Third return: `return numerator / denominator;` → `return Option.Some(numerator / denominator);`

---

## Example 7: Try-Catch with Returns

### Before
```csharp
public string? ReadFile(string path)
{
    try
    {
        return File.ReadAllText(path);
    }
    catch
    {
        return null;
    }
}
```

### After
```csharp
using LanguageExt;

public Option<string> ReadFile(string path)
{
    try
    {
        return Option.Some(File.ReadAllText(path));
    }
    catch
    {
        return Option<string>.None;
    }
}
```

### Transformations Applied
1. Return type: `string?` → `Option<string>`
2. Using directive: Added `using LanguageExt;`
3. Try block return: `return File.ReadAllText(path);` → `return Option.Some(File.ReadAllText(path));`
4. Catch block return: `return null;` → `return Option<string>.None;`

---

## Example 8: Existing Using Directive

### Before
```csharp
using System;
using System.Linq;

public string? GetName() => null;
```

### After
```csharp
using System;
using System.Linq;
using LanguageExt;

public Option<string> GetName() => Option<string>.None;
```

### Note
The `using LanguageExt;` directive is added only if not already present. The code fix checks for existing using directives before adding.

---

## Example 9: Nested Returns (Switch Expression)

### Before
```csharp
public string? GetStatus(int code) => code switch
{
    0 => null,
    1 => "Active",
    2 => "Inactive",
    _ => null
};
```

### After
```csharp
using LanguageExt;

public Option<string> GetStatus(int code) => code switch
{
    0 => Option<string>.None,
    1 => Option.Some("Active"),
    2 => Option.Some("Inactive"),
    _ => Option<string>.None
};
```

### Transformations Applied
1. Return type: `string?` → `Option<string>`
2. Using directive: Added `using LanguageExt;`
3. Each switch arm transformed:
   - `null` → `Option<string>.None`
   - String literal → `Option.Some(literal)`

---

## Example 10: ValueTask<T?>

### Before
```csharp
public async ValueTask<int?> GetCountAsync()
{
    await Task.Delay(100);
    return null;
}
```

### After
```csharp
using LanguageExt;

public async ValueTask<Option<int>> GetCountAsync()
{
    await Task.Delay(100);
    return Option<int>.None;
}
```

### Transformations Applied
1. Return type: `ValueTask<int?>` → `ValueTask<Option<int>>`
2. Using directive: Added `using LanguageExt;`
3. Return statement: `return null;` → `return Option<int>.None;`

---

## Complex Example: Repository Pattern

### Before
```csharp
using System.Threading.Tasks;

public class UserRepository
{
    private readonly Database _db;
    
    public async Task<User?> FindByIdAsync(int id)
    {
        if (id <= 0)
            return null;
        
        var user = await _db.Users.FindAsync(id);
        return user;
    }
    
    public User? FindByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return null;
        
        try
        {
            return _db.Users.FirstOrDefault(u => u.Email == email);
        }
        catch
        {
            return null;
        }
    }
}
```

### After
```csharp
using System.Threading.Tasks;
using LanguageExt;

public class UserRepository
{
    private readonly Database _db;
    
    public async Task<Option<User>> FindByIdAsync(int id)
    {
        if (id <= 0)
            return Option<User>.None;
        
        var user = await _db.Users.FindAsync(id);
        return Option.Some(user);
    }
    
    public Option<User> FindByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return Option<User>.None;
        
        try
        {
            return Option.Some(_db.Users.FirstOrDefault(u => u.Email == email));
        }
        catch
        {
            return Option<User>.None;
        }
    }
}
```

---

## Edge Cases

### Edge Case 1: Conditional Return Expression

**Before**:
```csharp
public string? GetName(bool condition) => condition ? "Alice" : null;
```

**After**:
```csharp
using LanguageExt;

public Option<string> GetName(bool condition) => 
    condition ? Option.Some("Alice") : Option<string>.None;
```

### Edge Case 2: Null-Coalescing in Return

**Before**:
```csharp
public string? GetName() => cache.Get("name") ?? null;
```

**After**:
```csharp
using LanguageExt;

public Option<string> GetName() => 
    Option.Some(cache.Get("name") ?? throw new Exception());
// Note: This edge case may need manual review as null-coalescing with null is unusual
```

### Edge Case 3: Multiple Methods in Same File

**Before**:
```csharp
public class Service
{
    public string? Method1() => null;
    public int? Method2() => null;
}
```

**After** (applying code fix to both):
```csharp
using LanguageExt;

public class Service
{
    public Option<string> Method1() => Option<string>.None;
    public Option<int> Method2() => Option<int>.None;
}
```

**Note**: The using directive is added only once, even when fixing multiple methods.

---

## Transformation Rules Summary

| Original Pattern | Transformed Pattern |
|-----------------|-------------------|
| `T?` (return type) | `Option<T>` |
| `Nullable<T>` (return type) | `Option<T>` |
| `Task<T?>` (return type) | `Task<Option<T>>` |
| `ValueTask<T?>` (return type) | `ValueTask<Option<T>>` |
| `return null;` | `return Option<T>.None;` |
| `return value;` | `return Option.Some(value);` |
| `=> null` | `=> Option<T>.None` |
| `=> value` | `=> Option.Some(value)` |
| (No `using LanguageExt;`) | Add `using LanguageExt;` |

---

## Code Fix Behavior

### Automatic Transformations
- ✅ Return type replacement
- ✅ All return statements transformed
- ✅ Using directive added if needed
- ✅ Preserves XML documentation comments
- ✅ Preserves method attributes
- ✅ Maintains code formatting

### Manual Review Recommended
- ⚠️ Calling code may need updates to handle Option<T>
- ⚠️ Interface implementations may require interface changes
- ⚠️ Complex expressions in return statements (edge cases)

---

**Status**: ✅ CODE FIX EXAMPLES COMPLETE
