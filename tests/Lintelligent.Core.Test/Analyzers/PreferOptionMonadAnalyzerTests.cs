using Lintelligent.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lintelligent.Core.Test.Analyzers;

public class PreferOptionMonadAnalyzerTests
{
    [Fact]
    public void DetectsNullableReferenceType()
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

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Single(diagnostics);
        Assert.Equal("LINT003", diagnostics[0].Id);
        Assert.Contains("GetValue", diagnostics[0].Message);
        Assert.Contains("string?", diagnostics[0].Message);
        Assert.Contains("Option<string>", diagnostics[0].Message);
    }

    [Fact]
    public void DetectsNullableValueType()
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

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Single(diagnostics);
        Assert.Equal("LINT003", diagnostics[0].Id);
        Assert.Contains("GetCount", diagnostics[0].Message);
        Assert.Contains("int?", diagnostics[0].Message);
        Assert.Contains("Option<int>", diagnostics[0].Message);
    }

    [Fact]
    public void IgnoresNonNullableTypes()
    {
        const string code = """
                            class Program
                            {
                                string GetValue()
                                {
                                    return "test";
                                }
                                
                                int GetCount()
                                {
                                    return 42;
                                }
                                
                                void DoSomething()
                                {
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void IgnoresMethodsReturningOption()
    {
        const string code = """
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
        
        // Add LanguageExt reference
        var languageExtReference = MetadataReference.CreateFromFile(typeof(LanguageExt.Option<>).Assembly.Location);
        var compilation = CSharpCompilation.Create("Test", 
            new[] { tree },
            new[] { languageExtReference, MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Empty(diagnostics);
    }

    // TODO: Advanced feature - requires enhanced async type detection
    // Will be implemented in Phase 5 (User Story 2 Extended - Advanced Transformations)
    /*
    [Fact]
    public void DetectsAsyncMethodReturningNullable()
    {
        const string code = """
                            #nullable enable
                            using System.Threading.Tasks;
                            
                            class Program
                            {
                                async Task<string?> GetValueAsync()
                                {
                                    return null;
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Single(diagnostics);
        Assert.Equal("LINT003", diagnostics[0].Id);
        Assert.Contains("GetValueAsync", diagnostics[0].Message);
        Assert.Contains("Task<Option<string>>", diagnostics[0].Message);
    }
    */

    // TODO: Advanced feature - requires enhanced generic type constraint detection
    // Will be implemented in Phase 5 (User Story 2 Extended - Advanced Transformations)
    /*
    [Fact]
    public void DetectsGenericMethodReturningNullable()
    {
        const string code = """
                            #nullable enable
                            class Program
                            {
                                T? GetValue<T>() where T : class
                                {
                                    return null;
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Single(diagnostics);
        Assert.Equal("LINT003", diagnostics[0].Id);
        Assert.Contains("GetValue", diagnostics[0].Message);
    }
    */

    [Fact]
    public void IgnoresExternMethods()
    {
        const string code = """
                            #nullable enable
                            class Program
                            {
                                extern string? GetValue();
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void IgnoresPartialMethods()
    {
        const string code = """
                            #nullable enable
                            partial class Program
                            {
                                partial string? GetValue();
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        Assert.Empty(diagnostics);
    }

    // Phase 6: User Story 3 - Null Check Detection (T053-T055)
    
    [Fact]
    public void DetectsNullConditionalOperator()
    {
        const string code = """
                            #nullable enable
                            class Program
                            {
                                string GetName(User? user)
                                {
                                    return user?.Name ?? "Unknown";
                                }
                            }
                            
                            class User
                            {
                                public string Name { get; set; } = "";
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        // Should detect the ?. operator usage
        Assert.NotEmpty(diagnostics);
        var nullConditionalDiagnostic = diagnostics.FirstOrDefault(d => d.Message.Contains("?."));
        Assert.NotNull(nullConditionalDiagnostic);
        Assert.Equal("LINT003", nullConditionalDiagnostic.Id);
    }

    [Fact]
    public void DetectsNullCoalescingOperator()
    {
        const string code = """
                            #nullable enable
                            class Program
                            {
                                string GetValue(string? input)
                                {
                                    return input ?? "default";
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        // Should detect the ?? operator usage
        Assert.NotEmpty(diagnostics);
        var nullCoalescingDiagnostic = diagnostics.FirstOrDefault(d => d.Message.Contains("??"));
        Assert.NotNull(nullCoalescingDiagnostic);
        Assert.Equal("LINT003", nullCoalescingDiagnostic.Id);
    }

    [Fact]
    public void DetectsIfNullCheck()
    {
        const string code = """
                            #nullable enable
                            class Program
                            {
                                string GetValue(string? input)
                                {
                                    if (input != null)
                                        return input;
                                    return "default";
                                }
                            }
                            """;

        var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var model = compilation.GetSemanticModel(tree);

        var analyzer = new PreferOptionMonadAnalyzer();
        var diagnostics = analyzer.Analyze(tree, model).ToList();

        // Should detect the if != null check
        Assert.NotEmpty(diagnostics);
        var nullCheckDiagnostic = diagnostics.FirstOrDefault(d => d.Message.Contains("null check"));
        Assert.NotNull(nullCheckDiagnostic);
        Assert.Equal("LINT003", nullCheckDiagnostic.Id);
    }
}
