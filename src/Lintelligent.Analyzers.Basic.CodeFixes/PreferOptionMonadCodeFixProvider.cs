using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.Analyzers.Basic.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferOptionMonadCodeFixProvider)), Shared]
public class PreferOptionMonadCodeFixProvider : CodeFixProvider
{
    private const string Title = "Transform to Option<T>";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LINT003");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var tree = await context.Document.GetSyntaxTreeAsync(context.CancellationToken);

        if (tree == null) return;

        var codeFix = new PreferOptionMonadCodeFix();
        var diagnosticResult = diagnostic.ToDiagnosticResult();

        if (!codeFix.CanFix(diagnosticResult)) return;

        context.RegisterCodeFix(
            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                Title,
                ct => ApplyCoreFix(context.Document, codeFix, diagnosticResult, ct),
                equivalenceKey: nameof(PreferOptionMonadCodeFixProvider)),
            diagnostic);
    }

    private async Task<Document> ApplyCoreFix(
        Document document, 
        ICodeFix fix, 
        DiagnosticResult diagnostic, 
        CancellationToken cancellationToken)
    {
        var tree = await document.GetSyntaxTreeAsync(cancellationToken);
        if (tree == null) return document;

        var result = fix.ApplyFix(diagnostic, tree);
        var newRoot = await result.UpdatedTree.GetRootAsync(cancellationToken);
        
        return document.WithSyntaxRoot(newRoot);
    }
}

public static class DiagnosticExtensions
{
    public static DiagnosticResult ToDiagnosticResult(this Diagnostic diagnostic)
    {
        return new DiagnosticResult(
            diagnostic.Id,
            diagnostic.GetMessage(),
            diagnostic.Location.SourceSpan,
            diagnostic.Severity);
    }
}
