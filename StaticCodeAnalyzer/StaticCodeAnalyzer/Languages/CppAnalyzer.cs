using StaticCodeAnalyzer.Models;
using StaticCodeAnalyzer.Models.StyleGuides;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Analyzers
{
    public class CppAnalyzer : CodeAnalyzer
    {
        protected override void InitializeStyleGuides()
        {
            base.InitializeStyleGuides();
            // Добавляем Google C++ Style Guide
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Существующие проверки
            results.AddRange(CheckBasicRules(lines, code));

            // Применяем стилевые правила
            results.AddRange(ApplyStyleRules(code, "C++"));

            return results;
        }

        private List<AnalysisResult> CheckBasicRules(string[] lines, string code)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Правило: использование nullptr
                if (line.Contains("= NULL") || line.Contains("== NULL") || line.Contains("!= NULL"))
                {
                    string fixedLine = line.Replace("= NULL", "= nullptr")
                                          .Replace("== NULL", "== nullptr")
                                          .Replace("!= NULL", "!= nullptr");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Modern C++",
                        "Use nullptr instead of NULL (C++ Core Guidelines)",
                        "Replace NULL with nullptr for type safety",
                        line.Trim(),
                        fixedLine.Trim()
                    ));
                }
            }

            return results;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            // Проверяем по расширению файла
            if (!string.IsNullOrEmpty(fileExtension))
            {
                if (fileExtension.ToLower() == ".cpp" || fileExtension.ToLower() == ".h" ||
                    fileExtension.ToLower() == ".hpp" || fileExtension.ToLower() == ".cc")
                    return true;
            }

            // Проверяем по содержимому кода
            return code.Contains("#include") || code.Contains("using namespace") ||
                   code.Contains("std::") || code.Contains("cout <<");
        }
    }
}