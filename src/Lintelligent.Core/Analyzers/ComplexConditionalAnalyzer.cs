using System.Collections.Generic;
using System.Linq;
using Lintelligent.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.Analyzers
{
    /// <summary>
    /// Analyzes conditional statements to detect excessive nesting (more than 3 levels deep).
    /// </summary>
    /// <remarks>
    /// This analyzer identifies if/switch statements nested deeper than 3 levels, which can reduce code readability and maintainability.
    /// It ignores else-if chains (not true nesting).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Triggers diagnostic:
    /// if (a)
    /// {
    ///     if (b)
    ///     {
    ///         if (c)
    ///         {
    ///             if (d) { }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class ComplexConditionalAnalyzer : ICodeAnalyzer
    {
        private const int MaxNestingDepth = 3;

        public IEnumerable<DiagnosticResult> Analyze(
            SyntaxTree tree,
            SemanticModel semanticModel)
        {
            var root = tree.GetRoot();
            foreach (var conditional in root.DescendantNodes().OfType<IfStatementSyntax>().Cast<SyntaxNode>()
                .Concat(root.DescendantNodes().OfType<SwitchStatementSyntax>()))
            {
                // Count nesting depth, ignoring else-if chains
                var depth = 1;
                var current = conditional.Parent;
                while (current != null)
                {
                    if (current is IfStatementSyntax || current is SwitchStatementSyntax)
                    {
                        // Ignore else-if chains
                        if (current is IfStatementSyntax ifStmt &&
                            ifStmt.Parent is ElseClauseSyntax elseClause &&
                            elseClause.Statement == ifStmt)
                        {
                            // Not true nesting, skip
                        }
                        else
                        {
                            depth++;
                        }
                    }
                    current = current.Parent;
                }
                if (depth > MaxNestingDepth)
                {
                    yield return new DiagnosticResult(
                        "LINT003",
                        $"Conditional nesting depth is {depth} (max: {MaxNestingDepth}). Consider extracting nested logic into separate methods or using guard clauses.",
                        conditional.GetLocation().SourceSpan,
                        DiagnosticSeverity.Warning);
                }
            }
        }
    }
}
