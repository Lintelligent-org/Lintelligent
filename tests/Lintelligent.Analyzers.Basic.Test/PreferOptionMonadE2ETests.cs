using System.Threading.Tasks;
using Xunit;
using VerifyCS = Lintelligent.Analyzers.Basic.Test.CSharpCodeFixVerifier<
    Lintelligent.Analyzers.Basic.PreferOptionMonadRoslynAdapter,
    Lintelligent.Analyzers.Basic.CodeFixes.PreferOptionMonadCodeFixProvider>;

namespace Lintelligent.Analyzers.Basic.Test;

/// <summary>
/// End-to-end integration tests for the Option Monad analyzer (Phase 8: T073)
/// Tests the complete workflow: detect → fix → verify
/// </summary>
public class PreferOptionMonadE2ETests
{
    [Fact]
    public async Task EndToEnd_NullableReferenceType_DetectAndFix()
    {
        var test = """
                   #nullable enable
                   using LanguageExt;
                   
                   class Program
                   {
                       [|string?|] GetName(bool condition)
                       {
                           if (condition)
                               return null;
                           return "Alice";
                       }
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using LanguageExt;
                        
                        class Program
                        {
                            Option<string> GetName(bool condition)
                            {
                                if (condition)
                                    return Option<string>.None;
                                return Option<string>.Some("Alice");
                            }
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_NullableValueType_DetectAndFix()
    {
        var test = """
                   using LanguageExt;
                   
                   class Program
                   {
                       [|int?|] GetCount(bool condition)
                       {
                           if (condition)
                               return null;
                           return 42;
                       }
                   }
                   """;

        var fixedCode = """
                        using LanguageExt;
                        
                        class Program
                        {
                            Option<int> GetCount(bool condition)
                            {
                                if (condition)
                                    return Option<int>.None;
                                return Option<int>.Some(42);
                            }
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_AsyncMethod_DetectAndFix()
    {
        var test = """
                   #nullable enable
                   using System.Threading.Tasks;
                   using LanguageExt;
                   
                   class Program
                   {
                       async [|Task<string?>|] GetValueAsync(bool condition)
                       {
                           await Task.Delay(100);
                           if (condition)
                               return null;
                           return "Result";
                       }
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using System.Threading.Tasks;
                        using LanguageExt;
                        
                        class Program
                        {
                            async Task<Option<string>> GetValueAsync(bool condition)
                            {
                                await Task.Delay(100);
                                if (condition)
                                    return Option<string>.None;
                                return Option<string>.Some("Result");
                            }
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_GenericMethod_DetectAndFix()
    {
        var test = """
                   #nullable enable
                   using LanguageExt;
                   
                   class Program
                   {
                       [|T?|] GetValue<T>() where T : class
                       {
                           return null;
                       }
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using LanguageExt;
                        
                        class Program
                        {
                            Option<T> GetValue<T>() where T : class
                            {
                                return Option<T>.None;
                            }
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_ExpressionBodied_DetectAndFix()
    {
        var test = """
                   #nullable enable
                   using LanguageExt;
                   
                   class Program
                   {
                       [|string?|] GetValue() => null;
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using LanguageExt;
                        
                        class Program
                        {
                            Option<string> GetValue() => Option<string>.None;
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_MultipleReturns_DetectAndFix()
    {
        var test = """
                   #nullable enable
                   using LanguageExt;
                   
                   class Program
                   {
                       [|string?|] GetStatus(int code)
                       {
                           if (code == 0)
                               return null;
                           if (code == 1)
                               return "Success";
                           return "Error";
                       }
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using LanguageExt;
                        
                        class Program
                        {
                            Option<string> GetStatus(int code)
                            {
                                if (code == 0)
                                    return Option<string>.None;
                                if (code == 1)
                                    return Option<string>.Some("Success");
                                return Option<string>.Some("Error");
                            }
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_SwitchExpression_DetectAndFix()
    {
        var test = """
                   #nullable enable
                   using LanguageExt;
                   
                   class Program
                   {
                       [|string?|] GetValue(int x) => x switch
                       {
                           0 => null,
                           1 => "One",
                           _ => "Other"
                       };
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using LanguageExt;
                        
                        class Program
                        {
                            Option<string> GetValue(int x) => x switch
                            {
                                0 => Option<string>.None,
                                1 => Option<string>.Some("One"),
                                _ => Option<string>.Some("Other")
                            };
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    [Fact]
    public async Task EndToEnd_ComplexScenario_DetectAndFix()
    {
        // Real-world scenario: Repository pattern method with explicit null check
        var test = """
                   #nullable enable
                   using System.Collections.Generic;
                   using System.Linq;
                   using LanguageExt;
                   
                   public class User
                   {
                       public int Id { get; set; }
                       public string Name { get; set; } = "";
                   }
                   
                   public class UserRepository
                   {
                       private readonly List<User> _users = new();
                       
                       [|User?|] FindById(int id)
                       {
                           var user = _users.FirstOrDefault(u => u.Id == id);
                           if (user == null)
                               return null;
                           return user;
                       }
                   }
                   """;

        var fixedCode = """
                        #nullable enable
                        using System.Collections.Generic;
                        using System.Linq;
                        using LanguageExt;
                        
                        public class User
                        {
                            public int Id { get; set; }
                            public string Name { get; set; } = "";
                        }
                        
                        public class UserRepository
                        {
                            private readonly List<User> _users = new();
                        
                            Option<User> FindById(int id)
                            {
                                var user = _users.FirstOrDefault(u => u.Id == id);
                                if (user == null)
                                    return Option<User>.None;
                                return Option<User>.Some(user);
                            }
                        }
                        """;

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }
}
