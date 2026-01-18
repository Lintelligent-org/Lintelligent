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
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.PreferOptionMonadDescription), Resources.ResourceManager, typeof(Resources)),
        helpLinkUri: "https://github.com/Lintelligent-org/Lintelligent/blob/main/docs/analyzers/LINT003.md");

    private readonly ICodeAnalyzer _analyzer = new PreferOptionMonadAnalyzer();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for compilation (runs once per compilation to get semantic model)
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterSyntaxTreeAction(treeContext =>
            {
                var semanticModel = compilationContext.Compilation.GetSemanticModel(treeContext.Tree);
                var diagnostics = _analyzer.Analyze(treeContext.Tree, semanticModel);
                
                foreach (var diagnostic in diagnostics)
                {
                    var location = Location.Create(treeContext.Tree, diagnostic.Span);
                    var roslynDiagnostic = Diagnostic.Create(Rule, location);
                    treeContext.ReportDiagnostic(roslynDiagnostic);
                }
            });
        });
    }
}
