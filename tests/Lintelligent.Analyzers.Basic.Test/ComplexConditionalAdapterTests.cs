using System.Threading.Tasks;
using Xunit;
using VerifyCS = Lintelligent.Analyzers.Basic.Test.CSharpAnalyzerVerifier<
    Lintelligent.Analyzers.Basic.ComplexConditionalRoslynAdapter>;

public class ComplexConditionalAdapterTests
{
    [Fact]
    public async Task AnalyzerDetectsDeeplyNestedIfs()
    {
        var test = """
using System;
class C {
    void M() {
        bool a = true, b = true, c = true, d = true;
        if (a)
        {
            if (b)
            {
                if (c)
                {
                    [|if (d) { }|]
                }
            }
        }
    }
}
""";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task AnalyzerDoesNotTriggerOnShallowIfs()
    {
        var test = """
class C {
    void M() {
        bool a = true, b = true, c = true;
        if (a) {
            if (b) {
                if (c) { }
            }
        }
    }
}
""";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
