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
        
        // Detect nullable return types
        foreach (var diagnostic in AnalyzeMethodReturnTypes(root, semanticModel))
            yield return diagnostic;
        
        // Detect null-conditional operators (obj?.Property)
        foreach (var diagnostic in AnalyzeNullConditionalOperators(root, semanticModel))
            yield return diagnostic;
        
        // Detect null-coalescing operators (value ?? default)
        foreach (var diagnostic in AnalyzeNullCoalescingOperators(root, semanticModel))
            yield return diagnostic;
        
        // Detect if-null checks (if (x != null))
        foreach (var diagnostic in AnalyzeIfNullChecks(root, semanticModel))
            yield return diagnostic;
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
                    DiagnosticSeverity.Info);
                
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
                    DiagnosticSeverity.Info);
                
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
                    DiagnosticSeverity.Info);
                
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
                    DiagnosticSeverity.Info);
            }
        }
    }
    
    /// <summary>
    /// Analyzes null-conditional operators (obj?.Property)
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeNullConditionalOperators(SyntaxNode root, SemanticModel semanticModel)
    {
        foreach (var conditionalAccess in root.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>())
        {
            var message = "Null-conditional operator (?.) detected. Consider using Option.Map or Option.Bind for safer null handling.";
            
            yield return new DiagnosticResult(
                DiagnosticId,
                message,
                conditionalAccess.OperatorToken.Span,
                DiagnosticSeverity.Info);
        }
    }
    
    /// <summary>
    /// Analyzes null-coalescing operators (value ?? default)
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeNullCoalescingOperators(SyntaxNode root, SemanticModel semanticModel)
    {
        foreach (var coalesceExpression in root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                     .Where(b => b.IsKind(SyntaxKind.CoalesceExpression)))
        {
            var message = "Null-coalescing operator (??) detected. Consider using Option.IfNone or Option.Match for explicit absence handling.";
            
            yield return new DiagnosticResult(
                DiagnosticId,
                message,
                coalesceExpression.OperatorToken.Span,
                DiagnosticSeverity.Info);
        }
    }
    
    /// <summary>
    /// Analyzes if-null checks (if (x != null) or if (x == null))
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeIfNullChecks(SyntaxNode root, SemanticModel semanticModel)
    {
        foreach (var binaryExpression in root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                     .Where(b => b.IsKind(SyntaxKind.NotEqualsExpression) || b.IsKind(SyntaxKind.EqualsExpression)))
        {
            // Check if one operand is a null literal
            var isNullCheck = binaryExpression.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                              binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression);
            
            if (!isNullCheck)
                continue;
            
            var message = "Explicit null check detected. Consider using Option.IsSome, Option.IsNone, or Option.Match for type-safe null handling.";
            
            yield return new DiagnosticResult(
                DiagnosticId,
                message,
                binaryExpression.OperatorToken.Span,
                DiagnosticSeverity.Info);
        }
    }
}
