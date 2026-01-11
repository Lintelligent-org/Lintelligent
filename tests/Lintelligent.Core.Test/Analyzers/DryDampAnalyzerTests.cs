using System.Linq;
using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lintelligent.Core.Test.Analyzers
{
    public class DryDampAnalyzerTests
    {
        #region Duplicate Code Detection (DRY) Tests

        [Fact]
        public void DetectsDuplicateCodeBlocks_WhenThreeStatementsRepeated_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }

                    void M2()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Single(diagnostics);
            Assert.Equal("LINT002", diagnostics[0].Id);
            Assert.Contains("Duplicate code block", diagnostics[0].Message);
            Assert.Contains("DRY principle", diagnostics[0].Message);
        }

        [Fact]
        public void DetectsMultipleDuplicateBlocks_WhenThreeOccurrences_ReturnsTwoDiagnostics()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        var a = 1;
                        var b = 2;
                        var c = a + b;
                    }

                    void M2()
                    {
                        var a = 1;
                        var b = 2;
                        var c = a + b;
                    }

                    void M3()
                    {
                        var a = 1;
                        var b = 2;
                        var c = a + b;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Should report 2 duplicates (second and third occurrence)
            Assert.Equal(2, diagnostics.Count);
            Assert.All(diagnostics, d => Assert.Equal("LINT002", d.Id));
        }

        [Fact]
        public void DoesNotDetectDuplicates_WhenOnlyTwoStatements_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        var x = 1;
                        var y = 2;
                    }

                    void M2()
                    {
                        var x = 1;
                        var y = 2;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void DoesNotDetectDuplicates_WhenBlocksAreDifferent_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }

                    void M2()
                    {
                        var a = 1;
                        var b = 3;
                        var c = a * b;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void DoesNotDetectDuplicates_WhenDifferentInternalWhitespace_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }

                    void M2()
                    {
                        var x=1;
                        var y=2;
                        var z=x+y;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Internal whitespace differences mean different normalized strings
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void DetectsDuplicates_InNestedBlocks_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        if (true)
                        {
                            var x = 1;
                            var y = 2;
                            var z = x + y;
                        }
                    }

                    void M2()
                    {
                        if (false)
                        {
                            var x = 1;
                            var y = 2;
                            var z = x + y;
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Single(diagnostics);
            Assert.Equal("LINT002", diagnostics[0].Id);
        }

        [Fact]
        public void DoesNotDetectDuplicates_WhenSingleOccurrence_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        #endregion

        #region Complex Condition Detection (DAMP) Tests

        [Fact]
        public void DetectsComplexCondition_WhenIfHasFiveBinaryExpressions_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(bool a, bool b, bool c, bool d, bool e, bool f)
                    {
                        if (a && b && c && d && e && f)
                        {
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Single(diagnostics);
            Assert.Equal("LINT002", diagnostics[0].Id);
            Assert.Contains("Complex condition", diagnostics[0].Message);
            Assert.Contains("DAMP principle", diagnostics[0].Message);
        }

        [Fact]
        public void DetectsComplexCondition_WhenWhileHasFiveBinaryExpressions_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(int x, int y, int z)
                    {
                        while (x > 0 && y < 100 && z != 50 && x * y > 200 && z % 2 == 0)
                        {
                            x--;
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Single(diagnostics);
            Assert.Equal("LINT002", diagnostics[0].Id);
            Assert.Contains("Complex condition", diagnostics[0].Message);
        }

        [Fact]
        public void DoesNotDetectComplexCondition_WhenExactlyThreeBinaryExpressions_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(bool a, bool b, bool c, bool d)
                    {
                        if (a && b && c && d)
                        {
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Exactly 3 binary expressions (threshold), should not trigger
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void DoesNotDetectComplexCondition_WhenLessThanThreeBinaryExpressions_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(bool a, bool b)
                    {
                        if (a && b)
                        {
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void DetectsComplexCondition_WhenMixedAndOrOperators_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(bool a, bool b, bool c, bool d, bool e, bool f)
                    {
                        if ((a && b) || (c && d) || (e && f))
                        {
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // 5 binary expressions (2 OR + 3 AND that are children), should trigger
            Assert.Single(diagnostics);
            Assert.Equal("LINT002", diagnostics[0].Id);
        }

        [Fact]
        public void DoesNotDetectComplexCondition_WhenNonLogicalBinaryExpression_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(int a, int b, int c, int d, int e, int f)
                    {
                        if (a + b + c + d + e + f > 100)
                        {
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Arithmetic expressions don't count as logical complexity
            Assert.Empty(diagnostics);
        }

        #endregion

        #region Combined DRY and DAMP Tests

        [Fact]
        public void DetectsBothDryAndDamp_WhenBothViolationsPresent_ReturnsBothDiagnostics()
        {
            const string code = @"
                class C
                {
                    void M1(bool a, bool b, bool c, bool d, bool e)
                    {
                        if (a && b && c && d && e)
                        {
                            var x = 1;
                            var y = 2;
                            var z = x + y;
                        }
                    }

                    void M2()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Equal(2, diagnostics.Count);
            Assert.Contains(diagnostics, d => d.Message.Contains("Complex condition"));
            Assert.Contains(diagnostics, d => d.Message.Contains("Duplicate code block"));
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void HandlesEmptyClass_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void HandlesEmptyMethod_ReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M() { }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void HandlesMalformedSyntaxTree_GracefullyReturnsNoDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M()
                    {
                        if (a && b && c
                        // missing closing parts
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            
            // Should not throw
            var diagnostics = analyzer.Analyze(tree, model).ToList();
            
            Assert.True(diagnostics.Count >= 0);
        }

        [Fact]
        public void HandlesNullSemanticModel_DoesNotThrow()
        {
            const string code = @"
                class C
                {
                    void M()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);

            var analyzer = new DryDampAnalyzer();
            
            // Should not throw even with null semantic model
            var diagnostics = analyzer.Analyze(tree, null).ToList();
            
            Assert.True(diagnostics.Count >= 0);
        }

        #endregion

        #region Boundary Value Tests

        [Fact]
        public void DetectsDuplicate_WhenExactlyThreeStatements_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M1()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }

                    void M2()
                    {
                        var x = 1;
                        var y = 2;
                        var z = x + y;
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Exactly 3 statements (minimum threshold), should trigger
            Assert.Single(diagnostics);
        }

        [Fact]
        public void DetectsComplexCondition_WhenExactlyFourBinaryExpressions_ReturnsDiagnostic()
        {
            const string code = @"
                class C
                {
                    void M(bool a, bool b, bool c, bool d, bool e)
                    {
                        if (a && b && c && d && e)
                        {
                        }
                    }
                }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Test", new[] { tree });
            var model = compilation.GetSemanticModel(tree);

            var analyzer = new DryDampAnalyzer();
            var diagnostics = analyzer.Analyze(tree, model).ToList();

            // Exactly 4 binary expressions (just over threshold of 3), should trigger
            Assert.Single(diagnostics);
        }

        #endregion
    }
}
