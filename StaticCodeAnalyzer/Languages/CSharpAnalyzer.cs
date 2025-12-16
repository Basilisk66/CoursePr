using StaticCodeAnalyzer.Models;
using StaticCodeAnalyzer.Models.StyleGuides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Analyzers
{
    public class CSharpAnalyzer : CodeAnalyzer
    {
        private static readonly HashSet<string> _csharpKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        protected override void InitializeStyleGuides()
        {
            base.InitializeStyleGuides();
            _styleGuides.Add(new MicrosoftStyleGuide());
        }

        protected override void InitializeKeywords()
        {
            // Используем тот же набор ключевых слов для базовых проверок
            _keywords = new HashSet<string>(_csharpKeywords, StringComparer.OrdinalIgnoreCase);
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // 1. Пробелы вокруг операторов
            results.AddRange(CheckOperatorSpacing(lines));
            // 2. Точка с запятой
            results.AddRange(CheckSemicolon(lines));
            // 3. Корректность конструкций (расширенная проверка)
            results.AddRange(CheckControlStructures(lines));
            // 4. Отступы (4 пробела)
            results.AddRange(CheckIndentation(lines));
            // 5. Пустые строки (теперь удаляет все пустые строки с фиксом)
            results.AddRange(CheckEmptyLines(lines));
            // 6. Именование
            results.AddRange(CheckNamingConvention(lines));
            // 7. Использование var
            results.AddRange(CheckVarUsage(lines));
            // 8. Фигурные скобки на новой строке
            results.AddRange(CheckBracePlacement(lines));
            // 9. Null checks и использование безопасных операторов
            results.AddRange(CheckNullSafety(lines));
            // 10. Имена-ключевые слова
            results.AddRange(CheckKeywordAsNames(lines));
            // 11. Исправление лишних = в условиях
            results.AddRange(CheckAssignmentInCondition(lines));
            // 12. Проверка try-catch-finally
            results.AddRange(CheckTryCatchFinally(code, lines));
            // 13. Проверка switch-default и структуры switch
            results.AddRange(CheckSwitchSemicolonAndBraces(lines));
            results.AddRange(CheckSwitchDefault(code, lines));
            // 14. Проверка правильности скобок
            results.AddRange(CheckBracketSyntax(lines));
            // 15. Проверка правильности конструкций (расширенный метод, без try/switch)
            results.AddRange(CheckExtendedControlStructures(lines));

            return results;
        }

        private List<AnalysisResult> CheckOperatorSpacing(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;
                if (Regex.IsMatch(line, @"\w[=+\-*/%&|^<>!]\w"))
                {
                    // Сохраняем отступы
                    string indent = new string(' ', line.Length - line.TrimStart().Length);
                    string trimmedLine = line.Trim();
                    string fixedLine = Regex.Replace(trimmedLine, @"(\w)([=+\-*/%&|^<>!])(\w)", "$1 $2 $3");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Formatting",
                        "Missing spaces around operators",
                        "Add spaces around operators per C# style guide",
                        line.Trim(),
                        indent + fixedLine // Сохраняем отступы
                    ));
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
                string trimmedLine = line.Trim();
                int lineNumber = i + 1;

                if (string.IsNullOrWhiteSpace(line) || trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
                    continue;

                // Сохраняем отступы
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Игнорируем объявления и конструкции без точек с запятой
                if (trimmedLine.Contains("namespace") || trimmedLine.Contains("class ") || trimmedLine.Contains("interface ") ||
                    trimmedLine.Contains("struct ") || trimmedLine.Contains("enum ") || trimmedLine.EndsWith("{") || trimmedLine.EndsWith("}") ||
                    trimmedLine.Contains("using ") || Regex.IsMatch(trimmedLine, @"^\s*#") ||
                    trimmedLine.StartsWith("if ") || trimmedLine.StartsWith("for ") || trimmedLine.StartsWith("while ") ||
                    trimmedLine.StartsWith("foreach ") || trimmedLine.StartsWith("switch ") || trimmedLine.StartsWith("do ") ||
                    trimmedLine.StartsWith("try") || trimmedLine.StartsWith("catch") || trimmedLine.StartsWith("finally") ||
                    trimmedLine.StartsWith("else") || trimmedLine.StartsWith("get;") || trimmedLine.StartsWith("set;") ||
                    trimmedLine.StartsWith("get =>") || trimmedLine.StartsWith("set =>") || trimmedLine.StartsWith("[") ||
                    trimmedLine.Contains("=>") || trimmedLine.Contains(":")) // Игнорируем метки case:
                    continue;

                // Дополнительные случаи, требующие точки с запятой
                bool needsSemicolon = false;

                // 1. Присваивания
                if (Regex.IsMatch(trimmedLine, @"^[a-zA-Z_][\w.]*\s*=.*") && !trimmedLine.EndsWith(";") && !trimmedLine.Contains("=>"))
                    needsSemicolon = true;

                // 2. Вызовы методов без точки с запятой
                else if (Regex.IsMatch(trimmedLine, @"^[a-zA-Z_][\w.]*\s*\(.*\)") && !trimmedLine.EndsWith(";") &&
                        !trimmedLine.Contains("if") && !trimmedLine.Contains("while") && !trimmedLine.Contains("for"))
                    needsSemicolon = true;

                // 3. Ключевые слова return, throw, break, continue, yield
                else if (Regex.IsMatch(trimmedLine, @"^(return|throw|break|continue|yield return|yield break)\b") &&
                        !trimmedLine.EndsWith(";") && !trimmedLine.EndsWith("{"))
                    needsSemicolon = true;

                // 4. Инкремент/декремент
                else if (Regex.IsMatch(trimmedLine, @"^[\w.]+(\+\+|--)") && !trimmedLine.EndsWith(";"))
                    needsSemicolon = true;

                // 5. Создание объектов с new
                else if (Regex.IsMatch(trimmedLine, @"^new\s+\w") && !trimmedLine.EndsWith(";"))
                    needsSemicolon = true;

                // 6. Операторы +=, -=, *=, /=, %=
                else if (Regex.IsMatch(trimmedLine, @"^[a-zA-Z_][\w.]*\s*(\+=|\-=|\*=|\/=|\%=).*") && !trimmedLine.EndsWith(";"))
                    needsSemicolon = true;

                if (needsSemicolon)
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Syntax",
                        "Missing semicolon at end of statement",
                        "Add semicolon",
                        trimmedLine,
                        indent + trimmedLine + ";" // Сохраняем отступы
                    ));
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
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Проверка if, for, while, foreach, switch, do на наличие фигурных скобок
                if (trimmedLine.StartsWith("if") || trimmedLine.StartsWith("for") || trimmedLine.StartsWith("while") ||
                    trimmedLine.StartsWith("foreach") || trimmedLine.StartsWith("switch") || trimmedLine.StartsWith("do"))
                {
                    // Проверяем, есть ли фигурные скобки в этой или следующей строке
                    bool hasBraces = false;

                    // Проверяем текущую строку
                    if (trimmedLine.Contains("{"))
                        hasBraces = true;

                    // Проверяем следующую строку (если она существует)
                    if (i + 1 < lines.Length)
                    {
                        string nextLine = lines[i + 1].Trim();
                        if (nextLine == "{")
                            hasBraces = true;
                    }

                    // Если нет фигурных скобок и строка не заканчивается точкой с запятой
                    if (!hasBraces && !trimmedLine.EndsWith(";") && !trimmedLine.EndsWith("{"))
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Best Practice",
                            "Control statement without braces",
                            "Add braces to avoid bugs",
                            trimmedLine,
                            indent + trimmedLine + " {" // Сохраняем отступы
                        ));
                    }
                }

                // Проверка throw без точки с запятой
                if (trimmedLine.StartsWith("throw") && !trimmedLine.Contains(";"))
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Syntax",
                        "Missing semicolon after throw",
                        "Add semicolon",
                        trimmedLine,
                        indent + trimmedLine + ";" // Сохраняем отступы
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckIndentation(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                int leadingSpaces = line.Length - line.TrimStart().Length;
                if (leadingSpaces % 4 != 0)
                {
                    int correctIndent = (leadingSpaces / 4) * 4;
                    string fixedLine = new string(' ', correctIndent) + line.TrimStart();
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        $"Indent must be multiple of 4 spaces (got {leadingSpaces})",
                        "Fix indentation",
                        line,
                        fixedLine
                    ));
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckEmptyLines(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        "Empty line detected",
                        "Remove empty line",
                        lines[i],
                        "" // Важно: пустая строка, не null!
                    ));
                }
            }
            return results;
        }


        private List<AnalysisResult> CheckNamingConvention(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Проверка классов
                var classMatch = Regex.Match(trimmedLine, @"class\s+([a-z]\w*)");
                if (classMatch.Success)
                {
                    string name = classMatch.Groups[1].Value;
                    string fixedName = char.ToUpper(name[0]) + name.Substring(1);
                    string fixedLine = Regex.Replace(trimmedLine, @"class\s+" + name, "class " + fixedName);
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Naming",
                        "Class name must use PascalCase",
                        "Rename class",
                        trimmedLine,
                        indent + fixedLine // Сохраняем отступы
                    ));
                }

                // Методы
                var methodMatch = Regex.Match(trimmedLine, @"(void|int|string|bool|double|float|char|Task|Task<\w+>)\s+([a-z]\w*)\s*\(");
                if (methodMatch.Success)
                {
                    string name = methodMatch.Groups[2].Value;
                    string fixedName = char.ToUpper(name[0]) + name.Substring(1);
                    string fixedLine = Regex.Replace(trimmedLine, @"\b" + name + @"\s*\(", fixedName + "(");
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Naming",
                        "Method name must use PascalCase",
                        "Rename method",
                        trimmedLine,
                        indent + fixedLine // Сохраняем отступы
                    ));
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckVarUsage(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                var match = Regex.Match(trimmedLine, @"\b(int|string|bool|double|float|char)\s+(\w+)\s*=");
                if (match.Success && !trimmedLine.Contains("var"))
                {
                    string varName = match.Groups[2].Value;
                    string fixedLine = Regex.Replace(trimmedLine, $@"\b(int|string|bool|double|float|char)\s+{varName}\s*=", $"var {varName} =");
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Code Style",
                        "Use 'var' when type is obvious",
                        "Replace explicit type with 'var'",
                        trimmedLine,
                        indent + fixedLine // Сохраняем отступы
                    ));
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
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Проверяем, если строка заканчивается на "{" но не начинается с "{" 
                // (фигурная скобка должна быть на отдельной строке)
                if (trimmedLine.EndsWith("{") && trimmedLine.Length > 1 &&
                    !trimmedLine.StartsWith("{") && !trimmedLine.Contains("=>") && !trimmedLine.Contains(" = "))
                {
                    // Разделяем строку на декларацию и фигурную скобку
                    string declaration = trimmedLine.Substring(0, trimmedLine.Length - 1).TrimEnd();
                    string fixedCode = indent + declaration + "\n" + indent + "{";

                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        "Opening brace should be on new line (Microsoft style)",
                        "Move brace to new line",
                        trimmedLine,
                        fixedCode
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckNullSafety(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                if (trimmedLine.Contains(" == null") || trimmedLine.Contains("!= null"))
                {
                    string suggestion = "Consider using 'is null' or 'is not null' for null checks";
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Null Safety",
                        "Direct null comparison detected",
                        suggestion,
                        trimmedLine,
                        indent + trimmedLine // Сохраняем отступы
                    ));
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckUnusedVariables(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();
            var usedVars = new HashSet<string>();
            var declaredVars = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var decl = Regex.Match(line, @"\b(var|int|string|bool|double|float|char)\s+(\w+)\s*=");
                if (decl.Success)
                {
                    string name = decl.Groups[2].Value;
                    declaredVars[name] = i + 1;
                }

                var usage = Regex.Matches(line, @"\b(\w+)\b");
                foreach (Match m in usage)
                {
                    usedVars.Add(m.Value);
                }
            }

            foreach (var kvp in declaredVars)
            {
                if (!usedVars.Contains(kvp.Key) && kvp.Key.Length > 1)
                {
                    string indent = new string(' ', lines[kvp.Value - 1].Length - lines[kvp.Value - 1].TrimStart().Length);
                    results.Add(new AnalysisResult(
                        kvp.Value,
                        "Code Smell",
                        $"Unused variable '{kvp.Key}'",
                        "Remove unused variable",
                        lines[kvp.Value - 1].Trim(),
                        indent + "// Removed: " + lines[kvp.Value - 1].Trim() // Сохраняем отступы
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckKeywordAsNames(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;

                string fixedLine = trimmedLine;
                bool changed = false;

                void RenameIfKeyword(string name)
                {
                    if (string.IsNullOrEmpty(name))
                        return;

                    if (_csharpKeywords.Contains(name) && !fixedLine.Contains("@" + name))
                    {
                        // Переименовываем все вхождения идентификатора как целого слова
                        string pattern = $@"\b{name}\b";
                        string replacement = name + "_";
                        string newFixed = Regex.Replace(fixedLine, pattern, replacement);
                        if (newFixed != fixedLine)
                        {
                            fixedLine = newFixed;
                            changed = true;
                        }
                    }
                }

                // 1. Переменные и поля: тип + имя
                var varDeclMatches = Regex.Matches(
                    trimmedLine,
                    @"\b(?:var|bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|short|ushort|string|object)\s+(?<name>\w+)\s*(?=[=;,{])");
                foreach (Match m in varDeclMatches)
                {
                    RenameIfKeyword(m.Groups["name"].Value);
                }

                // 2. Классы/структуры/интерфейсы/enum
                var typeDeclMatches = Regex.Matches(
                    trimmedLine,
                    @"\b(?:class|struct|interface|enum)\s+(?<name>\w+)");
                foreach (Match m in typeDeclMatches)
                {
                    RenameIfKeyword(m.Groups["name"].Value);
                }

                // 3. Методы
                var methodDeclMatches = Regex.Matches(
                    trimmedLine,
                    @"\b(?:public|private|protected|internal|static|virtual|override|sealed|async|extern|new|partial|\s)*" +
                    @"(?:void|bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|short|ushort|string|object|Task|Task<\w+>)\s+(?<name>\w+)\s*\(");
                foreach (Match m in methodDeclMatches)
                {
                    RenameIfKeyword(m.Groups["name"].Value);
                }

                // 4. Параметры методов
                var paramMatches = Regex.Matches(
                    trimmedLine,
                    @"\((?<params>[^)]*)\)");
                foreach (Match pm in paramMatches)
                {
                    string paramBlock = pm.Groups["params"].Value;
                    var singleParams = paramBlock.Split(',')
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p));

                    foreach (var param in singleParams)
                    {
                        // Находим последнее слово в параметре — это имя
                        var words = Regex.Matches(param, @"\b\w+\b")
                                         .Cast<Match>()
                                         .Select(m => m.Value)
                                         .ToList();
                        if (words.Count >= 1)
                        {
                            string paramName = words.Last();
                            // Не переименовываем, если это params-модификатор
                            if (!string.Equals(paramName, "params", StringComparison.OrdinalIgnoreCase))
                            {
                                RenameIfKeyword(paramName);
                            }
                        }
                    }
                }

                if (changed)
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Naming",
                        "Identifier name conflicts with C# keyword",
                        "Rename identifier by appending '_' suffix",
                        trimmedLine,
                        indent + fixedLine // Сохраняем отступы
                    ));
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckAssignmentInCondition(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);
                int lineNumber = i + 1;

                // Обрабатываем только конструкции с круглыми скобками
                if (!trimmedLine.Contains("(") || !trimmedLine.Contains(")"))
                    continue;

                // Ищем условия в if/while/for
                var match = Regex.Match(trimmedLine, @"\b(if|while|for)\s*\((?<cond>[^)]*)\)");
                if (!match.Success)
                    continue;

                string condition = match.Groups["cond"].Value;

                // Ищем избыточные '=' после корректного '==' (пример: x == = = = 10)
                string fixedCondition = Regex.Replace(
                    condition,
                    @"==(\s*=\s*)+",
                    "==");

                // Ничего не меняем, если не было "== = = ..."
                if (fixedCondition == condition)
                    continue;

                string fixedLine = trimmedLine.Substring(0, match.Groups["cond"].Index)
                                  + fixedCondition
                                  + trimmedLine.Substring(match.Groups["cond"].Index + condition.Length);

                results.Add(new AnalysisResult(
                    lineNumber,
                    "Syntax",
                    "Too many '=' characters in condition",
                    "Reduce multiple '=' after '==' to a single comparison operator '=='",
                    trimmedLine,
                    indent + fixedLine // Сохраняем отступы
                ));
            }
            return results;
        }

        private List<AnalysisResult> CheckBracketSyntax(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);
                int lineNumber = i + 1;

                // Проверка 1: Неправильные пробелы в вызовах методов
                var methodCallMatch = Regex.Match(trimmedLine, @"(\w+)\s*\.\s*(\w+)\s*\(\s*([^)]*)\s*\)");
                if (methodCallMatch.Success)
                {
                    if (trimmedLine.Contains(" .") || trimmedLine.Contains(". ") ||
                        trimmedLine.Contains(" (") || trimmedLine.Contains("( ") || trimmedLine.Contains(" )"))
                    {
                        // Исправляем: удаляем лишние пробелы
                        string fixedLine = trimmedLine
                            .Replace(" .", ".")
                            .Replace(". ", ".")
                            .Replace(" (", "(")
                            .Replace("( ", "(")
                            .Replace(" )", ")");

                        fixedLine = fixedLine.Replace("..", ".");

                        results.Add(new AnalysisResult(
                            lineNumber,
                            "Formatting",
                            "Incorrect spacing in method call",
                            "Remove extra spaces around dots and parentheses",
                            trimmedLine,
                            indent + fixedLine // Сохраняем отступы
                        ));
                    }
                }

                // Проверка 2: Закрывающая круглая скобка на новой строке
                if (trimmedLine == ")")
                {
                    if (i > 0)
                    {
                        string prevLine = lines[i - 1].Trim();

                        if (prevLine.Contains("(") && !prevLine.Contains(")"))
                        {
                            results.Add(new AnalysisResult(
                                lineNumber,
                                "Syntax",
                                "Closing parenthesis on separate line",
                                "Move closing parenthesis to the same line",
                                trimmedLine,
                                "// Should be on previous line with opening parenthesis"
                            ));
                        }
                    }
                }

                // Проверка 3: Недопустимые пробелы внутри скобок в условиях
                if (trimmedLine.Contains("if") || trimmedLine.Contains("while") || trimmedLine.Contains("for"))
                {
                    var conditionMatch = Regex.Match(trimmedLine, @"(if|while|for)\s*\(\s*([^)]+)\s*\)");
                    if (conditionMatch.Success)
                    {
                        string condition = conditionMatch.Groups[2].Value;
                        if (condition.StartsWith(" ") || condition.EndsWith(" "))
                        {
                            string cleanedCondition = condition.Trim();
                            string fixedLine = Regex.Replace(trimmedLine,
                                $@"{Regex.Escape(conditionMatch.Groups[0].Value)}",
                                $"{conditionMatch.Groups[1].Value} ({cleanedCondition})");

                            results.Add(new AnalysisResult(
                                lineNumber,
                                "Formatting",
                                "Extra spaces inside parentheses",
                                "Remove spaces inside parentheses",
                                trimmedLine,
                                indent + fixedLine // Сохраняем отступы
                            ));
                        }
                    }
                }
            }

            return results;
        }

        // НОВЫЙ МЕТОД: Расширенная проверка конструкций управления
        private List<AnalysisResult> CheckExtendedControlStructures(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);
                int lineNumber = i + 1;

                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;

                string structureType = GetStructureType(trimmedLine);
                // Расширенная проверка не обрабатывает try/switch — для них есть отдельные методы
                if (string.IsNullOrEmpty(structureType) || structureType == "try" || structureType == "switch")
                    continue;

                // Проверяем наличие скобок
                bool hasParentheses = HasParentheses(trimmedLine);
                bool hasOpeningCurlyBrace = trimmedLine.Contains("{");

                // Проверяем следующую строку на наличие открывающей фигурной скобки
                bool nextLineHasCurlyBrace = false;
                if (!hasOpeningCurlyBrace && i + 1 < lines.Length)
                {
                    string nextLineTrimmed = lines[i + 1].Trim();
                    nextLineHasCurlyBrace = nextLineTrimmed == "{";
                }

                bool hasCurlyBrace = hasOpeningCurlyBrace || nextLineHasCurlyBrace;

                // Определяем, нужны ли исправления
                bool needsParentheses = !hasParentheses && ShouldHaveParentheses(structureType);
                bool needsCurlyBrace = !hasCurlyBrace && ShouldHaveCurlyBrace(structureType);

                // Для for проверяем наличие двух точек с запятой
                bool needsForSemicolons = structureType == "for" && hasParentheses && !HasTwoSemicolons(trimmedLine);

                // Если все проверки пройдены - конструкция валидна, пропускаем
                if (!needsParentheses && !needsCurlyBrace && !needsForSemicolons)
                    continue;

                string fixedCode = GenerateFix(
                    trimmedLine, structureType, indent,
                    needsParentheses, needsCurlyBrace,
                    false, // needsCatchBlock
                    false, // needsDefaultCase
                    needsForSemicolons,
                    nextLineHasCurlyBrace
                );

                string errorMessage = BuildErrorMessage(
                    structureType, needsParentheses, needsCurlyBrace,
                    false, false, needsForSemicolons);

                string suggestion = BuildSuggestion(
                    structureType, needsParentheses, needsCurlyBrace,
                    false, false, needsForSemicolons);

                results.Add(new AnalysisResult(
                    lineNumber,
                    "Syntax",
                    errorMessage,
                    suggestion,
                    trimmedLine,
                    fixedCode
                ));
            }

            return results;
        }
        private string GetStructureType(string line)
        {
            if (line.StartsWith("if")) return "if";
            if (line.StartsWith("while")) return "while";
            if (line.StartsWith("for")) return "for";
            if (line.StartsWith("foreach")) return "foreach";
            if (line.StartsWith("do")) return "do";
            return "";
        }

        private bool HasParentheses(string line)
        {
            return line.Contains("(") && line.Contains(")");
        }

        private bool ShouldHaveParentheses(string structureType)
        {
            return structureType != "try";
        }

        private bool ShouldHaveCurlyBrace(string structureType)
        {
            return structureType != "do"; // do может быть без фигурных скобок
        }

        private bool HasTwoSemicolons(string line)
        {
            if (!line.Contains("(") || !line.Contains(")"))
                return false;

            int openParen = line.IndexOf('(');
            int closeParen = line.IndexOf(')');
            if (openParen < 0 || closeParen < 0 || closeParen <= openParen)
                return false;

            string inside = line.Substring(openParen + 1, closeParen - openParen - 1);
            return inside.Count(c => c == ';') == 2;
        }

        // HasCatchOrFinally / HasDefaultCase больше не используются в расширенных проверках

        private string GenerateFix(
            string originalLine, string structureType, string indent,
            bool needsParentheses, bool needsCurlyBrace,
            bool needsCatchBlock, bool needsDefaultCase,
            bool needsForSemicolons,
            bool nextLineHasCurlyBrace)
        {
            string fixedLine = originalLine;

            // Обрабатываем скобки
            if (needsParentheses)
            {
                if (structureType == "if" || structureType == "while" || structureType == "foreach")
                {
                    // if, while, foreach: добавляем круглые скобки с условием
                    if (originalLine.EndsWith("{"))
                    {
                        fixedLine = originalLine.Replace(" {", " (condition) {");
                    }
                    else if (originalLine.Contains("{"))
                    {
                        int braceIndex = originalLine.IndexOf('{');
                        fixedLine = originalLine.Insert(braceIndex, " (condition)");
                    }
                    else
                    {
                        fixedLine = originalLine + " (condition)";
                    }
                }
                else if (structureType == "for")
                {
                    if (needsForSemicolons)
                    {
                        // Уже есть круглые скобки, но нет двух точек с запятой
                        int openParen = originalLine.IndexOf('(');
                        int closeParen = originalLine.IndexOf(')');
                        if (openParen >= 0 && closeParen > openParen)
                        {
                            string inside = originalLine.Substring(openParen + 1, closeParen - openParen - 1).Trim();
                            if (string.IsNullOrWhiteSpace(inside))
                            {
                                fixedLine = originalLine.Replace("()", "(init; condition; increment)");
                            }
                            else
                            {
                                // Пытаемся исправить существующее содержимое
                                int semicolons = inside.Count(c => c == ';');
                                if (semicolons == 0)
                                    fixedLine = originalLine.Replace(inside, inside + "; condition; increment");
                                else if (semicolons == 1)
                                    fixedLine = originalLine.Replace(inside, inside + "; increment");
                            }
                        }
                    }
                    else
                    {
                        // Нет круглых скобок
                        fixedLine = originalLine + " (init; condition; increment)";
                    }
                }
                else if (structureType == "switch")
                {
                    fixedLine = originalLine + " (expression)";
                }
            }

            // Обрабатываем фигурные скобки
            if (needsCurlyBrace)
            {
                if (!fixedLine.Contains("{"))
                {
                    fixedLine += " {\n" + indent + "    /* body */\n" + indent + "}";
                }
            }

            // Обработка try-catch и switch-default вынесена в отдельные методы и здесь не выполняется

            // Сохраняем отступ
            if (!fixedLine.StartsWith(indent) && fixedLine.Contains("\n"))
            {
                // Добавляем отступ к каждой строке
                string[] fixLines = fixedLine.Split('\n');
                for (int j = 0; j < fixLines.Length; j++)
                {
                    if (!string.IsNullOrWhiteSpace(fixLines[j]) && !fixLines[j].StartsWith(indent))
                    {
                        fixLines[j] = indent + fixLines[j];
                    }
                }
                fixedLine = string.Join("\n", fixLines);
            }
            else if (!fixedLine.StartsWith(indent))
            {
                fixedLine = indent + fixedLine;
            }

            return fixedLine;
        }

        private string BuildErrorMessage(
            string structureType, bool needsParentheses, bool needsCurlyBrace,
            bool needsCatchBlock, bool needsDefaultCase, bool needsForSemicolons)
        {
            if (needsParentheses && needsCurlyBrace)
                return $"{structureType} statement missing both parentheses and curly braces";
            if (needsParentheses)
                return $"{structureType} statement missing parentheses";
            if (needsCurlyBrace)
                return $"{structureType} statement missing curly braces";
            if (needsForSemicolons)
                return "for loop should have three parts separated by semicolons";
            if (needsCatchBlock)
                return "try block should have catch or finally";
            if (needsDefaultCase)
                return "switch statement missing default case";

            return $"{structureType} statement needs correction";
        }

        private string BuildSuggestion(
            string structureType, bool needsParentheses, bool needsCurlyBrace,
            bool needsCatchBlock, bool needsDefaultCase, bool needsForSemicolons)
        {
            if (needsParentheses && needsCurlyBrace)
                return $"Add parentheses and curly braces to {structureType} statement";
            if (needsParentheses)
                return $"Add parentheses to {structureType} statement";
            if (needsCurlyBrace)
                return $"Add curly braces to {structureType} statement";
            if (needsForSemicolons)
                return "Add missing semicolons in for loop";
            if (needsCatchBlock)
                return "Add catch or finally block";
            if (needsDefaultCase)
                return "Add default case to switch statement";

            return $"Correct the {structureType} statement";
        }

        private List<AnalysisResult> CheckSwitchSemicolonAndBraces(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);
                int lineNumber = i + 1;

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                // 1. Некорректный switch(x);
                var semicolonMatch = Regex.Match(trimmed, @"^switch\s*\([^)]*\)\s*;");
                if (semicolonMatch.Success)
                {
                    string fixedLine = Regex.Replace(trimmed, @";\s*$", "");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Syntax",
                        "Unnecessary semicolon after switch expression",
                        "Remove semicolon: use 'switch (expr)' with a switch block",
                        trimmed,
                        indent + fixedLine
                    ));
                }

                // 2. Проверка наличия { } или следующей строки с {
                if (trimmed.StartsWith("switch"))
                {
                    bool hasBraceSameLine = trimmed.Contains("{");
                    bool hasBraceNextLine = false;

                    if (!hasBraceSameLine && i + 1 < lines.Length)
                    {
                        string nextTrimmed = lines[i + 1].Trim();
                        hasBraceNextLine = nextTrimmed == "{";
                    }

                    if (!hasBraceSameLine && !hasBraceNextLine)
                    {
                        results.Add(new AnalysisResult(
                            lineNumber,
                            "Best Practice",
                            "switch statement should be followed by a block with '{' '}'",
                            "Add '{' after switch expression and matching '}' after cases",
                            trimmed,
                            null // Только рекомендация, не ломаем структуру
                        ));
                    }
                }
            }

            return results;
        }
        private List<AnalysisResult> CheckTryCatchFinally(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();
            int tryLine = 0;
            List<int> catchLines = new List<int>();
            int finallyLine = 0;
            bool inTryBlock = false;
            int braceCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("try"))
                {
                    tryLine = i + 1;
                    inTryBlock = true;
                    if (line.Contains("{")) braceCount++;
                }
                else if (line.StartsWith("catch"))
                {
                    catchLines.Add(i + 1);
                }
                else if (line.StartsWith("finally"))
                {
                    finallyLine = i + 1;
                }

                // Отслеживаем фигурные скобки для определения конца try блока
                if (inTryBlock)
                {
                    if (line.Contains("{")) braceCount++;
                    if (line.Contains("}")) braceCount--;

                    if (braceCount == 0 && inTryBlock)
                    {
                        // Конец try блока
                        if (catchLines.Count == 0 && finallyLine == 0)
                        {
                            string indent = new string(' ', lines[tryLine - 1].Length - lines[tryLine - 1].TrimStart().Length);
                            results.Add(new AnalysisResult(
                                tryLine,
                                "Best Practice",
                                "try block without catch or finally",
                                "Add catch or finally block",
                                "try { ... }",
                                indent + "try \n" +
                                indent + "{\n" +
                                indent + "    // your code\n" +
                                indent + "} \n" +
                                indent + "catch (Exception ex) \n" +
                                indent + "{\n" +
                                indent + "    // handle exception\n" +
                                indent + "}"
                            ));
                        }
                        inTryBlock = false;
                    }
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckSwitchDefault(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();

            // Ищем switch блоки
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("switch"))
                {
                    int switchLine = i + 1;
                    bool hasDefault = false;
                    bool inSwitchBlock = false;
                    int braceCount = 0;

                    // Ищем внутри switch блока
                    for (int j = i; j < lines.Length; j++)
                    {
                        string innerLine = lines[j].Trim();

                        if (innerLine.StartsWith("switch"))
                        {
                            inSwitchBlock = true;
                            if (innerLine.Contains("{"))
                                braceCount++;
                        }

                        if (inSwitchBlock)
                        {
                            if (innerLine.Contains("{")) braceCount++;
                            if (innerLine.Contains("}")) braceCount--;

                            if (innerLine.StartsWith("default:"))
                            {
                                hasDefault = true;
                                break;
                            }

                            // Если вышли из switch блока
                            if (braceCount == 0 && inSwitchBlock)
                                break;
                        }
                    }

                    if (!hasDefault)
                    {
                        string indent = new string(' ', lines[i].Length - lines[i].TrimStart().Length);
                        results.Add(new AnalysisResult(
                            switchLine,
                            "Best Practice",
                            "switch statement missing default case",
                            "Add default case to switch statement",
                            line,
                            indent + line + "\n" + indent + "    // Add: default: break;"
                        ));
                    }
                }
            }

            return results;
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            if (!string.IsNullOrEmpty(fileExtension) && fileExtension.ToLower() == ".cs")
                return true;
            return code.Contains("using ") && (code.Contains("namespace ") || code.Contains("class "));
        }
    }
}