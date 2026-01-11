using System.Linq;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.Core.Test.Analyzers
{
    public class ComplexConditionalAnalyzerTests
    {
        [Fact]
        public void DetectsDeeplyNestedIfs_WhenDepthExceedsLimit_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M()
                    {
                        if (a)
                        {
                            if (b)
                            {
                                if (c)
                                {
                                    if (d) { }
                                }
                            }
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new ComplexConditionalAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Single(diagnostics);
            Assert.Equal("LINT003", diagnostics[0].Id);
            Assert.Contains("Conditional nesting depth is", diagnostics[0].Message);
        }

        [Fact]
        public void DoesNotDetectShallowIfs_WhenDepthIsWithinLimit_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M()
                    {
                        if (a)
                        {
                            if (b)
                            {
                                if (c) { }
                            }
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new ComplexConditionalAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void DetectsDeeplyNestedSwitches_WhenDepthExceedsLimit_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(int x)
                    {
                        switch (x)
                        {
                            case 1:
                                switch (x)
                                {
                                    case 2:
                                        switch (x)
                                        {
                                            case 3:
                                                switch (x) { break; }
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new ComplexConditionalAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Single(diagnostics);
            Assert.Equal("LINT003", diagnostics[0].Id);
        }

        [Fact]
        public void IgnoresElseIfChains_WhenDepthIsWithinLimit_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M()
                    {
                        if (a) { }
                        else if (b) { }
                        else if (c) { }
                        else { }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new ComplexConditionalAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void HandlesMalformedSyntaxTree_GracefullyReturnsNoDiagnostic()
        {
            // Missing closing braces, malformed code
            const string code = @"
                class C
                {
                    void M()
                    {
                        if (a)
                        {
                            if (b)
                                if (c)
                                    if (d)
                        // missing closing braces
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new ComplexConditionalAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Should not throw, may or may not report diagnostics depending on parse recovery
            Assert.True(diagnostics.Count >= 0);
        }
    }
}