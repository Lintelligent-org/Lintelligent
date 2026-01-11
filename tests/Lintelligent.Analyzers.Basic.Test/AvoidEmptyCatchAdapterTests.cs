using System.Threading.Tasks;
using Xunit;
using VerifyCS = Lintelligent.Analyzers.Basic.Test.CSharpAnalyzerVerifier<
    Lintelligent.Analyzers.Basic.AvoidEmptyCatchRoslynAdapter>;

public class AvoidEmptyCatchAdapterTests
{
    [Fact]
    public async Task AnalyzerDetectsEmptyCatch()
    {
        var test = """

                   class Program
                   {
                       void Test()
                       {
                           try { } 
                           [|catch|] { }
                       }
                   }
                   """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}