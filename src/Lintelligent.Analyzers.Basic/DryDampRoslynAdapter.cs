using System.Collections.Immutable;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Basic
{
    /// <summary>
    /// Roslyn adapter for <see cref="DryDampAnalyzer"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DryDampRoslynAdapter : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "LINT002",
            "DRY/DAMP principle violation",
            "{0}",
            "Lintelligent",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Detects code that violates DRY (Don't Repeat Yourself) or DAMP (Descriptive And Meaningful Phrases) principles.");

        private readonly ICodeAnalyzer _analyzer = new DryDampAnalyzer();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for compilation end to analyze entire syntax trees
            context.RegisterCompilationAction(compilationContext =>
            {
                foreach (var tree in compilationContext.Compilation.SyntaxTrees)
                {
                    var semanticModel = compilationContext.Compilation.GetSemanticModel(tree);
                    var diagnostics = _analyzer.Analyze(tree, semanticModel);
                    
                    foreach (var d in diagnostics)
                    {
                        var location = Location.Create(tree, d.Span);
                        compilationContext.ReportDiagnostic(Diagnostic.Create(Rule, location, d.Message));
                    }
                }
            });
        }
    }
}
