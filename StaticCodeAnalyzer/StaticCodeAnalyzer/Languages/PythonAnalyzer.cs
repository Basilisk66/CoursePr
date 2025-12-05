using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class PythonAnalyzer : CodeAnalyzer
    {
        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Правило 1: Проверка табов vs пробелов
                if (line.StartsWith("\t"))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Use spaces instead of tabs for indentation.",
                        "Replace tabs with 4 spaces.",
                        line,
                        line.Replace("\t", "    ")
                    ));
                }

                // Правило 2: Проверка максимальной длины строки (79 символов для Python)
                if (line.Length > 79)
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Line exceeds 79 characters (PEP 8).",
                        "Break the line into multiple lines.",
                        line.Trim(),
                        line.Trim()
                    ));
                }

                // Правило 3: Проверка на использование == вместо 'is' для None
                if (line.Contains("== None") || line.Contains("!= None"))
                {
                    string fixedLine = line.Replace("== None", "is None").Replace("!= None", "is not None");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Code Style",
                        "Use 'is' or 'is not' for comparisons with None.",
                        "Replace '== None' with 'is None' and '!= None' with 'is not None'.",
                        line.Trim(),
                        fixedLine.Trim()
                    ));
                }

                // Правило 4: Проверка импортов (должны быть в начале файла)
                if (i > 10 && line.Trim().StartsWith("import ") && !IsInFunctionOrClass(lines, i))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Style",
                        "Imports should be at the top of the file.",
                        "Move import statement to the beginning of the file.",
                        line.Trim(),
                        line.Trim()
                    ));
                }

                // Правило 5: Проверка именования переменных (snake_case)
                var varMatch = Regex.Match(line, @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=");
                if (varMatch.Success)
                {
                    string varName = varMatch.Groups[1].Value;
                    if (Regex.IsMatch(varName, @"[A-Z]"))
                    {
                        results.Add(new AnalysisResult(
                            lineNumber,
                            "Naming",
                            "Variable names should use snake_case, not camelCase.",
                            $"Rename '{varName}' to snake_case.",
                            line.Trim(),
                            line.Trim() // Автоматическое переименование сложно реализовать
                        ));
                    }
                }
            }
            return results;
        }

        private bool IsInFunctionOrClass(string[] lines, int currentLine)
        {
            for (int i = 0; i < currentLine; i++)
            {
                if (lines[i].Trim().StartsWith("def ") || lines[i].Trim().StartsWith("class "))
                    return true;
                if (lines[i].Trim().StartsWith("if ") || lines[i].Trim().StartsWith("for ") ||
                    lines[i].Trim().StartsWith("while "))
                    return true;
            }
            return false;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            // Проверяем по расширению файла
            if (!string.IsNullOrEmpty(fileExtension))
            {
                if (fileExtension.ToLower() == ".py")
                    return true;
            }

            // Проверяем по содержимому кода
            return code.Contains("def ") || code.Contains("import ") ||
                   code.Contains("print(") || code.Trim().StartsWith("#!");
        }
    }
}