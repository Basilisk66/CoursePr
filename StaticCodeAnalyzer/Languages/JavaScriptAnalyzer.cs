using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class JavaScriptAnalyzer : CodeAnalyzer
    {
        protected override void InitializeKeywords()
        {
            _keywords = new HashSet<string>
            {
                "abstract", "arguments", "await", "boolean", "break", "byte", "case", "catch",
                "char", "class", "const", "continue", "debugger", "default", "delete", "do",
                "double", "else", "enum", "eval", "export", "extends", "false", "final",
                "finally", "float", "for", "function", "goto", "if", "implements", "import",
                "in", "instanceof", "int", "interface", "let", "long", "native", "new",
                "null", "package", "private", "protected", "public", "return", "short",
                "static", "super", "switch", "synchronized", "this", "throw", "throws",
                "transient", "true", "try", "typeof", "var", "void", "volatile", "while",
                "with", "yield"
            };
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Применяем все правила
            results.AddRange(CheckVarUsage(lines));
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckSemicolon(lines));
            results.AddRange(CheckStrictEquality(lines));
            results.AddRange(CheckOperatorSpacing(lines));
            results.AddRange(CheckIndentation(lines, 4));
            results.AddRange(CheckKeywordsAsNames(lines));
            results.AddRange(CheckControlStructures(lines));
            results.AddRange(CheckCodeDuplication(code));
            results.AddRange(CheckStrictMode(code));

            return results;
        }

        private List<AnalysisResult> CheckVarUsage(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                // Проверяем использование var
                if (trimmed.Contains(" var ") && !trimmed.Contains("//"))
                {
                    // Проверяем, не является ли это частью for цикла
                    if (!trimmed.StartsWith("for ("))
                    {
                        string fixedLine = trimmed.Replace(" var ", " let ");
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Modern JS",
                            "Use let/const instead of var",
                            "Replace var with let or const",
                            trimmed,
                            fixedLine
                        ));
                    }
                }
            }

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
                   line.StartsWith("import ") || line.StartsWith("export ") ||
                   line.StartsWith("function ") || line.StartsWith("class ") ||
                   line.StartsWith("if ") || line.StartsWith("for ") ||
                   line.StartsWith("while ") || line.StartsWith("do ") ||
                   line.StartsWith("switch ") || line.Contains("=>") ||
                   line.Contains("(") && line.Contains(")") && line.Contains("{");
        }

        private bool NeedsSemicolon(string line)
        {
            return Regex.IsMatch(line, @"^\s*[a-zA-Z_][\w.]*\s*=.*") ||
                   Regex.IsMatch(line, @"^\s*[a-zA-Z_][\w.]*\s*\(.*\).*") ||
                   Regex.IsMatch(line, @"^\s*(return|break|continue|throw)\b.*") ||
                   Regex.IsMatch(line, @"^\s*\w+\s*(\+\+|--)\s*$") ||
                   Regex.IsMatch(line, @"^\s*new\s+\w.*") ||
                   Regex.IsMatch(line, @"^\s*console\.\w+\(.*\)") ||
                   Regex.IsMatch(line, @"^\s*document\.\w+\(.*\)");
        }

        private List<AnalysisResult> CheckStrictEquality(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                // Проверяем использование == вместо ===
                if (trimmed.Contains(" ==") && !trimmed.Contains("==="))
                {
                    string fixedLine = trimmed.Replace(" ==", " ===");
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Type Safety",
                        "Use strict equality (===) instead of loose equality (==)",
                        "Replace == with ===",
                        trimmed,
                        fixedLine
                    ));
                }

                // Проверяем использование != вместо !==
                if (trimmed.Contains("!=") && !trimmed.Contains("!=="))
                {
                    string fixedLine = trimmed.Replace("!=", "!==");
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Type Safety",
                        "Use strict inequality (!==) instead of loose inequality (!=)",
                        "Replace != with !==",
                        trimmed,
                        fixedLine
                    ));
                }
            }

            return results;
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
                var operators = new[] { "=", "==", "===", "!=", "!==", "<", ">", "<=", ">=",
                                      "+", "-", "*", "/", "%", "&&", "||", "+=", "-=", "*=", "/=" };

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

        private List<AnalysisResult> CheckControlStructures(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
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
                            trimmed + " {\n} catch (error) {\n    // handle error\n}"
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
                        "Consider extracting duplicate code into a function",
                        lines[i],
                        "// Consider refactoring duplicate code"
                    ));
                    i += duplicateCount - 1;
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckStrictMode(string code)
        {
            var results = new List<AnalysisResult>();

            if (!code.Contains("'use strict'") && !code.Contains("\"use strict\""))
            {
                results.Add(new AnalysisResult(
                    1,
                    "Best Practice",
                    "Missing 'use strict' directive",
                    "Add 'use strict' at the beginning of the file or function",
                    "",
                    "'use strict';"
                ));
            }

            return results;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            if (!string.IsNullOrEmpty(fileExtension))
            {
                string ext = fileExtension.ToLower();
                return ext == ".js" || ext == ".jsx" || ext == ".ts" || ext == ".tsx";
            }

            return !string.IsNullOrEmpty(code) &&
                   (code.Contains("function") || code.Contains("=>") ||
                    code.Contains("const ") || code.Contains("let ") ||
                    code.Contains("console.log") || code.Contains("document.") ||
                    code.Contains("window.") || code.Contains("addEventListener") ||
                    code.Contains("import ") || code.Contains("export "));
        }
    }
}