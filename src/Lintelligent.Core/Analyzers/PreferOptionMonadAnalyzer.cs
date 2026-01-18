using System.Collections.Generic;
using System.Linq;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.Analyzers;

/// <summary>
/// Analyzer that detects methods returning nullable types (T?) and suggests using Option&lt;T&gt; instead.
/// Also detects unsafe null check patterns (?., ??, if != null) and suggests Option methods.
/// </summary>
public sealed class PreferOptionMonadAnalyzer : ICodeAnalyzer
{
    private const string DiagnosticId = "LINT003";
    
    public IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel semanticModel)
    {
        var root = tree.GetRoot();
        var reportedSpans = new HashSet<Microsoft.CodeAnalysis.Text.TextSpan>();
        
        // First, identify methods with nullable return types OR Option<T> returns
        // These will either get a diagnostic (nullable) or should skip null-check diagnostics (Option<T>)
        var methodsToSkipNullChecks = new HashSet<MethodDeclarationSyntax>();
        foreach (var methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var returnType = methodDeclaration.ReturnType;
            if (returnType == null)
                continue;
            
            // Skip null checks in methods with nullable returns
            if (returnType is NullableTypeSyntax || IsSemanticNullableType(returnType, semanticModel))
            {
                methodsToSkipNullChecks.Add(methodDeclaration);
                continue;
            }
            
            // Also skip null checks in methods that already return Option<T>
            var typeInfo = semanticModel.GetTypeInfo(returnType);
            if (typeInfo.Type != null && NullableTypeHelper.IsOptionType(typeInfo.Type))
            {
                methodsToSkipNullChecks.Add(methodDeclaration);
            }
        }
        
        // Detect nullable return types
        foreach (var diagnostic in AnalyzeMethodReturnTypes(root, semanticModel))
        {
            if (reportedSpans.Add(diagnostic.Span))
                yield return diagnostic;
        }
        
        // Detect null-conditional operators (obj?.Property) - but not in methods to skip
        foreach (var diagnostic in AnalyzeNullConditionalOperators(root, semanticModel, methodsToSkipNullChecks))
        {
            if (reportedSpans.Add(diagnostic.Span))
                yield return diagnostic;
        }
        
        // Detect null-coalescing operators (value ?? default) - but not in methods to skip
        foreach (var diagnostic in AnalyzeNullCoalescingOperators(root, semanticModel, methodsToSkipNullChecks))
        {
            if (reportedSpans.Add(diagnostic.Span))
                yield return diagnostic;
        }
        
        // Detect if-null checks (if (x != null)) - but not in methods to skip
        foreach (var diagnostic in AnalyzeIfNullChecks(root, semanticModel, methodsToSkipNullChecks))
        {
            if (reportedSpans.Add(diagnostic.Span))
                yield return diagnostic;
        }
    }
    
    private bool IsSemanticNullableType(TypeSyntax returnType, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(returnType);
        var typeSymbol = typeInfo.Type;
        
        if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Error)
            return false;
        
        // Check for async wrappers with nullable inner type
        return NullableTypeHelper.IsAsyncWrappedNullable(typeSymbol);
    }
    
    /// <summary>
    /// Analyzes method return types for nullable patterns
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeMethodReturnTypes(SyntaxNode root, SemanticModel semanticModel)
    {
        // Find all method declarations
        foreach (var methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // Skip extern methods (no implementation)
            if (methodDeclaration.Modifiers.Any(m => m.Text == "extern"))
                continue;
            
            // Skip partial methods (no implementation here)
            if (methodDeclaration.Modifiers.Any(m => m.Text == "partial") && methodDeclaration.Body == null)
                continue;
            
            var returnType = methodDeclaration.ReturnType;
            if (returnType == null)
                continue;
            
            // FIRST: Check syntax-based patterns (works for generics when semantic model fails)
            // Check if the syntax is a nullable type (T?, string?, int?)
            if (returnType is NullableTypeSyntax nullableSyntax)
            {
                var methodName = methodDeclaration.Identifier.Text;
                var currentReturnType = returnType.ToString();
                var suggestedReturnType = $"Option<{nullableSyntax.ElementType}>";
                
                var message = $"Method '{methodName}' returns nullable type '{currentReturnType}'. Consider using '{suggestedReturnType}' to make absence of value explicit.";
                
                yield return new DiagnosticResult(
                    DiagnosticId,
                    message,
                    returnType.Span,
                    DiagnosticSeverity.Warning);
                
                continue;
            }
            
            // Check for Task<T?> or ValueTask<T?> syntax (async methods with nullable inner type)
            if (returnType is GenericNameSyntax genericSyntax &&
                (genericSyntax.Identifier.Text == "Task" || genericSyntax.Identifier.Text == "ValueTask") &&
                genericSyntax.TypeArgumentList.Arguments.Count == 1 &&
                genericSyntax.TypeArgumentList.Arguments[0] is NullableTypeSyntax innerNullable)
            {
                var methodName = methodDeclaration.Identifier.Text;
                var wrapperType = genericSyntax.Identifier.Text;
                var innerType = innerNullable.ElementType;
                var currentReturnType = returnType.ToString();
                var suggestedReturnType = $"{wrapperType}<Option<{innerType}>>";
                
                var message = $"Method '{methodName}' returns nullable type '{currentReturnType}'. Consider using '{suggestedReturnType}' to make absence of value explicit.";
                
                yield return new DiagnosticResult(
                    DiagnosticId,
                    message,
                    returnType.Span,
                    DiagnosticSeverity.Warning);
                
                continue;
            }
            
            // SECOND: Get the type symbol for semantic analysis
            var typeInfo = semanticModel.GetTypeInfo(returnType);
            var typeSymbol = typeInfo.Type;
            
            if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Error)
                continue;
            
            // Check if already using Option<T>
            if (NullableTypeHelper.IsOptionType(typeSymbol))
                continue;
            
            // Check for Task<T?> or ValueTask<T?> (async methods)
            if (NullableTypeHelper.IsAsyncWrappedNullable(typeSymbol))
            {
                var methodName = methodDeclaration.Identifier.Text;
                var currentReturnType = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var suggestedReturnType = NullableTypeHelper.BuildSuggestedAsyncOptionType(typeSymbol);
                
                var message = $"Method '{methodName}' returns nullable type '{currentReturnType}'. Consider using '{suggestedReturnType}' to make absence of value explicit.";
                
                yield return new DiagnosticResult(
                    DiagnosticId,
                    message,
                    returnType.Span,
                    DiagnosticSeverity.Warning);
                
                continue;
            }
            
            // Check for regular nullable types (T? or Nullable<T>)
            if (NullableTypeHelper.IsNullable(typeSymbol))
            {
                var methodName = methodDeclaration.Identifier.Text;
                var currentReturnType = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var suggestedReturnType = NullableTypeHelper.BuildSuggestedOptionType(typeSymbol);
                
                var message = $"Method '{methodName}' returns nullable type '{currentReturnType}'. Consider using '{suggestedReturnType}' to make absence of value explicit.";
                
                yield return new DiagnosticResult(
                    DiagnosticId,
                    message,
                    returnType.Span,
                    DiagnosticSeverity.Warning);
            }
        }
    }
    
    /// <summary>
    /// Analyzes null-conditional operators (obj?.Property)
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeNullConditionalOperators(SyntaxNode root, SemanticModel semanticModel, HashSet<MethodDeclarationSyntax> methodsWithNullableReturns)
    {
        foreach (var conditionalAccess in root.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>())
        {
            // Skip if inside a method that already has a nullable return type diagnostic
            var containingMethod = conditionalAccess.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod != null && methodsWithNullableReturns.Contains(containingMethod))
                continue;
                
            var message = "Null-conditional operator (?.) detected. Consider using Option.Map or Option.Bind for safer null handling.";
            
            yield return new DiagnosticResult(
                DiagnosticId,
                message,
                conditionalAccess.OperatorToken.Span,
                DiagnosticSeverity.Warning);
        }
    }
    
    /// <summary>
    /// Analyzes null-coalescing operators (value ?? default)
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeNullCoalescingOperators(SyntaxNode root, SemanticModel semanticModel, HashSet<MethodDeclarationSyntax> methodsWithNullableReturns)
    {
        foreach (var coalesceExpression in root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                     .Where(b => b.IsKind(SyntaxKind.CoalesceExpression)))
        {
            // Skip if inside a method that already has a nullable return type diagnostic
            var containingMethod = coalesceExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod != null && methodsWithNullableReturns.Contains(containingMethod))
                continue;
                
            var message = "Null-coalescing operator (??) detected. Consider using Option.IfNone or Option.Match for explicit absence handling.";
            
            yield return new DiagnosticResult(
                DiagnosticId,
                message,
                coalesceExpression.OperatorToken.Span,
                DiagnosticSeverity.Warning);
        }
    }
    
    /// <summary>
    /// Analyzes if-null checks (if (x != null) or if (x == null))
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeIfNullChecks(SyntaxNode root, SemanticModel semanticModel, HashSet<MethodDeclarationSyntax> methodsWithNullableReturns)
    {
        foreach (var binaryExpression in root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                     .Where(b => b.IsKind(SyntaxKind.NotEqualsExpression) || b.IsKind(SyntaxKind.EqualsExpression)))
        {
            // Check if one operand is a null literal
            var isNullCheck = binaryExpression.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                              binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression);
            
            if (!isNullCheck)
                continue;
            
            // Skip if inside a method that already has a nullable return type diagnostic
            var containingMethod = binaryExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod != null && methodsWithNullableReturns.Contains(containingMethod))
                continue;
            
            var message = "Explicit null check detected. Consider using Option.IsSome, Option.IsNone, or Option.Match for type-safe null handling.";
            
            yield return new DiagnosticResult(
                DiagnosticId,
                message,
                binaryExpression.OperatorToken.Span,
                DiagnosticSeverity.Warning);
        }
    }
}
