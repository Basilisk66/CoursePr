using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class CppAnalyzer : CodeAnalyzer
    {
        protected override void InitializeKeywords()
        {
            _keywords = new HashSet<string>
            {
                "alignas", "alignof", "and", "and_eq", "asm", "auto", "bitand", "bitor", "bool", "break",
                "case", "catch", "char", "char16_t", "char32_t", "class", "compl", "const", "constexpr",
                "const_cast", "continue", "decltype", "default", "delete", "do", "double", "dynamic_cast",
                "else", "enum", "explicit", "export", "extern", "false", "float", "for", "friend", "goto",
                "if", "inline", "int", "long", "mutable", "namespace", "new", "noexcept", "not", "not_eq",
                "nullptr", "operator", "or", "or_eq", "private", "protected", "public", "register",
                "reinterpret_cast", "return", "short", "signed", "sizeof", "static", "static_assert",
                "static_cast", "struct", "switch", "template", "this", "thread_local", "throw", "true",
                "try", "typedef", "typeid", "typename", "union", "unsigned", "using", "virtual", "void",
                "volatile", "wchar_t", "while", "xor", "xor_eq"
            };
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Применяем правила
            results.AddRange(CheckSemicolon(lines));
            results.AddRange(CheckOperatorSpacing(lines));
            results.AddRange(CheckIndentation(lines));
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckBracePlacement(lines));
            results.AddRange(CheckKeywordsAsNames(lines));
            results.AddRange(CheckControlStructures(lines));
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
                   line.StartsWith("#") ||
                   line.StartsWith("if ") || line.StartsWith("for ") ||
                   line.StartsWith("while ") || line.StartsWith("switch ") ||
                   line.StartsWith("class ") || line.StartsWith("struct ") ||
                   line.StartsWith("namespace ") || line.Contains("(") && line.Contains(")") && line.Contains("{");
        }

        private bool NeedsSemicolon(string line)
        {
            return Regex.IsMatch(line, @"^\s*[a-zA-Z_][\w.]*\s*=.*") ||
                   Regex.IsMatch(line, @"^\s*[a-zA-Z_][\w.]*\s*\(.*\).*") ||
                   Regex.IsMatch(line, @"^\s*(return|break|continue|throw|delete)\b.*") ||
                   Regex.IsMatch(line, @"^\s*\w+\s*(\+\+|--)\s*$") ||
                   Regex.IsMatch(line, @"^\s*new\s+\w.*");
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
                var operators = new[] { "=", "==", "!=", "<", ">", "<=", ">=", "+", "-", "*", "/", "%" };
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

                // Проверяем фигурные скобки на той же строке
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
                        "Opening brace should be on new line",
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
                   line.Contains("namespace ") && line.Contains("{");
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

                // Проверка for, if, while на наличие скобок
                if (trimmed.StartsWith("if") || trimmed.StartsWith("for") || trimmed.StartsWith("while"))
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
                            fixedLine = trimmed.Replace(" ", " (condition) ");
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
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckResourceLeaks(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();

            // Проверяем открытие файлов без закрытия
            var filePatterns = new[]
            {
                @"\bfstream\s*\w+\s*\([^)]+\)",
                @"\bifstream\s*\w+\s*\([^)]+\)",
                @"\bofstream\s*\w+\s*\([^)]+\)"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                foreach (var pattern in filePatterns)
                {
                    if (Regex.IsMatch(line, pattern))
                    {
                        // Проверяем, есть ли .close() в пределах 10 строк
                        bool hasClose = false;
                        for (int j = i; j < Math.Min(i + 10, lines.Length); j++)
                        {
                            if (lines[j].Contains(".close()"))
                            {
                                hasClose = true;
                                break;
                            }
                        }

                        if (!hasClose)
                        {
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Resource Management",
                                "File stream opened but not explicitly closed",
                                "Add .close() call or use RAII",
                                line.Trim(),
                                line.Trim() + " // Ensure proper closing"
                            ));
                        }
                    }
                }
            }

            return results;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            if (!string.IsNullOrEmpty(fileExtension))
            {
                string ext = fileExtension.ToLower();
                return ext == ".cpp" || ext == ".h" || ext == ".hpp" || ext == ".cc" || ext == ".cxx";
            }

            return !string.IsNullOrEmpty(code) &&
                   (code.Contains("#include") ||
                    code.Contains("std::") ||
                    code.Contains("namespace ") ||
                    code.Contains("cout") ||
                    code.Contains("cin") ||
                    code.Contains("new ") && code.Contains("delete"));
        }
    }
}