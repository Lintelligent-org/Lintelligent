using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Lintelligent.Core.Abstractions
{
    public interface ICodeAnalyzer
    {
        IEnumerable<DiagnosticResult> Analyze(SyntaxTree tree, SemanticModel semanticModel);
    }
}