using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Lintelligent.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Basic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferOptionMonadRoslynAdapter : DiagnosticAnalyzer
{
    private const string DiagnosticId = "LINT003";
    
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: new LocalizableResourceString(nameof(Resources.PreferOptionMonadTitle), Resources.ResourceManager, typeof(Resources)),
        messageFormat: new LocalizableResourceString(nameof(Resources.PreferOptionMonadMessageFormat), Resources.ResourceManager, typeof(Resources)),
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.PreferOptionMonadDescription), Resources.ResourceManager, typeof(Resources)),
        helpLinkUri: "https://github.com/Lintelligent-org/Lintelligent/blob/main/docs/analyzers/LINT003.md");

    private readonly ICodeAnalyzer _analyzer = new PreferOptionMonadAnalyzer();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for method declarations (nullable return types)
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
        
        // Register for null-conditional operators (obj?.Property)
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.ConditionalAccessExpression);
        
        // Register for null-coalescing operators (value ?? default)
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.CoalesceExpression);
        
        // Register for null checks (if (x != null) or if (x == null))
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.NotEqualsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.EqualsExpression);
    }
    
    private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext ctx)
    {
        // IMPORTANT: Only analyze the current node to prevent duplicate diagnostics
        // We don't call _analyzer.Analyze() which analyzes the entire tree
        // Instead, we analyze only the node that triggered this action
        
        var node = ctx.Node;
        var semanticModel = ctx.SemanticModel;
        IEnumerable<DiagnosticResult> diagnostics = new List<DiagnosticResult>();
        
        // Route to the appropriate analyzer method based on node kind
        switch (node.Kind())
        {
            case SyntaxKind.MethodDeclaration:
                // Analyze this specific method for nullable return type
                if (node is MethodDeclarationSyntax method)
                {
                    diagnostics = AnalyzeSpecificMethod(method, semanticModel);
                }
                break;
                
            case SyntaxKind.ConditionalAccessExpression:
            case SyntaxKind.CoalesceExpression:
            case SyntaxKind.NotEqualsExpression:
            case SyntaxKind.EqualsExpression:
                // Phase 6 null check patterns - not yet implemented in adapter
                // Skip for now to prevent duplicate diagnostics
                diagnostics = Enumerable.Empty<DiagnosticResult>();
                break;
        }
        
        foreach (var diagnostic in diagnostics)
        {
            var location = Location.Create(ctx.Node.SyntaxTree, diagnostic.Span);
            var roslynDiagnostic = Diagnostic.Create(Rule, location, diagnostic.Message);
            ctx.ReportDiagnostic(roslynDiagnostic);
        }
    }
    
    /// <summary>
    /// Analyzes a specific method declaration for nullable return types
    /// </summary>
    private IEnumerable<DiagnosticResult> AnalyzeSpecificMethod(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        // Skip extern methods (no implementation)
        if (method.Modifiers.Any(m => m.Text == "extern"))
            yield break;
        
        var returnType = method.ReturnType;
        var methodReturnTypeSymbol = semanticModel.GetTypeInfo(returnType).Type;
        
        if (methodReturnTypeSymbol == null)
            yield break;
        
        // Check syntax for nullable reference types (T?) or nullable value types (T?)
        // This is more reliable than semantic model for nullable reference types
        bool isSyntacticallyNullable = returnType is NullableTypeSyntax;
        
        // Also check for async wrapped nullable: Task<T?> or ValueTask<T?>
        bool isAsyncWrappedNullable = false;
        if (returnType is GenericNameSyntax generic &&
            (generic.Identifier.Text == "Task" || generic.Identifier.Text == "ValueTask") &&
            generic.TypeArgumentList.Arguments.Count == 1)
        {
            isAsyncWrappedNullable = generic.TypeArgumentList.Arguments[0] is NullableTypeSyntax;
        }
        
        // Check semantic model for Nullable<T> (value types without ? syntax)
        bool isSemanticNullable = NullableTypeHelper.IsNullable(methodReturnTypeSymbol);
        bool isSemanticAsyncNullable = NullableTypeHelper.IsAsyncWrappedNullable(methodReturnTypeSymbol);
        
        // Combine checks: either syntax shows nullable OR semantic model shows nullable
        bool isNullable = isSyntacticallyNullable || isSemanticNullable;
        bool isAsync = isAsyncWrappedNullable || isSemanticAsyncNullable;
        
        if (!isNullable && !isAsync)
            yield break;
        
        // Skip if already using Option<T>
        if (NullableTypeHelper.IsOptionType(methodReturnTypeSymbol))
            yield break;
        
        // Build diagnostic message
        var suggestedOptionType = isAsync
            ? NullableTypeHelper.BuildSuggestedAsyncOptionType(methodReturnTypeSymbol)
            : NullableTypeHelper.BuildSuggestedOptionType(methodReturnTypeSymbol);
        
        var message = $"Method '{method.Identifier.Text}' returns nullable type '{methodReturnTypeSymbol}'. " +
                      $"Consider using '{suggestedOptionType}' to make absence of value explicit.";
        
        yield return new DiagnosticResult(DiagnosticId, message, returnType.Span, DiagnosticSeverity.Info);
    }
}
