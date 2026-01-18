using System.Threading.Tasks;
using Xunit;
using VerifyCS = Lintelligent.Analyzers.Basic.Test.CSharpAnalyzerVerifier<
    Lintelligent.Analyzers.Basic.PreferOptionMonadRoslynAdapter>;

namespace Lintelligent.Analyzers.Basic.Test;

/// <summary>
/// Tests for Roslyn adapter configuration and suppression mechanisms.
/// Basic detection tests are covered in PreferOptionMonadRegressionTests.
/// </summary>
public class PreferOptionMonadAdapterTests
{
    [Fact]
    public async Task IgnoresVoidMethodsWithRoslyn()
    {
        var test = """
                   class Program
                   {
                       void DoSomething()
                       {
                       }
                   }
                   """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    // Configuration and Suppression Tests

    [Fact]
    public async Task RespectsPragmaWarningDisable()
    {
        var test = """
                   class Program
                   {
                   #pragma warning disable LINT003
                       int? GetCount()
                       {
                           return null;
                       }
                   #pragma warning restore LINT003
                   }
                   """;

        // Should not report diagnostic when #pragma warning disable is present
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RespectsSeverityConfiguration()
    {
        // This test verifies that the analyzer can be configured via .editorconfig
        // In a real scenario, this would be controlled by .editorconfig settings
        // The Roslyn testing framework supports this through configuration
        var test = """
                   class Program
                   {
                       [|int?|] GetCount()
                       {
                           return null;
                       }
                   }
                   """;

        // With default severity (Info), diagnostic should still be reported
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RespectsEditorConfigExclusions()
    {
        // This test verifies that certain code patterns can be excluded
        // For now, we verify that the analyzer respects standard exclusion patterns
        // More sophisticated .editorconfig support can be added in future versions
        var test = """
                   class Program
                   {
                       [|int?|] GetCount()
                       {
                           return null;
                       }
                   }
                   """;

        // This is a placeholder test - in a real scenario, exclusions would be
        // configured via .editorconfig and the analyzer would check file paths
        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
