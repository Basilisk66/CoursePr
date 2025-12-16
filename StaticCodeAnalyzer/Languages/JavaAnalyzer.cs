using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class JavaAnalyzer : CodeAnalyzer
    {
        protected override void InitializeKeywords()
        {
            _keywords = new HashSet<string>
            {
                "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char", "class",
                "const", "continue", "default", "do", "double", "else", "enum", "extends", "final",
                "finally", "float", "for", "goto", "if", "implements", "import", "instanceof", "int",
                "interface", "long", "native", "new", "package", "private", "protected", "public",
                "return", "short", "static", "strictfp", "super", "switch", "synchronized", "this",
                "throw", "throws", "transient", "try", "void", "volatile", "while"
            };
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Применяем все правила
            results.AddRange(CheckSemicolon(lines));
            results.AddRange(CheckOperatorSpacing(lines));
            results.AddRange(CheckIndentation(lines));
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckBracePlacement(lines));
            results.AddRange(CheckKeywordsAsNames(lines));
            results.AddRange(CheckControlStructures(lines));
            results.AddRange(CheckCodeDuplication(code));
            results.AddRange(CheckResourceLeaks(code, lines));

            return results;
        }

        private List<AnalysisResult> CheckSemicolon(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) ||
                    trimmed.StartsWith("//") ||
                    trimmed.StartsWith("/*") ||
                    ShouldSkipSemicolonCheck(trimmed))
                    continue;

                // Проверяем, должна ли строка заканчиваться точкой с запятой
                if (!trimmed.EndsWith(";") && NeedsSemicolon(trimmed))
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Syntax",
                        "Missing semicolon at end of statement",
                        "Add semicolon",
                        trimmed,
                        trimmed + ";"
                    ));
                }
            }
            return results;
        }

        private bool ShouldSkipSemicolonCheck(string line)
        {
            return line.EndsWith("{") || line.EndsWith("}") ||
                   line.StartsWith("import ") || line.StartsWith("package ") ||
                   line.StartsWith("class ") || line.StartsWith("interface ") ||
                   line.StartsWith("@") || // Аннотации
                   line.Contains("(") && line.Contains(")") && line.Contains("{");
        }

        private bool NeedsSemicolon(string line)
        {
            // Проверяем типы выражений, требующих точки с запятой
            return Regex.IsMatch(line, @"^\s*[a-zA-Z_][\w.]*\s*=.*") || // присваивание
                   Regex.IsMatch(line, @"^\s*[a-zA-Z_][\w.]*\s*\(.*\).*") || // вызов метода
                   Regex.IsMatch(line, @"^\s*(return|break|continue|throw)\b.*") || // ключевые слова
                   Regex.IsMatch(line, @"^\s*\w+\s*(\+\+|--)\s*$") || // инкремент/декремент
                   Regex.IsMatch(line, @"^\s*new\s+\w.*"); // создание объекта
        }

        private List<AnalysisResult> CheckOperatorSpacing(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                // Проверяем операторы без пробелов
                var operators = new[] { "=", "==", "!=", "<", ">", "<=", ">=", "+", "-", "*", "/", "%", "&&", "||" };
                foreach (var op in operators)
                {
                    var pattern = $@"(\S)({Regex.Escape(op)})(\S)";
                    if (Regex.IsMatch(trimmed, pattern))
                    {
                        string fixedLine = Regex.Replace(trimmed, pattern, "$1 $2 $3");
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Formatting",
                            $"Missing spaces around operator '{op}'",
                            "Add spaces around operator",
                            trimmed,
                            fixedLine
                        ));
                        break;
                    }
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckBracePlacement(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Проверяем фигурные скобки на той же строке (Java style обычно ставит на новой строке)
                if (trimmed.EndsWith("{") && trimmed.Length > 1 &&
                    !trimmed.StartsWith("{") &&
                    !IsBraceException(trimmed))
                {
                    string declaration = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();
                    string indent = new string(' ', line.Length - line.TrimStart().Length);
                    string fixedCode = indent + declaration + "\n" + indent + "{";

                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        "Opening brace should be on new line (Java style)",
                        "Move opening brace to a new line",
                        trimmed,
                        fixedCode
                    ));
                }
            }
            return results;
        }

        private bool IsBraceException(string line)
        {
            return line.Contains(" = {") || line.Contains("={") ||
                   line.Contains("[] {") || line.Contains("return {") ||
                   line.StartsWith("@interface");
        }

        private List<AnalysisResult> CheckControlStructures(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Проверка if, for, while, switch
                if (trimmed.StartsWith("if") || trimmed.StartsWith("for") ||
                    trimmed.StartsWith("while") || trimmed.StartsWith("switch"))
                {
                    bool hasParentheses = trimmed.Contains('(') && trimmed.Contains(')');
                    bool hasOpeningBrace = trimmed.Contains('{');

                    if (!hasOpeningBrace && i + 1 < lines.Length)
                    {
                        string nextLine = lines[i + 1].Trim();
                        hasOpeningBrace = nextLine == "{";
                    }

                    if (!hasParentheses || !hasOpeningBrace)
                    {
                        string indent = new string(' ', line.Length - line.TrimStart().Length);
                        string fixedLine = trimmed;

                        if (!hasParentheses)
                        {
                            fixedLine = trimmed + " (condition)";
                        }

                        if (!hasOpeningBrace)
                        {
                            fixedLine += " {\n" + indent + "    // code\n" + indent + "}";
                        }

                        results.Add(new AnalysisResult(
                            i + 1,
                            "Syntax",
                            "Control structure missing parentheses or braces",
                            "Add parentheses and/or braces",
                            trimmed,
                            indent + fixedLine
                        ));
                    }

                    // Специальная проверка для switch
                    if (trimmed.StartsWith("switch"))
                    {
                        // Проверяем наличие default в следующих строках
                        bool hasDefault = false;
                        int braceCount = 0;
                        bool inSwitch = false;

                        for (int j = i; j < lines.Length && j < i + 50; j++)
                        {
                            string currentLine = lines[j].Trim();

                            if (currentLine.StartsWith("switch"))
                            {
                                inSwitch = true;
                            }

                            if (inSwitch)
                            {
                                if (currentLine.Contains("{")) braceCount++;
                                if (currentLine.Contains("}")) braceCount--;

                                if (currentLine.StartsWith("default:"))
                                {
                                    hasDefault = true;
                                    break;
                                }

                                if (braceCount == 0 && j > i)
                                {
                                    break;
                                }
                            }
                        }

                        if (!hasDefault)
                        {
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Best Practice",
                                "switch statement missing default case",
                                "Add default case",
                                trimmed,
                                trimmed + " {\n    // ...\n    default:\n        break;\n}"
                            ));
                        }
                    }
                }

                // Проверка try без catch/finally
                if (trimmed.StartsWith("try"))
                {
                    bool hasCatchOrFinally = false;
                    for (int j = i + 1; j < lines.Length && j < i + 10; j++)
                    {
                        if (lines[j].Trim().StartsWith("catch") || lines[j].Trim().StartsWith("finally"))
                        {
                            hasCatchOrFinally = true;
                            break;
                        }
                    }

                    if (!hasCatchOrFinally)
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Syntax",
                            "try block without catch or finally",
                            "Add catch or finally block",
                            trimmed,
                            trimmed + " {\n} catch (Exception e) {\n    // handle exception\n}"
                        ));
                    }
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckCodeDuplication(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Простой поиск дубликатов (3+ одинаковых строк подряд)
            for (int i = 0; i < lines.Length - 2; i++)
            {
                string line1 = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line1) || line1.StartsWith("//"))
                    continue;

                int duplicateCount = 1;
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string line2 = lines[j].Trim();
                    if (line1 == line2)
                    {
                        duplicateCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (duplicateCount >= 3)
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Code Quality",
                        $"Found {duplicateCount} duplicate lines of code",
                        "Consider extracting duplicate code into a method",
                        lines[i],
                        "// Consider refactoring duplicate code"
                    ));
                    i += duplicateCount - 1;
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckResourceLeaks(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();

            // Проверяем ресурсы, требующие закрытия
            var resourcePatterns = new[]
            {
                @"\bnew\s+FileInputStream\s*\(",
                @"\bnew\s+FileOutputStream\s*\(",
                @"\bnew\s+BufferedReader\s*\(",
                @"\bnew\s+Scanner\s*\(",
                @"\bnew\s+Connection\s*\(",
                @"\bDriverManager\.getConnection\s*\("
            };

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                foreach (var pattern in resourcePatterns)
                {
                    if (Regex.IsMatch(line, pattern))
                    {
                        // Проверяем, есть ли .close() или try-with-resources
                        bool hasClose = false;
                        bool inTryWithResources = false;

                        // Проверяем try-with-resources
                        if (i > 0 && lines[i - 1].Trim().StartsWith("try ("))
                        {
                            inTryWithResources = true;
                        }

                        // Ищем .close() в следующих строках
                        for (int j = i; j < Math.Min(i + 20, lines.Length); j++)
                        {
                            if (lines[j].Contains(".close()") || lines[j].Contains(".close();"))
                            {
                                hasClose = true;
                                break;
                            }
                        }

                        if (!hasClose && !inTryWithResources)
                        {
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Resource Management",
                                "Resource opened but not explicitly closed",
                                "Use try-with-resources or add .close() call",
                                line.Trim(),
                                "try (" + line.Trim() + ") {\n    // use resource\n} // auto-closed"
                            ));
                        }
                    }
                }
            }

            return results;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            if (!string.IsNullOrEmpty(fileExtension) && fileExtension.ToLower() == ".java")
                return true;

            return !string.IsNullOrEmpty(code) &&
                   (code.Contains("public class") ||
                    code.Contains("import java.") ||
                    code.Contains("System.out.print") ||
                    code.Contains("public static void main"));
        }
    }
}