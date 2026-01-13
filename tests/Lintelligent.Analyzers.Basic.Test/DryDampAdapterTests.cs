using System.Threading.Tasks;
using Xunit;
using VerifyCS = Lintelligent.Analyzers.Basic.Test.CSharpAnalyzerVerifier<
    Lintelligent.Analyzers.Basic.DryDampRoslynAdapter>;

namespace Lintelligent.Analyzers.Basic.Test
{
    public class DryDampAdapterTests
    {
        [Fact]
        public async Task AnalyzerDetectsDuplicateCodeBlocks()
        {
            var test = """
                       class Program
                       {
                           void Test()
                           {
                               var x = 1;
                               var y = 2;
                               var z = x + y;
                           }
                           
                           void Test2()
                           {
                               [|{|]
                                   var x = 1;
                                   var y = 2;
                                   var z = x + y;
                               }
                           }
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerDetectsComplexIfCondition()
        {
            var test = """
                       class Program
                       {
                           void Test(User user)
                           {
                               [|if|] (user != null && user.IsActive && user.HasPermission("Admin") && user.Department == "IT" && user.Score > 100)
                               {
                                   // Should extract: if (IsAuthorizedITUser(user))
                               }
                           }
                       }
                       
                       class User
                       {
                           public bool IsActive { get; set; }
                           public string Department { get; set; }
                           public int Score { get; set; }
                           public bool HasPermission(string permission) => true;
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerDetectsComplexWhileCondition()
        {
            var test = """
                       class Program
                       {
                           void Test(int x, int y, int z)
                           {
                               [|while|] (x > 0 && y < 100 && z != 50 && x * y > 200 && z % 2 == 0)
                               {
                                   x--;
                               }
                           }
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerDoesNotFlagSimpleConditions()
        {
            var test = """
                       class Program
                       {
                           void Test(int x, int y)
                           {
                               if (x > 0 && y < 100)
                               {
                                   // Simple condition - should not trigger
                               }
                           }
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerDoesNotFlagShortBlocks()
        {
            var test = """
                       class Program
                       {
                           void Test()
                           {
                               var x = 1;
                               var y = 2;
                           }
                           
                           void Test2()
                           {
                               var x = 1;
                               var y = 2;
                           }
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerDetectsMultipleDuplicateBlocks()
        {
            var test = """
                       class Program
                       {
                           void First()
                           {
                               var a = 1;
                               var b = 2;
                               var c = a + b;
                               System.Console.WriteLine(c);
                           }
                           
                           void Second()
                           {
                               [|{|]
                                   var a = 1;
                                   var b = 2;
                                   var c = a + b;
                                   System.Console.WriteLine(c);
                               }
                           }
                           
                           void Third()
                           {
                               [|{|]
                                   var a = 1;
                                   var b = 2;
                                   var c = a + b;
                                   System.Console.WriteLine(c);
                               }
                           }
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerIgnoresNonIdenticalBlocks()
        {
            var test = """
                       class Program
                       {
                           void Test()
                           {
                               var x = 1;
                               var y = 2;
                               var z = x + y;
                           }
                           
                           void Test2()
                           {
                               var a = 1;
                               var b = 3;
                               var c = a * b;
                           }
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnalyzerDetectsBothDryAndDampViolations()
        {
            var test = """
                       class Program
                       {
                           void ProcessUser(User user)
                           {
                               [|if|] (user != null && user.IsActive && user.HasPermission("Admin") && user.Department == "IT" && user.Score > 100)
                               {
                                   var x = 1;
                                   var y = 2;
                                   var z = x + y;
                               }
                           }
                           
                           void ProcessAdmin(User admin)
                           {
                               if (admin != null && admin.IsActive)
                               {
                                   [|{|]
                                       var x = 1;
                                       var y = 2;
                                       var z = x + y;
                                   }
                               }
                           }
                       }
                       
                       class User
                       {
                           public bool IsActive { get; set; }
                           public string Department { get; set; }
                           public int Score { get; set; }
                           public bool HasPermission(string permission) => true;
                       }
                       """;

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
