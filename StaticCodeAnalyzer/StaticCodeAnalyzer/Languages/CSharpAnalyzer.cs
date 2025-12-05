using StaticCodeAnalyzer.Models;
using StaticCodeAnalyzer.Models.StyleGuides;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Analyzers
{
    public class CSharpAnalyzer : CodeAnalyzer
    {
        protected override void InitializeStyleGuides()
        {
            base.InitializeStyleGuides();
            _styleGuides.Add(new MicrosoftStyleGuide());
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Существующие проверки
            results.AddRange(CheckBasicRules(lines));

            // Применяем стилевые правила
            results.AddRange(ApplyStyleRules(code, "C#"));

            return results;
        }

        private List<AnalysisResult> CheckBasicRules(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Правило: использование var
                var explicitTypeMatch = Regex.Match(line, @"\b(int|string|double)\s+\w+\s*=");
                if (explicitTypeMatch.Success && !line.Contains("var"))
                {
                    string fixedLine = Regex.Replace(line, @"\b(int|string|double)\s+(\w+\s*=)", "var $2");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Code Style",
                        "Consider using 'var' for implicit typing (Microsoft C# Coding Conventions)",
                        "Replace explicit type with 'var' for better readability",
                        line.Trim(),
                        fixedLine.Trim()
                    ));
                }

                // Правило: длинные строки
                if (line.Length > 120)
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Line exceeds 120 characters (Microsoft C# Coding Conventions)",
                        "Break the line into multiple lines or use string interpolation",
                        line.Trim(),
                        line.Trim()
                    ));
                }
            }

            return results;
        }

        protected override List<AnalysisResult> CheckStyleRule(string[] lines, StyleRule rule, string language, string styleGuideName)
        {
            var results = new List<AnalysisResult>();

            if (language != "C#") return results;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Применяем правила Microsoft Style Guide для C#
                switch (rule.Id)
                {
                    case "MS_001": // Именование классов
                        var classMatch = Regex.Match(line, @"class\s+([a-z][a-zA-Z0-9_]*)");
                        if (classMatch.Success)
                        {
                            results.Add(new AnalysisResult(
                                lineNumber,
                                "Naming",
                                $"{rule.Message} ({styleGuideName})",
                                rule.Suggestion,
                                line.Trim(),
                                Regex.Replace(line, @"class\s+([a-z][a-zA-Z0-9_]*)",
                                    m => $"class {char.ToUpper(m.Groups[1].Value[0]) + m.Groups[1].Value.Substring(1)}")
                            ));
                        }
                        break;

                    case "MS_002": // Использование var
                        if (Regex.IsMatch(line, @"\b(int|string|double)\s+\w+\s*=") && !line.Contains("var"))
                        {
                            string fixedLine = Regex.Replace(line, @"\b(int|string|double)\s+(\w+\s*=)", "var $2");
                            results.Add(new AnalysisResult(
                                lineNumber,
                                "Code Style",
                                $"{rule.Message} ({styleGuideName})",
                                rule.Suggestion,
                                line.Trim(),
                                fixedLine.Trim()
                            ));
                        }
                        break;
                }
            }

            return results;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            // Проверяем по расширению файла
            if (!string.IsNullOrEmpty(fileExtension))
            {
                if (fileExtension.ToLower() == ".cs")
                    return true;
            }

            // Проверяем по содержимому кода
            return code.Contains("using ") &&
                   (code.Contains("namespace ") || code.Contains("class ") || code.Contains("public ") || code.Contains("private "));
        }
    }
}