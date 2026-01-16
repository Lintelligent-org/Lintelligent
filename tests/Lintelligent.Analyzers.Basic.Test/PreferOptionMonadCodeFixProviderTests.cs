using System.Threading.Tasks;
using Xunit;
using VerifyCS = Lintelligent.Analyzers.Basic.Test.CSharpCodeFixVerifier<
    Lintelligent.Analyzers.Basic.PreferOptionMonadRoslynAdapter,
    Lintelligent.Analyzers.Basic.CodeFixes.PreferOptionMonadCodeFixProvider>;

namespace Lintelligent.Analyzers.Basic.Test;

public class PreferOptionMonadCodeFixProviderTests
{
    [Fact]
    public async Task CodeFixTransformsNullableToOption()
    {
        var test = """
            class Program
            {
                [|int?|] GetValue()
                {
                    return null;
                }
            }
            """;

        var fixedCode = """
            using LanguageExt;

            class Program
            {
                Option<int> GetValue()
                {
                    return Option<int>.None;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task CodeFixTransformsNullableValueType()
    {
        var test = """
            using System;

            class Program
            {
                [|DateTime?|] GetValue()
                {
                    return null;
                }
            }
            """;

        var fixedCode = """
            using System;
            using LanguageExt;


            class Program
            {
                Option<DateTime> GetValue()
                {
                    return Option<DateTime>.None;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task CodeFixTransformsMultipleReturns()
    {
        var test = """
            using System;

            class Program
            {
                [|int?|] GetValue(bool flag)
                {
                    if (flag)
                        return null;
                    else
                        return 42;
                }
            }
            """;

        var fixedCode = """
            using System;
            using LanguageExt;


            class Program
            {
                Option<int> GetValue(bool flag)
                {
                    if (flag)
                        return Option<int>.None;
                    else
                        return Option<int>.Some(42);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }
}
