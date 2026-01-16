using System.Collections.Immutable;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Basic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferOptionMonadRoslynAdapter : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "LINT003",
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
        var diagnostics = _analyzer.Analyze(ctx.Node.SyntaxTree, ctx.SemanticModel);
        foreach (var diagnostic in diagnostics)
        {
            var location = Location.Create(ctx.Node.SyntaxTree, diagnostic.Span);
            var roslynDiagnostic = Diagnostic.Create(Rule, location, diagnostic.Message);
            ctx.ReportDiagnostic(roslynDiagnostic);
        }
    }
}
