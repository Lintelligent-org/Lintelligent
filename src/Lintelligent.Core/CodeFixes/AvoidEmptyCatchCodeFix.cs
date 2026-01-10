using System.Linq;
using Lintelligent.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.CodeFixes
{
    public sealed class AvoidEmptyCatchCodeFix : ICodeFix
    {
        public bool CanFix(DiagnosticResult diagnostic) => diagnostic.Id == "LINT001";

        public CodeFixResult ApplyFix(DiagnosticResult diagnostic, SyntaxTree tree)
        {
            var root = tree.GetRoot();
            var tokenSpan = diagnostic.Span;

            // Find the catch clause node by span
            var catchNode = root.DescendantNodes()
                .OfType<CatchClauseSyntax>()
                .FirstOrDefault(c => c.CatchKeyword.Span.Equals(tokenSpan));

            if (catchNode == null) return new CodeFixResult(tree);

            // Insert a comment inside the empty catch block
            var newBlock = catchNode.Block.WithStatements(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ParseStatement("// TODO: handle exception\n")));

            var newCatch = catchNode.WithBlock(newBlock);
            var newRoot = root.ReplaceNode(catchNode, newCatch);

            return new CodeFixResult(tree.WithRootAndOptions(newRoot, tree.Options));
        }
    }
}