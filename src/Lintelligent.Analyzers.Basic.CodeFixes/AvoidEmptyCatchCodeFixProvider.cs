using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Lintelligent.Core.Abstractions;
using Lintelligent.Core.CodeFixes;
using Microsoft.CodeAnalysis;

public static class Extensions
{
    public static DiagnosticResult ToDiagnosticResult(this Diagnostic diag)
    {
        return new DiagnosticResult(
            diag.Id,
            diag.GetMessage(),
            diag.Location.SourceSpan,
            DiagnosticSeverity.Warning);
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidEmptyCatchProvider)), Shared]
public class AvoidEmptyCatchProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LINT001");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var tree = await context.Document.GetSyntaxTreeAsync(context.CancellationToken);

        var codeFix = new AvoidEmptyCatchCodeFix();

        if (!codeFix.CanFix(diagnostic.ToDiagnosticResult())) return;

        context.RegisterCodeFix(
            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                "Add TODO comment to catch",
                ct => ApplyCoreFix(context.Document, codeFix, diagnostic.ToDiagnosticResult(), ct)),
            diagnostic);
    }

    private async Task<Document> ApplyCoreFix(Document doc, ICodeFix fix, DiagnosticResult diagnostic, CancellationToken ct)
    {
        var tree = await doc.GetSyntaxTreeAsync(ct);
        var result = fix.ApplyFix(diagnostic, tree);
        return doc.WithSyntaxRoot(await result.UpdatedTree.GetRootAsync(ct));
    }
}