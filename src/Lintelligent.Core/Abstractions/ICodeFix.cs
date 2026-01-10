using Microsoft.CodeAnalysis;

namespace Lintelligent.Core.Abstractions
{
    public interface ICodeFix
    {
        bool CanFix(DiagnosticResult diagnostic);
        CodeFixResult ApplyFix(DiagnosticResult diagnostic, SyntaxTree tree);
    }
}