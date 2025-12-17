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
            results.AddRange(CheckCommaSpacing(lines));
            results.AddRange(CheckOperatorSpacing(lines));
            results.AddRange(CheckIndentationConsistency(lines)); // Используем улучшенную проверку
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckBracePlacement(lines));
            results.AddRange(CheckKeywordsAsNames(lines));
            results.AddRange(CheckControlStructures(lines));
            results.AddRange(CheckResourceLeaks(code, lines));

            return results;
        }

        private List<AnalysisResult> CheckIndentationConsistency(string[] lines)
        {
            var results = new List<AnalysisResult>();
            int expectedIndent = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Определяем текущий отступ
                int currentIndent = line.Length - line.TrimStart().Length;

                // Проверяем отступы для открывающих/закрывающих скобок
                if (trimmed.EndsWith("{") && !trimmed.StartsWith("namespace") && !IsBraceException(trimmed))
                {
                    // После открывающей скобки увеличиваем ожидаемый отступ
                    int correctIndent = expectedIndent;

                    if (currentIndent != correctIndent)
                    {
                        string fixedLine = new string(' ', correctIndent) + trimmed;
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Formatting",
                            $"Inconsistent indentation before opening brace (expected {correctIndent}, got {currentIndent})",
                            "Fix indentation",
                            line,
                            fixedLine
                        ));
                    }

                    expectedIndent += 4;
                }
                else if (trimmed.StartsWith("}"))
                {
                    expectedIndent -= 4;
                    if (expectedIndent < 0) expectedIndent = 0;

                    int correctIndent = expectedIndent;

                    if (currentIndent != correctIndent)
                    {
                        string fixedLine = new string(' ', correctIndent) + trimmed;
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Formatting",
                            $"Inconsistent indentation before closing brace (expected {correctIndent}, got {currentIndent})",
                            "Fix indentation",
                            line,
                            fixedLine
                        ));
                    }
                }
                else
                {
                    // Обычная строка - проверяем соответствие ожидаемому отступу
                    int correctIndent = expectedIndent;

                    if (currentIndent != correctIndent && !IsExceptionLine(trimmed))
                    {
                        string fixedLine = new string(' ', correctIndent) + trimmed;
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Formatting",
                            $"Inconsistent indentation (expected {correctIndent}, got {currentIndent})",
                            "Fix indentation",
                            line,
                            fixedLine
                        ));
                    }
                }
            }

            return results;
        }

        private bool IsExceptionLine(string line)
        {
            // Исключения для проверки отступов
            return line.StartsWith("#") || // директивы препроцессора
                   line.StartsWith("//") || // комментарии
                   (line.Contains("(") && line.Contains(")") && line.Contains("{")); // конструкторы/функции на одной строке
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
                    trimmed.StartsWith("/*"))
                    continue;

                // Проверка на лишнюю точку с запятой после управляющих структур
                if (trimmed.EndsWith(";"))
                {
                    // Проверяем, не является ли это управляющей структурой (if, for, while, switch)
                    if (HasExtraSemicolonAfterControlStructure(trimmed))
                    {
                        string indent = new string(' ', line.Length - line.TrimStart().Length);
                        // Удаляем точку с запятой и любые пробелы после неё, но сохраняем остальное содержимое
                        string withoutSemicolon = trimmed.TrimEnd(';').TrimEnd();
                        string fixedLine = indent + withoutSemicolon;
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Syntax",
                            "Extra semicolon at end of control structure",
                            "Remove semicolon",
                            line,
                            fixedLine
                        ));
                        continue;
                    }
                }

                if (ShouldSkipSemicolonCheck(trimmed))
                    continue;

                if (!trimmed.EndsWith(";") && NeedsSemicolon(trimmed))
                {
                    string indent = new string(' ', line.Length - line.TrimStart().Length);
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Syntax",
                        "Missing semicolon at end of statement",
                        "Add semicolon",
                        line,
                        indent + trimmed + ";"
                    ));
                }
            }
            return results;
        }

        private bool HasExtraSemicolonAfterControlStructure(string line)
        {
            // Проверяем, заканчивается ли строка на ; после управляющей структуры
            // Паттерны: if(...); for(...); while(...); switch(...);
            // Используем более надежный подход: находим управляющую структуру и проверяем наличие ; после закрывающей скобки

            string trimmed = line.Trim();

            // Проверяем, заканчивается ли строка на ;
            if (!trimmed.EndsWith(";"))
                return false;

            // Удаляем ; и проверяем, является ли это управляющей структурой
            string withoutSemicolon = trimmed.TrimEnd(';').TrimEnd();

            // Паттерны для управляющих структур с правильной обработкой вложенных скобок
            var controlPatterns = new[]
            {
                @"^\s*if\s*\(",
                @"^\s*for\s*\(",
                @"^\s*while\s*\(",
                @"^\s*switch\s*\("
            };

            foreach (var pattern in controlPatterns)
            {
                if (Regex.IsMatch(withoutSemicolon, pattern))
                {
                    // Проверяем, что после открывающей скобки есть соответствующая закрывающая скобка
                    // и после неё нет фигурной скобки (которая была бы частью тела)
                    int openParenIndex = withoutSemicolon.IndexOf('(');
                    if (openParenIndex >= 0)
                    {
                        int parenCount = 0;
                        bool foundMatchingParen = false;
                        for (int i = openParenIndex; i < withoutSemicolon.Length; i++)
                        {
                            if (withoutSemicolon[i] == '(')
                                parenCount++;
                            else if (withoutSemicolon[i] == ')')
                            {
                                parenCount--;
                                if (parenCount == 0)
                                {
                                    // Нашли соответствующую закрывающую скобку
                                    // Проверяем, что после неё нет фигурной скобки или другого содержимого (кроме пробелов)
                                    string afterParen = withoutSemicolon.Substring(i + 1).Trim();
                                    if (string.IsNullOrEmpty(afterParen) || !afterParen.StartsWith("{"))
                                    {
                                        foundMatchingParen = true;
                                    }
                                    break;
                                }
                            }
                        }
                        if (foundMatchingParen)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool ShouldSkipSemicolonCheck(string line)
        {
            // Пропускаем проверку для строк, которые не должны иметь точку с запятой
            return line.EndsWith("{") || line.EndsWith("}") ||
                   line.StartsWith("#") ||
                   line.StartsWith("class ") || line.StartsWith("struct ") ||
                   line.StartsWith("namespace ") ||
                   (line.Contains("(") && line.Contains(")") && line.Contains("{"));
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

                // Сохраняем отступ оригинальной строки
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Сначала проверяем многосимвольные операторы (чтобы не разбить == на = =)
                // Исключаем ++ и -- - они не должны иметь пробелов вокруг
                var multiCharOperators = new[] { "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=" };
                HashSet<int> multiCharOpPositions = new HashSet<int>();

                foreach (var op in multiCharOperators)
                {
                    var escapedOp = Regex.Escape(op);
                    var pattern = $@"(\S)({escapedOp})(\S)";
                    var matches = Regex.Matches(trimmed, pattern);

                    foreach (Match match in matches)
                    {
                        if (!IsInsideString(trimmed, match.Index))
                        {
                            // Отмечаем позиции этого оператора
                            int opStartIndex = match.Index + match.Groups[2].Index - match.Groups[0].Index;
                            for (int j = 0; j < op.Length; j++)
                            {
                                multiCharOpPositions.Add(opStartIndex + j);
                            }

                            string fixedTrimmed = trimmed.Substring(0, match.Index) +
                                match.Groups[1].Value + " " + match.Groups[2].Value + " " + match.Groups[3].Value +
                                trimmed.Substring(match.Index + match.Length);
                            string fixedLine = indent + fixedTrimmed;
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Formatting",
                                $"Missing spaces around operator '{op}'",
                                "Add spaces around operator",
                                line,
                                fixedLine
                            ));
                        }
                    }
                }

                // Теперь проверяем одиночные символьные операторы (но не те, что входят в многосимвольные)
                var singleCharOps = new[] { "=", "<", ">", "+", "-", "*", "/", "%" };
                foreach (var op in singleCharOps)
                {
                    var escapedOp = Regex.Escape(op);
                    var pattern = $@"(\S)({escapedOp})(\S)";
                    var matches = Regex.Matches(trimmed, pattern);

                    foreach (Match match in matches)
                    {
                        int opIndex = match.Index + match.Groups[2].Index - match.Index;

                        // Пропускаем, если этот оператор уже является частью многосимвольного оператора
                        if (multiCharOpPositions.Contains(opIndex))
                            continue;

                        // Проверяем контекст вокруг оператора для исключения ++ и --
                        char charBefore = opIndex > 0 ? trimmed[opIndex - 1] : ' ';
                        char charAfter = opIndex + 1 < trimmed.Length ? trimmed[opIndex + 1] : ' ';

                        // Пропускаем инкремент/декремент - они не должны иметь пробелов
                        if ((op == "+" && charAfter == '+') || (op == "-" && charAfter == '-'))
                            continue;

                        // Пропускаем если это часть многосимвольного оператора
                        bool isPartOfMultiChar = false;
                        if (op == "=")
                        {
                            if (charAfter == '=' || charBefore == '=' || charBefore == '!' ||
                                charBefore == '<' || charBefore == '>' || charBefore == '+' ||
                                charBefore == '-' || charBefore == '*' || charBefore == '/' ||
                                charBefore == '%')
                                isPartOfMultiChar = true;
                        }
                        else if (op == "<" && charAfter == '=')
                            isPartOfMultiChar = true;
                        else if (op == ">" && charAfter == '=')
                            isPartOfMultiChar = true;
                        else if (op == "+" && (charAfter == '=' || charAfter == '+'))
                            isPartOfMultiChar = true;
                        else if (op == "-" && (charAfter == '=' || charAfter == '-' || charAfter == '>'))
                            isPartOfMultiChar = true;
                        else if (op == "*" && charAfter == '=')
                            isPartOfMultiChar = true;
                        else if (op == "/" && charAfter == '=')
                            isPartOfMultiChar = true;

                        if (isPartOfMultiChar)
                            continue;

                        if (!IsInsideString(trimmed, match.Index))
                        {
                            string fixedTrimmed = trimmed.Substring(0, match.Index) +
                                match.Groups[1].Value + " " + match.Groups[2].Value + " " + match.Groups[3].Value +
                                trimmed.Substring(match.Index + match.Length);
                            string fixedLine = indent + fixedTrimmed;
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Formatting",
                                $"Missing spaces around operator '{op}'",
                                "Add spaces around operator",
                                line,
                                fixedLine
                            ));
                        }
                    }
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckCommaSpacing(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                // Сохраняем отступ оригинальной строки
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Проходим по строке вручную для проверки каждой запятой
                for (int j = 0; j < trimmed.Length; j++)
                {
                    if (trimmed[j] == ',')
                    {
                        // Пропускаем запятые внутри строковых литералов
                        if (IsInsideString(trimmed, j))
                            continue;

                        // Пропускаем запятые в числах (например, 1,000,000)
                        if (j > 0 && j < trimmed.Length - 1)
                        {
                            char charBefore = trimmed[j - 1];
                            char charAfter = trimmed[j + 1];
                            if (char.IsDigit(charBefore) && char.IsDigit(charAfter))
                                continue;
                        }

                        // Пропускаем запятые в шаблонах (например, std::vector<int, int>)
                        if (IsInsideTemplate(trimmed, j))
                            continue;

                        // Проверяем, есть ли пробел после запятой
                        if (j + 1 < trimmed.Length)
                        {
                            // ВСЕГДА ставим пробел после запятой, если следующий символ не пробел
                            // и не закрывающая скобка/угловая скобка
                            char nextChar = trimmed[j + 1];

                            // Исключения: не ставим пробел если следующий символ:
                            // 1. Пробел
                            // 2. Закрывающая скобка
                            // 3. Закрывающая угловая скобка
                            // 4. Начало комментария
                            if (!char.IsWhiteSpace(nextChar) &&
                                nextChar != ')' &&
                                nextChar != ']' &&
                                nextChar != '>' &&
                                nextChar != '}')
                            {
                                // Вставляем один пробел после запятой
                                string fixedTrimmed = trimmed.Substring(0, j + 1) + " " + trimmed.Substring(j + 1);
                                string fixedLine = indent + fixedTrimmed;

                                results.Add(new AnalysisResult(
                                    i + 1,
                                    "Formatting",
                                    "Missing space after comma",
                                    "Add space after comma",
                                    line,
                                    fixedLine
                                ));

                                // Обновляем trimmed для текущей строки
                                trimmed = fixedTrimmed;
                            }
                        }
                    }
                }
            }
            return results;
        }

        private bool IsInsideTemplate(string line, int index)
        {
            // Проверяем, находится ли запятая внутри шаблонных параметров
            int angleBracketCount = 0;
            bool inTemplate = false;

            for (int i = 0; i < index && i < line.Length; i++)
            {
                char c = line[i];

                if (c == '<')
                {
                    angleBracketCount++;
                    inTemplate = angleBracketCount > 0;
                }
                else if (c == '>')
                {
                    if (angleBracketCount > 0)
                        angleBracketCount--;
                    if (angleBracketCount == 0)
                        inTemplate = false;
                }
            }

            return inTemplate && angleBracketCount > 0;
        }

        private bool IsInsideString(string line, int index)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escaped = false;

            for (int i = 0; i < index && i < line.Length; i++)
            {
                char c = line[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                }
                else if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                }
            }

            return inSingleQuote || inDoubleQuote;
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

        protected new List<AnalysisResult> CheckKeywordsAsNames(string[] lines)
        {
            var results = new List<AnalysisResult>();
            if (!_keywords.Any()) return results;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*"))
                    continue;

                // Проверка имен классов
                var classMatch = Regex.Match(trimmedLine, @"^class\s+(\w+)\s*(?:[:{])");
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[1].Value;
                    if (_keywords.Contains(className.ToLower()))
                    {
                        string fixedName = className + "_";
                        string fixedLine = indent + trimmedLine.Replace(className, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Class name '{className}' conflicts with C++ keyword",
                            "Rename class by appending '_' suffix",
                            line,
                            fixedLine
                        ));
                    }
                }

                // Проверка имен структур
                var structMatch = Regex.Match(trimmedLine, @"^struct\s+(\w+)\s*(?:[:{])");
                if (structMatch.Success)
                {
                    string structName = structMatch.Groups[1].Value;
                    if (_keywords.Contains(structName.ToLower()))
                    {
                        string fixedName = structName + "_";
                        string fixedLine = indent + trimmedLine.Replace(structName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Struct name '{structName}' conflicts with C++ keyword",
                            "Rename struct by appending '_' suffix",
                            line,
                            fixedLine
                        ));
                    }
                }

                // Проверка имен переменных (объявление с типом)
                var varDeclPatterns = new[]
                {
                    @"\b(int|char|float|double|bool|string|auto|void|long|short|unsigned|signed)\s+(\w+)\s*[=;,\[\)]",
                    @"\b(const|static|volatile|mutable)\s+(int|char|float|double|bool|string|auto|void|long|short|unsigned|signed)\s+(\w+)\s*[=;,\[\)]",
                    @"\b(const|static|volatile|mutable)\s+(\w+)\s*[=;,\[\)]"
                };

                foreach (var pattern in varDeclPatterns)
                {
                    var varMatch = Regex.Match(trimmedLine, pattern);
                    if (varMatch.Success)
                    {
                        string varName = varMatch.Groups.Count > 3 ? varMatch.Groups[3].Value :
                                        varMatch.Groups.Count > 2 ? varMatch.Groups[2].Value : varMatch.Groups[1].Value;

                        // Пропускаем, если это тип, а не имя переменной
                        if (varName == "const" || varName == "static" || varName == "volatile" || varName == "mutable")
                            continue;

                        if (_keywords.Contains(varName.ToLower()))
                        {
                            string fixedName = varName + "_";
                            string fixedLine = line.Replace(varName, fixedName);
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Naming",
                                $"Variable name '{varName}' conflicts with C++ keyword",
                                "Rename variable by appending '_' suffix",
                                line,
                                fixedLine
                            ));
                            break; // Обработали эту строку, переходим к следующей
                        }
                    }
                }

                // Проверка имен переменных (простое присваивание)
                var simpleVarMatch = Regex.Match(trimmedLine, @"^\s*(\w+)\s*=");
                if (simpleVarMatch.Success)
                {
                    string varName = simpleVarMatch.Groups[1].Value;
                    // Пропускаем, если это не ключевое слово или это часть другого паттерна
                    if (_keywords.Contains(varName.ToLower()) && !IsTypeName(varName))
                    {
                        string fixedName = varName + "_";
                        string fixedLine = line.Replace(varName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Variable name '{varName}' conflicts with C++ keyword",
                            "Rename variable by appending '_' suffix",
                            line,
                            fixedLine
                        ));
                    }
                }

                // Проверка параметров функций
                var funcMatch = Regex.Match(trimmedLine, @"^\s*\w+\s+(\w+)\s*\([^)]*\)");
                if (funcMatch.Success)
                {
                    // Извлекаем параметры из скобок
                    var paramMatch = Regex.Match(trimmedLine, @"\(([^)]+)\)");
                    if (paramMatch.Success)
                    {
                        string paramsStr = paramMatch.Groups[1].Value;
                        // Разбираем параметры (могут быть с типами)
                        var paramNameMatches = Regex.Matches(paramsStr, @"\b(\w+)\s*(?:[,=)])");
                        foreach (Match paramMatchInner in paramNameMatches)
                        {
                            string paramName = paramMatchInner.Groups[1].Value;
                            // Пропускаем типы и ключевые слова-модификаторы
                            if (!IsTypeName(paramName) && _keywords.Contains(paramName.ToLower()))
                            {
                                string fixedName = paramName + "_";
                                string fixedLine = Regex.Replace(line, $@"\b{Regex.Escape(paramName)}\b", fixedName);
                                results.Add(new AnalysisResult(
                                    i + 1,
                                    "Naming",
                                    $"Parameter name '{paramName}' conflicts with C++ keyword",
                                    "Rename parameter by appending '_' suffix",
                                    line,
                                    fixedLine
                                ));
                            }
                        }
                    }
                }
            }
            return results;
        }

        private bool IsTypeName(string name)
        {
            // Проверяем, является ли имя типом данных
            var types = new HashSet<string> { "int", "char", "float", "double", "bool", "void", "long", "short", "unsigned", "signed", "auto" };
            return types.Contains(name.ToLower());
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

        public override string FixCode(string code, List<AnalysisResult> errors)
        {
            if (errors == null || !errors.Any())
                return code;

            var lines = code.Split('\n').ToList();
            var errorsToFix = errors
                .Where(e => e.LineNumber > 0 && e.LineNumber <= lines.Count)
                .OrderByDescending(e => e.LineNumber) // Исправляем от конца к началу
                .ToList();

            // Создаем копию для работы
            List<string> workingLines = new List<string>(lines);

            // Обрабатываем ошибки
            foreach (var error in errorsToFix)
            {
                try
                {
                    int lineIndex = error.LineNumber - 1;

                    if (lineIndex >= 0 && lineIndex < workingLines.Count)
                    {
                        string originalLine = workingLines[lineIndex];
                        string originalIndent = new string(' ', originalLine.Length - originalLine.TrimStart().Length);

                        // Проверяем, кратно ли количество пробелов 4
                        bool isIndentMultipleOf4 = originalIndent.Length % 4 == 0;

                        if (error.FixedCode == "")
                        {
                            // Удаляем строку
                            workingLines.RemoveAt(lineIndex);
                        }
                        else if (!string.IsNullOrWhiteSpace(error.FixedCode))
                        {
                            string fixedCode = error.FixedCode;

                            // Если FixedCode содержит несколько строк
                            if (fixedCode.Contains("\n"))
                            {
                                var fixedLines = fixedCode.Split('\n').ToList();

                                // Применяем правильные отступы к каждой строке
                                for (int j = 0; j < fixedLines.Count; j++)
                                {
                                    string fixedLine = fixedLines[j];

                                    // Если строка не пустая
                                    if (!string.IsNullOrWhiteSpace(fixedLine))
                                    {
                                        // Определяем текущий отступ в исправленной строке
                                        string fixedIndent = new string(' ', fixedLine.Length - fixedLine.TrimStart().Length);

                                        // Если фиксированная строка не имеет отступа, добавляем оригинальный
                                        if (fixedIndent.Length == 0 && originalIndent.Length > 0)
                                        {
                                            fixedLines[j] = originalIndent + fixedLine;
                                        }
                                        // Если имеет отступ, но мы хотим сохранить уровень вложенности
                                        else if (j > 0) // Для всех строк кроме первой
                                        {
                                            // Для вложенных блоков добавляем дополнительный отступ
                                            // Если исходный отступ кратен 4, оставляем как есть
                                            if (isIndentMultipleOf4)
                                            {
                                                fixedLines[j] = originalIndent + new string(' ', 4) + fixedLine.TrimStart();
                                            }
                                            else
                                            {
                                                // Иначе выравниваем по 4 пробелам
                                                int correctedIndent = ((originalIndent.Length / 4) + 1) * 4;
                                                fixedLines[j] = new string(' ', correctedIndent) + fixedLine.TrimStart();
                                            }
                                        }
                                        // Для первой строки (j == 0)
                                        else if (fixedIndent.Length == 0 && originalIndent.Length > 0)
                                        {
                                            // Если исправленная строка не имеет отступа, добавляем оригинальный
                                            fixedLines[j] = originalIndent + fixedLine;
                                        }
                                    }
                                }

                                // Заменяем одну строку несколькими
                                workingLines.RemoveAt(lineIndex);
                                workingLines.InsertRange(lineIndex, fixedLines);
                            }
                            else
                            {
                                // Одна строка - сохраняем оригинальный отступ если исправление не касается форматирования

                                // Удаляем ведущие пробелы из исправленной строки
                                string trimmedFixedCode = fixedCode.TrimStart();

                                // Проверяем, касается ли исправление форматирования пробелов
                                bool isFormattingError = error.Message.Contains("indentation") ||
                                                       error.Message.Contains("space") ||
                                                       error.Message.Contains("Formatting");

                                if (isFormattingError)
                                {
                                    // Для ошибок форматирования выравниваем по правильному отступу
                                    // Проверяем, нужно ли корректировать отступ
                                    if (new string(' ', originalLine.Length - originalLine.TrimStart().Length).Length % 4 != 0)
                                    {
                                        // Выравниваем по 4 пробелам
                                        int correctedIndent = ((new string(' ', originalLine.Length - originalLine.TrimStart().Length).Length / 4) + 1) * 4;
                                        workingLines[lineIndex] = new string(' ', correctedIndent) + trimmedFixedCode;
                                    }
                                    else
                                    {
                                        // Отступ уже кратен 4, сохраняем его
                                        workingLines[lineIndex] = new string(' ', originalLine.Length - originalLine.TrimStart().Length) + trimmedFixedCode;
                                    }
                                }
                                else
                                {
                                    // Для не-форматирующих ошибок сохраняем оригинальный отступ
                                    workingLines[lineIndex] = new string(' ', originalLine.Length - originalLine.TrimStart().Length) + trimmedFixedCode;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // В отладочных целях можно логировать ошибку
                    Console.WriteLine($"Error fixing line {error.LineNumber}: {ex.Message}");
                    // Продолжаем с остальными исправлениями
                }
            }

            return string.Join("\n", workingLines);
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