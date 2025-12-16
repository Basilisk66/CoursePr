using StaticCodeAnalyzer.Analyzers;
using System.Collections.Generic;
using System.Linq;

namespace StaticCodeAnalyzer.Models
{
    public static class AnalyzerFactory
    {
        private static readonly List<CodeAnalyzer> _analyzers = new List<CodeAnalyzer>
        {
            new CSharpAnalyzer(),
            new HtmlAnalyzer(),
            new PythonAnalyzer(),
            new JavaAnalyzer(),
            new CppAnalyzer(),
            new JavaScriptAnalyzer()
        };

        public static CodeAnalyzer GetAnalyzer(string code, string filePath = "")
        {
            // Сначала по расширению файла
            if (!string.IsNullOrEmpty(filePath))
            {
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
                foreach (var analyzer in _analyzers)
                {
                    if (analyzer.CanHandle("", fileExtension))
                    {
                        return analyzer;
                    }
                }
            }

            // Затем по содержимому кода
            if (!string.IsNullOrEmpty(code))
            {
                foreach (var analyzer in _analyzers)
                {
                    if (analyzer.CanHandle(code, ""))
                    {
                        return analyzer;
                    }
                }
            }

            return null;
        }

        public static List<string> GetSupportedLanguages()
        {
            return new List<string> { "C#", "C++", "Java", "Python", "JavaScript", "HTML" };
        }
    }
}