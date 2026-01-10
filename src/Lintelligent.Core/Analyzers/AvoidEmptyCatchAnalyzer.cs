using System.Collections.Generic;
using System.Linq;
using Lintelligent.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.Analyzers
{
    public sealed class AvoidEmptyCatchAnalyzer : ICodeAnalyzer
    {
        public IEnumerable<DiagnosticResult> Analyze(
            SyntaxTree tree,
            SemanticModel semanticModel)
        {
            var root = tree.GetRoot();
            foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
            {
                if (!catchClause.Block.Statements.Any())
                {
                    yield return new DiagnosticResult(
                        "LINT001",
                        "Avoid empty catch blocks",
                        catchClause.CatchKeyword.Span,
                        DiagnosticSeverity.Warning);
                }
            }
        }
    }
}