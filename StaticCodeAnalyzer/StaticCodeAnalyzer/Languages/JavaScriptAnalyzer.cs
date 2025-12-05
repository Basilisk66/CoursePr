using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class JavaScriptAnalyzer : CodeAnalyzer
    {
        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Правило 1: Проверка на использование == вместо ===
                if ((line.Contains(" ==") || line.Contains("!=")) &&
                    !line.Contains("===") && !line.Contains("!=="))
                {
                    string fixedLine = line.Replace(" ==", " ===").Replace("!=", "!==");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Type Safety",
                        "Use strict equality (===) instead of loose equality (==).",
                        "Replace == with === and != with !==",
                        line.Trim(),
                        fixedLine.Trim()
                    ));
                }

                // Правило 2: Проверка на var (использовать let/const)
                if (line.Contains(" var "))
                {
                    string fixedLine = line.Replace(" var ", " let ");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Modern JS",
                        "Use let/const instead of var.",
                        "Replace var with let or const.",
                        line.Trim(),
                        fixedLine.Trim()
                    ));
                }

                // Правило 3: Проверка точки с запятой
                var statementMatch = Regex.Match(line.Trim(), @"^[^{}\[\]()]*[a-zA-Z0-9_\)\}\]]$");
                if (statementMatch.Success && !line.Trim().EndsWith(";") &&
                    !line.Trim().EndsWith("{") && !string.IsNullOrWhiteSpace(line) &&
                    !line.Trim().StartsWith("//") && !line.Trim().StartsWith("/*") &&
                    !line.Trim().StartsWith("*") && !line.Trim().StartsWith("if") &&
                    !line.Trim().StartsWith("for") && !line.Trim().StartsWith("while") &&
                    !line.Trim().StartsWith("function") && !line.Trim().StartsWith("class"))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Missing semicolon at end of statement.",
                        "Add semicolon at the end of the line.",
                        line.Trim(),
                        line.Trim() + ";"
                    ));
                }

                // Правило 4: Проверка строковых кавычек
                if (line.Contains("'") && !line.Contains("`") && !line.Contains('"'))
                {
                    // Проверяем, что это не строка с апострофом внутри
                    var singleQuoteMatch = Regex.Match(line, @"'[^']*'");
                    if (singleQuoteMatch.Success && !line.Contains("I'm") && !line.Contains("don't"))
                    {
                        results.Add(new AnalysisResult(
                            lineNumber,
                            "Style",
                            "Use double quotes for strings.",
                            "Replace single quotes with double quotes.",
                            line.Trim(),
                            line.Replace("'", "\"")
                        ));
                    }
                }

                // Правило 5: Проверка console.log в production коде
                if (line.Contains("console.log"))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Debugging",
                        "Remove console.log from production code.",
                        "Remove or comment out console.log statement.",
                        line.Trim(),
                        "// " + line.Trim()
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
                if (fileExtension.ToLower() == ".js" || fileExtension.ToLower() == ".javascript")
                    return true;
            }

            // Проверяем по содержимому кода
            if (string.IsNullOrEmpty(code))
                return false;

            string cleanCode = code.Trim();

            // Ключевые слова JavaScript/ES6+
            bool hasJsKeywords =
                cleanCode.Contains("function") ||
                cleanCode.Contains("=>") || // Стрелочные функции
                cleanCode.Contains("const ") ||
                cleanCode.Contains("let ") ||
                cleanCode.Contains("var ") ||
                cleanCode.Contains("console.log") ||
                cleanCode.Contains("document.") ||
                cleanCode.Contains("window.") ||
                cleanCode.Contains("addEventListener") ||
                cleanCode.Contains("querySelector") ||
                cleanCode.Contains("getElementById") ||
                cleanCode.Contains("import ") && cleanCode.Contains("from ") ||
                cleanCode.Contains("export ") ||
                cleanCode.Contains("Promise") ||
                cleanCode.Contains("async ") ||
                cleanCode.Contains("await ");

            // JS-специфичные конструкции
            bool hasJsSyntax =
                cleanCode.Contains("${") && cleanCode.Contains("}") || // Template literals
                cleanCode.Contains("`") && cleanCode.Contains("`") || // Backticks
                cleanCode.Contains("=== ") || cleanCode.Contains("!==") ||
                cleanCode.Contains("() =>") || // Arrow functions
                cleanCode.Contains(".then(") || cleanCode.Contains(".catch(");

            // Исключаем другие языки
            bool notOtherLanguage =
                !cleanCode.Contains("public class") &&    // Не Java
                !cleanCode.Contains("System.out") &&      // Не Java
                !cleanCode.Contains("using ") &&          // Не C#
                !cleanCode.Contains("namespace ") &&      // Не C#
                !cleanCode.Contains("cout") &&            // Не C++
                !cleanCode.Contains("#include") &&        // Не C++
                !cleanCode.Contains("def ") &&            // Не Python
                !cleanCode.Contains("import ") && !cleanCode.Contains("from ") && // Не Python imports
                !cleanCode.Contains("print(") &&          // Не Python
                !cleanCode.Contains("<html") &&           // Не HTML
                !cleanCode.Contains("<div") &&            // Не HTML
                !cleanCode.Contains("<!DOCTYPE");         // Не HTML

            // Для JS должно быть достаточно ключевых слов или синтаксиса
            return (hasJsKeywords || hasJsSyntax) && notOtherLanguage;
        }
    }
}