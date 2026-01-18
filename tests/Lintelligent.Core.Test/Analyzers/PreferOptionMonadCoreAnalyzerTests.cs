using System.Linq;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.Core.Test.Analyzers;

/// <summary>
/// Tests for the Core PreferOptionMonadAnalyzer to ensure it returns
/// Warning severity (not Info) which was a critical bug that prevented
/// diagnostics from appearing in build output.
/// </summary>
public class PreferOptionMonadCoreAnalyzerTests
{
    [Fact]
    public void CoreAnalyzer_ShouldReturnWarningSeverity_NotInfo()
    {
        // This test ensures the Core analyzer returns Warning severity
        // Regression test for bug where severity was hardcoded to Info
        var code = """
            class Program
            {
                int? GetValue()
                {
                    return null;
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var analyzer = new PreferOptionMonadAnalyzer();

        var diagnostics = analyzer.Analyze(syntaxTree, semanticModel).ToList();

        Assert.NotEmpty(diagnostics);
        var diagnostic = diagnostics.First();
        
        // CRITICAL: Must be Warning, not Info
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void CoreAnalyzer_AllDiagnosticsShouldBeWarning()
    {
        // Ensure ALL diagnostics from the analyzer are Warning severity
        var code = """
            class Program
            {
                int? GetValue() => null;
                string? GetName() => null;
                T? Load<T>(string fileName) where T : class => null;
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var analyzer = new PreferOptionMonadAnalyzer();

        var diagnostics = analyzer.Analyze(syntaxTree, semanticModel).ToList();

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, d => 
            Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
    }

    [Fact]
    public void CoreAnalyzer_DetectsGenericNullable()
    {
        // Regression test for generic nullable detection
        var code = """
            class Program
            {
                T? Load<T>(string fileName) where T : class
                {
                    return null;
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var analyzer = new PreferOptionMonadAnalyzer();

        var diagnostics = analyzer.Analyze(syntaxTree, semanticModel).ToList();

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("Option"));
    }

    [Fact]
    public void CoreAnalyzer_MessageShouldNotContainPlaceholders()
    {
        // Regression test for message format issues
        var code = """
            class Program
            {
                int? GetValue() => null;
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var analyzer = new PreferOptionMonadAnalyzer();

        var diagnostics = analyzer.Analyze(syntaxTree, semanticModel).ToList();

        Assert.NotEmpty(diagnostics);
        foreach (var diagnostic in diagnostics)
        {
            Assert.DoesNotContain("{0}", diagnostic.Message);
            Assert.DoesNotContain("{1}", diagnostic.Message);
            Assert.DoesNotContain("{2}", diagnostic.Message);
        }
    }
}
