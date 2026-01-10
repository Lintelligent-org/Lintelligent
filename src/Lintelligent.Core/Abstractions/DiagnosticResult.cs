using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Lintelligent.Core.Abstractions
{
    public sealed class DiagnosticResult
    {
        public string Id { get; }
        public string Message { get; }
        public TextSpan Span { get; }
        public DiagnosticSeverity Severity { get; }

        public DiagnosticResult(string id, string message, TextSpan span, DiagnosticSeverity severity)
        {
            Id = id;
            Message = message;
            Span = span;
            Severity = severity;
        }
    }
}