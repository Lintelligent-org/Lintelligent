using System.Collections.Immutable;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers.Basic
{
    /// <summary>
    /// Roslyn adapter for <see cref="ComplexConditionalAnalyzer"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComplexConditionalRoslynAdapter : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "LINT002",
            "Excessive conditional nesting",
            "Conditional nesting depth is too high",
            "Lintelligent",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private readonly ICodeAnalyzer _analyzer = new ComplexConditionalAnalyzer();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(ctx =>
            {
                var diagnostics = _analyzer.Analyze(ctx.Node.SyntaxTree, ctx.SemanticModel);
                // Only report diagnostics for the current node to avoid duplicates
                foreach (var d in diagnostics)
                {
                    if (d.Span.Equals(ctx.Node.Span))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(ctx.Node.SyntaxTree, d.Span)));
                    }
                }
            }, SyntaxKind.IfStatement, SyntaxKind.SwitchStatement);
        }
    }
}
