using Lintelligent.Core.Abstractions;
using Lintelligent.Core.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lintelligent.Core.Test.CodeFixes;

public class AvoidEmptyCatchCodeFixTests
{
    [Fact]
    public void AddsTodoCommentInEmptyCatch()
    {
        var code = """

                               class Program
                               {
                                   void Test()
                                   {
                                       try { } catch { }
                                   }
                               }
                   """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var diagnostic = new DiagnosticResult(
            "LINT001", "Avoid empty catch blocks",
            tree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>()
                .First().CatchKeyword.Span,
            DiagnosticSeverity.Warning);

        var fix = new AvoidEmptyCatchCodeFix();
        var result = fix.ApplyFix(diagnostic, tree);

        var newCode = result.UpdatedTree.GetText().ToString();
        Assert.Contains("// TODO: handle exception", newCode);
    }
}