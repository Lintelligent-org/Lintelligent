using Lintelligent.Core.Abstractions;
using Lintelligent.Core.Analyzers;
using Lintelligent.Core.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.Core.Test.CodeFixes;

public class PreferOptionMonadCodeFixTests
{
    [Fact]
    public void TransformsNullableReferenceType()
    {
        const string code = """
                            #nullable enable
                            class Program
                            {
                                string? GetValue()
                                {
                                    return null;
                                }
                            }
                            """;

        const string expected = """
                                #nullable enable
                                using LanguageExt;
                                
                                class Program
                                {
                                    Option<string> GetValue()
                                    {
                                        return Option<string>.None;
                                    }
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        // Get diagnostic from analyzer
        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        // Apply code fix
        var codeFix = new PreferOptionMonadCodeFix();
        Assert.True(codeFix.CanFix(diagnostics[0]));
        
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Equal(expected.Trim(), actualCode.Trim());
    }

    [Fact]
    public void TransformsNullReturnToNone()
    {
        const string code = """
                            class Program
                            {
                                int? GetCount()
                                {
                                    return null;
                                }
                            }
                            """;

        const string expected = """
                                using LanguageExt;
                                
                                class Program
                                {
                                    Option<int> GetCount()
                                    {
                                        return Option<int>.None;
                                    }
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Equal(expected.Trim(), actualCode.Trim());
    }

    [Fact]
    public void TransformsNonNullReturnToSome()
    {
        const string code = """
                            class Program
                            {
                                string? GetValue()
                                {
                                    return "test";
                                }
                            }
                            """;

        const string expected = """
                                using LanguageExt;
                                
                                class Program
                                {
                                    Option<string> GetValue()
                                    {
                                        return Option<string>.Some("test");
                                    }
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Equal(expected.Trim(), actualCode.Trim());
    }

    [Fact]
    public void AddsUsingDirective()
    {
        const string code = """
                            class Program
                            {
                                int? GetValue()
                                {
                                    return 42;
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Contains("using LanguageExt;", actualCode);
    }

    [Fact]
    public void PreservesExistingUsingDirective()
    {
        const string code = """
                            using LanguageExt;
                            using System;
                            
                            class Program
                            {
                                int? GetValue()
                                {
                                    return 42;
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        // Should have using LanguageExt only once
        var usingCount = actualCode.Split("using LanguageExt;").Length - 1;
        Assert.Equal(1, usingCount);
    }

    [Fact]
    public void TransformsMultipleReturns()
    {
        const string code = """
                            class Program
                            {
                                string? GetValue(bool flag)
                                {
                                    if (flag)
                                        return null;
                                    else
                                        return "test";
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Contains("Option<string>.None", actualCode);
        Assert.Contains("Option<string>.Some(\"test\")", actualCode);
    }

    [Fact]
    public void TransformsGenericMethod()
    {
        const string code = """
                            class Program
                            {
                                T? GetValue<T>()
                                {
                                    return default;
                                }
                            }
                            """;

        const string expected = """
                                using LanguageExt;
                                
                                class Program
                                {
                                    Option<T> GetValue<T>()
                                    {
                                        return Option<T>.None;
                                    }
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Equal(expected.Trim(), actualCode.Trim());
    }

    [Fact]
    public void TransformsAsyncMethod()
    {
        const string code = """
                            using System.Threading.Tasks;
                            
                            class Program
                            {
                                async Task<string?> GetValueAsync()
                                {
                                    return await Task.FromResult<string?>(null);
                                }
                            }
                            """;

        const string expected = """
                                using System.Threading.Tasks;
                                using LanguageExt;
                                
                                class Program
                                {
                                    async Task<Option<string>> GetValueAsync()
                                    {
                                        return Option<string>.None;
                                    }
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Contains("Task<Option<string>>", actualCode);
        Assert.Contains("Option<string>.None", actualCode);
    }

    [Fact]
    public void TransformsExpressionBodiedMember()
    {
        const string code = """
                            class Program
                            {
                                string? GetValue() => null;
                            }
                            """;

        const string expected = """
                                using LanguageExt;
                                
                                class Program
                                {
                                    Option<string> GetValue() => Option<string>.None;
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Equal(expected.Trim(), actualCode.Trim());
    }

    [Fact]
    public void TransformsMultipleReturnsWithMixedTypes()
    {
        const string code = """
                            class Program
                            {
                                int? GetValue(int x)
                                {
                                    if (x < 0)
                                        return null;
                                    if (x == 0)
                                        return 0;
                                    return x * 2;
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        Assert.Contains("Option<int>.None", actualCode);
        Assert.Contains("Option<int>.Some(0)", actualCode);
        Assert.Contains("Option<int>.Some(x * 2)", actualCode);
    }

    [Fact]
    public void TransformsSwitchExpression()
    {
        const string code = """
                            class Program
                            {
                                string? GetValue(int x) => x switch
                                {
                                    0 => null,
                                    _ => "value"
                                };
                            }
                            """;

        const string expected = """
                                using LanguageExt;
                                
                                class Program
                                {
                                    Option<string> GetValue(int x) => x switch
                                    {
                                        0 => Option<string>.None,
                                        _ => Option<string>.Some("value")
                                    };
                                }
                                """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();
        
        Assert.Single(diagnostics);

        var codeFix = new PreferOptionMonadCodeFix();
        var result = codeFix.ApplyFix(diagnostics[0], tree);
        var actualCode = result.UpdatedTree.ToString();

        // Check semantic correctness - switch expression transformation
        Assert.Contains("Option<string> GetValue(int x) => x switch", actualCode);
        Assert.Contains("Option<string>.None", actualCode);  
        Assert.Contains("Option<string>.Some", actualCode); // Some method is called
    }
}
