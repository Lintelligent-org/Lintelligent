using Microsoft.CodeAnalysis;

namespace Lintelligent.Core.Abstractions
{
    public sealed class CodeFixResult
    {
        public SyntaxTree UpdatedTree { get; }
        public CodeFixResult(SyntaxTree updatedTree) => UpdatedTree = updatedTree;
    }
}