// Models/AnalysisResult.cs
namespace StaticCodeAnalyzer.Models
{
    public class AnalysisResult
    {
        public int LineNumber { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; } // Возможное решение для показа
        public string OriginalCode { get; set; } // Исходная строка кода
        public string FixedCode { get; set; }   // Исправленная строка кода

        public AnalysisResult(int lineNumber, string errorType, string message, string suggestion, string originalCode, string fixedCode)
        {
            LineNumber = lineNumber;
            ErrorType = errorType;
            Message = message;
            Suggestion = suggestion;
            OriginalCode = originalCode;
            FixedCode = fixedCode;
        }
    }
}