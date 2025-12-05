using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class JavaAnalyzer : CodeAnalyzer
    {
        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Правило 1: Проверка именования классов (CamelCase)
                var classMatch = Regex.Match(line, @"class\s+([a-zA-Z_][a-zA-Z0-9_]*)");
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[1].Value;
                    if (!char.IsUpper(className[0]))
                    {
                        results.Add(new AnalysisResult(
                            lineNumber,
                            "Naming",
                            "Class names should use PascalCase.",
                            $"Rename class '{className}' to start with uppercase letter.",
                            line.Trim(),
                            line.Trim() // Сложно автоматически исправить
                        ));
                    }
                }

                // Правило 2: Проверка фигурных скобок
                if (line.Trim() == "{")
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Opening brace should be on the same line as declaration.",
                        "Move opening brace to the end of previous line.",
                        line.Trim(),
                        " // { should be on previous line"
                    ));
                }

                // Правило 3: Проверка использования System.out.println
                if (line.Contains("System.out.println"))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Code Style",
                        "Consider using logger instead of System.out.println.",
                        "Replace with proper logging framework.",
                        line.Trim(),
                        line.Trim() // Замена зависит от контекста
                    ));
                }

                // Правило 4: Проверка длинных строк
                if (line.Length > 100)
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Line is too long (max 100 characters recommended).",
                        "Break the line into multiple lines.",
                        line.Trim(),
                        line.Trim()
                    ));
                }

                // Правило 5: Проверка финальных констант
                var constantMatch = Regex.Match(line, @"\b(?:public|private|protected)?\s+static\s+(\w+)\s+([A-Z_][A-Z0-9_]*)\s*=");
                if (constantMatch.Success && !line.Contains("final"))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Code Style",
                        "Constants should be declared as final.",
                        "Add 'final' modifier to constant declaration.",
                        line.Trim(),
                        line.Replace("static", "static final")
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
                if (fileExtension.ToLower() == ".java")
                    return true;
            }

            // Проверяем по содержимому кода
            if (string.IsNullOrEmpty(code))
                return false;

            string cleanCode = code.Trim();

            // Ключевые слова Java
            bool hasJavaKeywords =
                cleanCode.Contains("public class") ||
                cleanCode.Contains("class ") && (cleanCode.Contains("{") || cleanCode.Contains("}")) ||
                cleanCode.Contains("import java.") ||
                cleanCode.Contains("System.out.print") ||
                cleanCode.Contains("public static void main") ||
                cleanCode.Contains("String[] args") ||
                cleanCode.Contains("extends ") ||
                cleanCode.Contains("implements ") ||
                cleanCode.Contains("throws ") ||
                cleanCode.Contains("new ") && cleanCode.Contains("()");

            // Исключаем похожие языки
            bool notOtherLanguage =
                !cleanCode.Contains("using ") &&           // Не C#
                !cleanCode.Contains("namespace ") &&       // Не C#
                !cleanCode.Contains("cout") &&             // Не C++
                !cleanCode.Contains("#include") &&         // Не C++
                !cleanCode.Contains("def ") &&             // Не Python
                !cleanCode.Contains("import ") && cleanCode.Contains("from ") && // Не Python импорты
                !cleanCode.Contains("function") &&         // Не JavaScript
                !cleanCode.Contains("var ") &&             // Не JavaScript
                !cleanCode.Contains("let ") &&             // Не JavaScript
                !cleanCode.Contains("const ");             // Не JavaScript

            return hasJavaKeywords && notOtherLanguage;
        }
    }
}