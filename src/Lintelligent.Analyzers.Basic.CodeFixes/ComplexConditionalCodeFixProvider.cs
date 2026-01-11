using System.Collections.Immutable;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lintelligent.Analyzers.Basic.CodeFixes
{
    /// <summary>
    /// Code fix provider for ComplexConditionalAnalyzer. Offers to extract nested logic into a new method.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ComplexConditionalCodeFixProvider)), Shared]
    public class ComplexConditionalCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LINT003");

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the conditional statement
            var node = root.FindNode(diagnosticSpan);
            if (node is IfStatementSyntax || node is SwitchStatementSyntax)
            {
                context.RegisterCodeFix(
                    Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                        "Extract nested logic to method",
                        c => ExtractToMethodAsync(context.Document, node, c),
                        nameof(ComplexConditionalCodeFixProvider)),
                    diagnostic);
            }
        }

        private async Task<Document> ExtractToMethodAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            // This is a stub implementation. In a real scenario, you would analyze the nested block and generate a new method.
            // For now, just add a TODO comment above the node.
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var trivia = SyntaxFactory.Comment("// TODO: Consider extracting this nested logic into a separate method for readability.");
            editor.ReplaceNode(node, (n, generator) => n.WithLeadingTrivia(n.GetLeadingTrivia().Add(trivia)));
            return editor.GetChangedDocument();
        }
    }
}
