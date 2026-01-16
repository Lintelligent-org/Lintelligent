using System.Collections.Immutable;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Basic
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidEmptyCatchRoslynAdapter : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "LINT001",
            "Avoid empty catch blocks",
            "Avoid empty catch blocks",
            "Lintelligent",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Detects empty catch blocks that swallow exceptions without handling or logging.",
            helpLinkUri: "https://github.com/Lintelligent-org/Lintelligent/blob/main/docs/analyzers/LINT001.md");

        private readonly ICodeAnalyzer _analyzer = new AvoidEmptyCatchAnalyzer();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(ctx =>
            {
                var diagnostics = _analyzer.Analyze(ctx.Node.SyntaxTree, ctx.SemanticModel);
                foreach (var d in diagnostics)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(ctx.Node.SyntaxTree, d.Span)));
                }
            }, SyntaxKind.CatchClause);
        }
    }
}