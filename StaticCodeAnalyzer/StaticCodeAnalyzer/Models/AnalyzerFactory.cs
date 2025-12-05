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
            // Сначала проверяем по расширению файла (самый надежный способ)
            if (!string.IsNullOrEmpty(filePath))
            {
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
                var analyzerByExtension = _analyzers.FirstOrDefault(analyzer =>
                    analyzer.CanHandle("", fileExtension));

                if (analyzerByExtension != null)
                {
                    return analyzerByExtension;
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
    }
}