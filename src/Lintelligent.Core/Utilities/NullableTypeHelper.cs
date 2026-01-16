using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.Utilities;

/// <summary>
/// Helper class for detecting and analyzing nullable types in C# code
/// </summary>
public static class NullableTypeHelper
{
    /// <summary>
    /// Determines if a type symbol represents a nullable type (either T? reference type or Nullable&lt;T&gt; value type)
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <returns>True if the type is nullable; otherwise false</returns>
    public static bool IsNullable(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        // Check for nullable reference types (string?, MyClass?)
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            return true;

        // Check for nullable value types (int?, DateTime?)
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return true;

        return false;
    }

    /// <summary>
    /// Extracts the inner type from a nullable type (T from T?)
    /// </summary>
    /// <param name="typeSymbol">The nullable type symbol</param>
    /// <returns>The inner type symbol, or null if not a nullable type</returns>
    public static ITypeSymbol? GetInnerType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return null;

        // For nullable reference types, the type itself is the inner type
        // (string? is just string with NullableAnnotation.Annotated)
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // Return the type with NullableAnnotation.NotAnnotated
            return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        // For nullable value types (Nullable<T>), extract the type argument
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments.Length > 0 ? namedType.TypeArguments[0] : null;
        }

        return null;
    }

    /// <summary>
    /// Determines if a type is Task&lt;T?&gt; or ValueTask&lt;T?&gt; (async methods returning nullable types)
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check</param>
    /// <returns>True if the type is an async-wrapped nullable type</returns>
    public static bool IsAsyncWrappedNullable(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        // Check for Task<T> or ValueTask<T>
        var typeName = namedType.OriginalDefinition.ToString();
        if (typeName != "System.Threading.Tasks.Task<T>" &&
            typeName != "System.Threading.Tasks.ValueTask<T>")
            return false;

        // Check if the type argument is nullable
        if (namedType.TypeArguments.Length == 0)
            return false;

        var innerType = namedType.TypeArguments[0];
        return IsNullable(innerType);
    }

    /// <summary>
    /// Extracts the inner nullable type from Task&lt;T?&gt; or ValueTask&lt;T?&gt;
    /// </summary>
    /// <param name="typeSymbol">The async type symbol</param>
    /// <returns>The nullable inner type (T?), or null if not an async-wrapped nullable</returns>
    public static ITypeSymbol? GetAsyncWrappedNullableType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return null;

        var typeName = namedType.OriginalDefinition.ToString();
        if (typeName != "System.Threading.Tasks.Task<T>" &&
            typeName != "System.Threading.Tasks.ValueTask<T>")
            return null;

        if (namedType.TypeArguments.Length == 0)
            return null;

        var innerType = namedType.TypeArguments[0];
        return IsNullable(innerType) ? innerType : null;
    }

    /// <summary>
    /// Determines if the method is already returning an Option type from language-ext
    /// </summary>
    /// <param name="typeSymbol">The return type symbol</param>
    /// <returns>True if the type is already Option&lt;T&gt;</returns>
    public static bool IsOptionType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        // Check for LanguageExt.Option<T>
        var fullName = namedType.OriginalDefinition.ToString();
        return fullName == "LanguageExt.Option<A>" ||
               fullName.StartsWith("LanguageExt.Option<");
    }

    /// <summary>
    /// Gets the method return type syntax from a method declaration
    /// </summary>
    /// <param name="methodDeclaration">The method declaration syntax</param>
    /// <returns>The return type syntax</returns>
    public static TypeSyntax? GetMethodReturnType(MethodDeclarationSyntax? methodDeclaration)
    {
        return methodDeclaration?.ReturnType;
    }

    /// <summary>
    /// Builds the suggested Option type name from a nullable type
    /// </summary>
    /// <param name="typeSymbol">The nullable type symbol</param>
    /// <returns>The suggested Option type string (e.g., "Option&lt;string&gt;")</returns>
    public static string BuildSuggestedOptionType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return "Option<T>";

        var innerType = GetInnerType(typeSymbol);
        if (innerType == null)
            return "Option<T>";

        // Get the display name (e.g., "string", "List<int>")
        var innerTypeName = innerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        return $"Option<{innerTypeName}>";
    }

    /// <summary>
    /// Builds the suggested async Option type name (Task&lt;Option&lt;T&gt;&gt; or ValueTask&lt;Option&lt;T&gt;&gt;)
    /// </summary>
    /// <param name="typeSymbol">The async-wrapped nullable type symbol</param>
    /// <returns>The suggested async Option type string</returns>
    public static string BuildSuggestedAsyncOptionType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return "Task<Option<T>>";

        var typeName = namedType.OriginalDefinition.ToString();
        var isValueTask = typeName == "System.Threading.Tasks.ValueTask<T>";
        var wrapperType = isValueTask ? "ValueTask" : "Task";

        var nullableInnerType = GetAsyncWrappedNullableType(typeSymbol);
        if (nullableInnerType == null)
            return $"{wrapperType}<Option<T>>";

        var innerType = GetInnerType(nullableInnerType);
        if (innerType == null)
            return $"{wrapperType}<Option<T>>";

        var innerTypeName = innerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return $"{wrapperType}<Option<{innerTypeName}>>";
    }
}
