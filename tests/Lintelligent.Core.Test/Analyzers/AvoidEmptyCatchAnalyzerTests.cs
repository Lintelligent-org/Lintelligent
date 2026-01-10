using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.Core.Test.Analyzers;

public class AvoidEmptyCatchAnalyzerTests
{
    [Fact]
    public void DetectsEmptyCatch()
    {
        const string code = """

                                        class Program
                                        {
                                            void Test()
                                            {
                                                try { } 
                                                catch { }
                                            }
                                        }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [tree]);
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new AvoidEmptyCatchAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Single(diagnostics);
        Assert.Equal("LINT001", diagnostics[0].Id);
    }
}