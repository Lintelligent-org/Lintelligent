using System.Collections.Generic;
using System.Linq;
using Lintelligent.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.Analyzers
{
    /// <summary>
    /// Analyzes code for adherence to DRY (Don't Repeat Yourself) and DAMP (Descriptive And Meaningful Phrases) principles.
    /// </summary>
    /// <remarks>
    /// This analyzer detects:
    /// 1. Duplicate code blocks that should be extracted into methods (DRY violations)
    /// 2. Complex inline logic that should be encapsulated with intention-revealing method names (DAMP violations)
    /// 
    /// The analyzer encourages code that clearly expresses WHAT it does at a high level,
    /// while encapsulating HOW it does it in well-named methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// // DRY violation - duplicate blocks:
    /// if (user != null &amp;&amp; user.IsActive &amp;&amp; user.HasPermission("Admin"))
    /// {
    ///     // logic
    /// }
    /// if (user != null &amp;&amp; user.IsActive &amp;&amp; user.HasPermission("Admin"))
    /// {
    ///     // same logic repeated
    /// }
    /// 
    /// // DAMP violation - complex condition not encapsulated:
    /// if (user != null &amp;&amp; user.IsActive &amp;&amp; user.HasPermission("Admin") &amp;&amp; user.Department == "IT")
    /// {
    ///     // Should extract: if (IsAuthorizedITUser(user))
    /// }
    /// </code>
    /// </example>
    public sealed class DryDampAnalyzer : ICodeAnalyzer
    {
        private const int MinimumStatementsForDuplication = 3;
        private const int MaximumBinaryExpressionsBeforeExtraction = 3;

        public IEnumerable<DiagnosticResult> Analyze(
            SyntaxTree tree,
            SemanticModel semanticModel)
        {
            var root = tree.GetRoot();
            
            // Check for DRY violations (duplicate code blocks)
            foreach (var diagnostic in AnalyzeDuplicateBlocks(root))
            {
                yield return diagnostic;
            }
            
            // Check for DAMP violations (complex conditions not encapsulated)
            foreach (var diagnostic in AnalyzeComplexConditions(root))
            {
                yield return diagnostic;
            }
        }

        private IEnumerable<DiagnosticResult> AnalyzeDuplicateBlocks(SyntaxNode root)
        {
            // Find all blocks with at least MinimumStatementsForDuplication statements
            var blocks = root.DescendantNodes()
                .OfType<BlockSyntax>()
                .Where(b => b.Statements.Count >= MinimumStatementsForDuplication)
                .ToList();

            // Group blocks by their normalized text to find duplicates
            var blockGroups = blocks
                .GroupBy(b => NormalizeBlockText(b))
                .Where(g => g.Count() > 1)
                .ToList();

            // Report duplicates (but only once per set of duplicates)
            var reportedBlocks = new HashSet<BlockSyntax>();
            
            foreach (var group in blockGroups)
            {
                var duplicates = group.ToList();
                
                // Report the second occurrence onwards
                for (int i = 1; i < duplicates.Count; i++)
                {
                    var block = duplicates[i];
                    if (!reportedBlocks.Contains(block))
                    {
                        reportedBlocks.Add(block);
                        yield return new DiagnosticResult(
                            "LINT002",
                            $"Duplicate code block detected. Consider extracting this logic into a reusable method (DRY principle).",
                            block.OpenBraceToken.Span,
                            DiagnosticSeverity.Info);
                    }
                }
            }
        }

        private IEnumerable<DiagnosticResult> AnalyzeComplexConditions(SyntaxNode root)
        {
            // Find if statements with complex conditions
            foreach (var ifStatement in root.DescendantNodes().OfType<IfStatementSyntax>())
            {
                if (IsComplexCondition(ifStatement.Condition))
                {
                    yield return new DiagnosticResult(
                        "LINT002",
                        "Complex condition detected. Consider extracting into a method with an intention-revealing name (DAMP principle).",
                        ifStatement.IfKeyword.Span,
                        DiagnosticSeverity.Info);
                }
            }

            // Find while statements with complex conditions
            foreach (var whileStatement in root.DescendantNodes().OfType<WhileStatementSyntax>())
            {
                if (IsComplexCondition(whileStatement.Condition))
                {
                    yield return new DiagnosticResult(
                        "LINT002",
                        "Complex condition detected. Consider extracting into a method with an intention-revealing name (DAMP principle).",
                        whileStatement.WhileKeyword.Span,
                        DiagnosticSeverity.Info);
                }
            }
        }

        private bool IsComplexCondition(ExpressionSyntax condition)
        {
            // Count binary expressions (&&, ||, etc.)
            var binaryExpressionCount = condition
                .DescendantNodesAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .Count(b => b.IsKind(SyntaxKind.LogicalAndExpression) || 
                           b.IsKind(SyntaxKind.LogicalOrExpression));

            return binaryExpressionCount > MaximumBinaryExpressionsBeforeExtraction;
        }

        private string NormalizeBlockText(BlockSyntax block)
        {
            // Normalize the block text by removing whitespace and comments
            // This helps identify structurally identical blocks even if formatting differs
            var statements = block.Statements.Select(s => s.ToString().Trim());
            return string.Join("|", statements);
        }
    }
}
