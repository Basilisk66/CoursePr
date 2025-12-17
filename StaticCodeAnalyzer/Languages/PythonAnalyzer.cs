using StaticCodeAnalyzer.Models.StyleGuides;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class PythonAnalyzer : CodeAnalyzer
    {
        protected override void InitializeKeywords()
        {
            _keywords = new HashSet<string>
            {
                "and", "as", "assert", "async", "await", "break", "class", "continue", "def",
                "del", "elif", "else", "except", "false", "finally", "for", "from", "global",
                "if", "import", "in", "is", "lambda", "none", "nonlocal", "not", "or", "pass",
                "raise", "return", "true", "try", "while", "with", "yield"
            };
        }

        protected override void InitializeStyleGuides()
        {
            base.InitializeStyleGuides();
            _styleGuides.Add(new PEP8StyleGuide());
        }

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Применяем правила
            results.AddRange(CheckOperatorSpacing(lines));
            results.AddRange(CheckCommaSpacing(lines)); // Улучшенная проверка
            results.AddRange(CheckNoneComparisons(lines));
            results.AddRange(CheckIndentation(lines, 4));
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckKeywordsAsNames(lines));
            results.AddRange(CheckControlStructures(lines));
            results.AddRange(CheckCodeDuplication(lines));
            results.AddRange(CheckEmptyIfStatements(lines));
            results.AddRange(CheckImports(lines));
            results.AddRange(CheckNamingConvention(lines));

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

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                // Проверка имен классов
                var classMatch = Regex.Match(trimmedLine, @"^class\s+(\w+)\s*(?:\(|:)");
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[1].Value;
                    if (_keywords.Contains(className.ToLower()))
                    {
                        string fixedName = className + "_";
                        string fixedLine = trimmedLine.Replace(className, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Class name '{className}' conflicts with Python keyword",
                            "Rename class by appending '_' suffix",
                            trimmedLine,
                            fixedLine
                        ));
                    }
                }

                // Проверка имен функций
                var funcMatch = Regex.Match(trimmedLine, @"^def\s+(\w+)\s*\(");
                if (funcMatch.Success)
                {
                    string funcName = funcMatch.Groups[1].Value;
                    if (_keywords.Contains(funcName.ToLower()))
                    {
                        string fixedName = funcName + "_";
                        string fixedLine = trimmedLine.Replace(funcName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Function name '{funcName}' conflicts with Python keyword",
                            "Rename function by appending '_' suffix",
                            trimmedLine,
                            fixedLine
                        ));
                    }
                }

                // Проверка имен переменных (простое присваивание)
                var varMatch = Regex.Match(trimmedLine, @"^\s*(\w+)\s*=");
                if (varMatch.Success)
                {
                    string varName = varMatch.Groups[1].Value;
                    // Пропускаем, если это константа (UPPER_SNAKE_CASE) или специальная переменная
                    if (_keywords.Contains(varName.ToLower()) && !IsUpperSnakeCase(varName) && !IsSpecialVariable(varName))
                    {
                        string fixedName = varName + "_";
                        string fixedLine = line.Replace(varName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Variable name '{varName}' conflicts with Python keyword",
                            "Rename variable by appending '_' suffix",
                            line.Trim(),
                            fixedLine.Trim()
                        ));
                    }
                }

                // Проверка параметров функций (в определении функции)
                if (trimmedLine.StartsWith("def "))
                {
                    var paramMatch = Regex.Match(trimmedLine, @"def\s+\w+\s*\((.*?)\)");
                    if (paramMatch.Success && !string.IsNullOrWhiteSpace(paramMatch.Groups[1].Value))
                    {
                        string paramsStr = paramMatch.Groups[1].Value;
                        // Разбираем параметры (могут быть с типами аннотациями и значениями по умолчанию)
                        // Ищем имена параметров - они идут перед : (type hint) или = (default value) или , (next param)
                        var paramNameMatches = Regex.Matches(paramsStr, @"\b(\w+)(?=\s*(?:[,:=\*]|$))");
                        foreach (Match paramMatchInner in paramNameMatches)
                        {
                            string paramName = paramMatchInner.Groups[1].Value;
                            // Пропускаем специальные параметры (*args, **kwargs)
                            if (paramName == "self" || paramName == "cls" || paramName.StartsWith("*"))
                                continue;

                            if (_keywords.Contains(paramName.ToLower()))
                            {
                                string fixedName = paramName + "_";
                                // Более точная замена - используем границы слов
                                string fixedLine = Regex.Replace(line, $@"\b{Regex.Escape(paramName)}\b", fixedName);
                                results.Add(new AnalysisResult(
                                    i + 1,
                                    "Naming",
                                    $"Parameter name '{paramName}' conflicts with Python keyword",
                                    "Rename parameter by appending '_' suffix",
                                    line.Trim(),
                                    fixedLine.Trim()
                                ));
                            }
                        }
                    }
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

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Пропускаем import и from statements - они обрабатываются отдельно
                if (trimmed.StartsWith("import ") || trimmed.StartsWith("from "))
                    continue;

                // Многосимвольные операторы - проверяем только что они имеют пробелы вокруг, но не разбиваем сами операторы
                var multiCharOperators = new[] { "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "**", "//", "->", "++", "--" };

                foreach (var op in multiCharOperators)
                {
                    var escapedOp = Regex.Escape(op);
                    // Паттерн: оператор слит с символом слева или справа (но не внутри строки)
                    var pattern = $@"([^\s])({escapedOp})([^\s])";
                    var matches = Regex.Matches(trimmed, pattern);
                    foreach (Match match in matches)
                    {
                        if (!IsInsideString(trimmed, match.Index))
                        {
                            // Исключаем инкремент/декремент - они должны быть без пробелов
                            if (op == "++" || op == "--")
                                continue;

                            string fixedLine = trimmed.Substring(0, match.Index) +
                                match.Groups[1].Value + " " + match.Groups[2].Value + " " + match.Groups[3].Value +
                                trimmed.Substring(match.Index + match.Length);
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Formatting",
                                $"Missing spaces around operator '{op}'",
                                "Add spaces around operator",
                                trimmed,
                                fixedLine
                            ));
                        }
                    }
                }

                // Собираем позиции, которые уже являются частью многосимвольных операторов
                HashSet<int> multiCharOpPositions = new HashSet<int>();
                foreach (var op in multiCharOperators)
                {
                    int index = 0;
                    while ((index = trimmed.IndexOf(op, index)) != -1)
                    {
                        if (!IsInsideString(trimmed, index))
                        {
                            for (int j = 0; j < op.Length; j++)
                            {
                                multiCharOpPositions.Add(index + j);
                            }
                        }
                        index += op.Length;
                    }
                }

                // Одиночные символьные операторы (но не те, что входят в многосимвольные)
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

                        // Дополнительная проверка: проверяем контекст вокруг оператора
                        char charBefore = opIndex > 0 ? trimmed[opIndex - 1] : ' ';
                        char charAfter = opIndex + 1 < trimmed.Length ? trimmed[opIndex + 1] : ' ';

                        // Пропускаем если это часть многосимвольного оператора
                        bool isPartOfMultiChar = false;
                        if (op == "=")
                        {
                            if (charAfter == '=' || charBefore == '=' || charBefore == '!' || charBefore == '<' ||
                                charBefore == '>' || charBefore == '+' || charBefore == '-' || charBefore == '*' ||
                                charBefore == '/' || charBefore == '%')
                                isPartOfMultiChar = true;
                        }
                        else if (op == "<" && charAfter == '=')
                            isPartOfMultiChar = true;
                        else if (op == ">" && charAfter == '=')
                            isPartOfMultiChar = true;
                        else if (op == "+" && (charAfter == '+' || charAfter == '='))
                            isPartOfMultiChar = true;
                        else if (op == "-" && (charAfter == '-' || charAfter == '=' || charAfter == '>'))
                            isPartOfMultiChar = true;
                        else if (op == "*" && (charAfter == '*' || charAfter == '='))
                            isPartOfMultiChar = true;
                        else if (op == "/" && (charAfter == '/' || charAfter == '='))
                            isPartOfMultiChar = true;

                        if (isPartOfMultiChar)
                            continue;

                        if (!IsInsideString(trimmed, match.Index) && !IsException(trimmed, op))
                        {
                            string fixedLine = trimmed.Substring(0, match.Index) +
                                match.Groups[1].Value + " " + match.Groups[2].Value + " " + match.Groups[3].Value +
                                trimmed.Substring(match.Index + match.Length);
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Formatting",
                                $"Missing spaces around operator '{op}'",
                                "Add spaces around operator",
                                trimmed,
                                fixedLine
                            ));
                        }
                    }
                }

                // Словесные операторы: and, or, is - используем границы слов
                // Исправленный паттерн: ищем только отдельно стоящие операторы
                var wordOperators = new[] { "and", "or", "is", "not", "in" };

                foreach (var op in wordOperators)
                {
                    // Ищем случаи где оператор слит с другими словами (без пробелов)
                    // Но только когда оператор стоит отдельно (не внутри другого слова)
                    var pattern = $@"(?<!\w){Regex.Escape(op)}(?!\w)";
                    var matches = Regex.Matches(trimmed, pattern, RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        int opIndex = match.Index;

                        // Проверяем контекст слева и справа
                        bool hasSpaceLeft = opIndex > 0 && char.IsWhiteSpace(trimmed[opIndex - 1]);
                        bool hasSpaceRight = opIndex + op.Length < trimmed.Length && char.IsWhiteSpace(trimmed[opIndex + op.Length]);

                        // Если оператор стоит в начале строки или после открывающей скобки, это тоже ок
                        bool atStartOfLine = opIndex == 0;
                        bool afterOpenParen = opIndex > 0 && trimmed[opIndex - 1] == '(';

                        // Если оператор стоит в конце строки или перед закрывающей скобкой, это тоже ок
                        bool atEndOfLine = opIndex + op.Length == trimmed.Length;
                        bool beforeCloseParen = opIndex + op.Length < trimmed.Length && trimmed[opIndex + op.Length] == ')';

                        // Проверяем, что оператор не является частью другого слова
                        bool isPartOfWord = false;
                        if (opIndex > 0 && char.IsLetterOrDigit(trimmed[opIndex - 1]))
                            isPartOfWord = true;
                        if (opIndex + op.Length < trimmed.Length && char.IsLetterOrDigit(trimmed[opIndex + op.Length]))
                            isPartOfWord = true;

                        // Проверяем, не является ли это частью составного оператора (is not, not in)
                        bool isCompoundOperator = false;
                        if (op == "is" && opIndex + 2 < trimmed.Length && trimmed.Substring(opIndex + 2, 3) == "not")
                            isCompoundOperator = true;
                        if (op == "not" && opIndex + 3 < trimmed.Length && trimmed.Substring(opIndex + 4, 2) == "in")
                            isCompoundOperator = true;

                        if (!isPartOfWord && !IsInsideString(trimmed, opIndex) && !isCompoundOperator)
                        {
                            // Проверяем, нужны ли пробелы
                            bool needsSpaceLeft = !hasSpaceLeft && !atStartOfLine && !afterOpenParen;
                            bool needsSpaceRight = !hasSpaceRight && !atEndOfLine && !beforeCloseParen;

                            if (needsSpaceLeft || needsSpaceRight)
                            {
                                string fixedLine = trimmed;

                                if (needsSpaceRight && opIndex + op.Length < fixedLine.Length)
                                {
                                    fixedLine = fixedLine.Insert(opIndex + op.Length, " ");
                                }
                                if (needsSpaceLeft && opIndex > 0)
                                {
                                    fixedLine = fixedLine.Insert(opIndex, " ");
                                }

                                results.Add(new AnalysisResult(
                                    i + 1,
                                    "Formatting",
                                    $"Missing spaces around operator '{op}'",
                                    "Add spaces around operator",
                                    trimmed,
                                    fixedLine
                                ));
                            }
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

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Сохраняем отступ оригинальной строки
                string indent = new string(' ', line.Length - line.TrimStart().Length);

                // Ищем запятые без пробела после них (но не внутри строк)
                for (int j = 0; j < trimmed.Length; j++)
                {
                    if (trimmed[j] == ',')
                    {
                        // Пропускаем запятые внутри строковых литералов
                        if (IsInsideString(trimmed, j))
                            continue;

                        // Пропускаем запятые в числах (например, 1,000)
                        if (j > 0 && j < trimmed.Length - 1)
                        {
                            char charBefore = trimmed[j - 1];
                            char charAfter = trimmed[j + 1];
                            if (char.IsDigit(charBefore) && char.IsDigit(charAfter))
                                continue;
                        }

                        // Пропускаем запятые в конце строки
                        bool isAtEnd = j == trimmed.Length - 1;

                        // Проверяем, есть ли пробел после запятой
                        if (!isAtEnd)
                        {
                            int nextCharIndex = j + 1;

                            // Пропускаем пробелы и табы, ищем следующий непробельный символ
                            while (nextCharIndex < trimmed.Length && char.IsWhiteSpace(trimmed[nextCharIndex]))
                            {
                                nextCharIndex++;
                            }

                            // Если следующий непробельный символ существует
                            if (nextCharIndex < trimmed.Length)
                            {
                                char nextChar = trimmed[nextCharIndex];

                                // Проверяем различные случаи
                                bool needsSpace = true;

                                // Исключения:
                                // 1. Следующий символ - закрывающая скобка или квадратная скобка
                                if (nextChar == ')' || nextChar == ']' || nextChar == '}')
                                {
                                    needsSpace = false;
                                }
                                // 2. Если это конец комментария
                                else if (nextCharIndex + 1 < trimmed.Length &&
                                         trimmed[nextCharIndex] == '#' && nextCharIndex == j + 1)
                                {
                                    needsSpace = false;
                                }
                                // 3. Если следующая запятая или двоеточие
                                else if (nextChar == ',' || nextChar == ':')
                                {
                                    needsSpace = false;
                                }

                                if (needsSpace)
                                {
                                    // Проверяем, нет ли уже пробела сразу после запятой
                                    if (j + 1 < trimmed.Length && !char.IsWhiteSpace(trimmed[j + 1]))
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
                }
            }
            return results;
        }

        private List<AnalysisResult> CheckNoneComparisons(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Проверяем == None и != None
                // Используем границы слов для точного поиска

                // Паттерн для == None: может быть частью if/while/elif и т.д.
                var patternEquals = @"(\b\w+\b)\s*==\s*None\b";
                var matchesEquals = Regex.Matches(trimmed, patternEquals, RegexOptions.IgnoreCase);
                foreach (Match match in matchesEquals)
                {
                    if (!IsInsideString(trimmed, match.Index))
                    {
                        string varName = match.Groups[1].Value;
                        string fixedLine = trimmed.Substring(0, match.Index) +
                            $"{varName} is None" +
                            trimmed.Substring(match.Index + match.Length);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Best Practice",
                            "Use 'is None' instead of '== None' for None comparison",
                            "Replace '== None' with 'is None'",
                            trimmed,
                            fixedLine
                        ));
                    }
                }

                // Паттерн для != None
                var patternNotEquals = @"(\b\w+\b)\s*!=\s*None\b";
                var matchesNotEquals = Regex.Matches(trimmed, patternNotEquals, RegexOptions.IgnoreCase);
                foreach (Match match in matchesNotEquals)
                {
                    if (!IsInsideString(trimmed, match.Index))
                    {
                        string varName = match.Groups[1].Value;
                        string fixedLine = trimmed.Substring(0, match.Index) +
                            $"{varName} is not None" +
                            trimmed.Substring(match.Index + match.Length);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Best Practice",
                            "Use 'is not None' instead of '!= None' for None comparison",
                            "Replace '!= None' with 'is not None'",
                            trimmed,
                            fixedLine
                        ));
                    }
                }
            }
            return results;
        }

        private bool IsInsideString(string line, int index)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escaped = false;
            bool inTripleSingle = false;
            bool inTripleDouble = false;

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

                // Проверка тройных кавычек
                if (i + 2 < line.Length)
                {
                    string triple = line.Substring(i, 3);
                    if (triple == "'''" && !inDoubleQuote && !inTripleDouble)
                    {
                        inTripleSingle = !inTripleSingle;
                        i += 2;
                        continue;
                    }
                    if (triple == "\"\"\"" && !inSingleQuote && !inTripleSingle)
                    {
                        inTripleDouble = !inTripleDouble;
                        i += 2;
                        continue;
                    }
                }

                if (!inTripleSingle && !inTripleDouble)
                {
                    if (c == '\'' && !inDoubleQuote)
                    {
                        inSingleQuote = !inSingleQuote;
                    }
                    else if (c == '"' && !inSingleQuote)
                    {
                        inDoubleQuote = !inDoubleQuote;
                    }
                }
            }

            return inSingleQuote || inDoubleQuote || inTripleSingle || inTripleDouble;
        }

        private bool IsException(string line, string op)
        {
            // Исключения для определенных случаев
            if (op == "*" && line.Contains("**")) return true; // ** оператор
            if (op == "-" && line.Contains("->")) return true; // type hint
            if (line.Contains("f\"") && line.Contains("{") && line.Contains("}")) return true; // f-strings
            if (line.Contains("'") || line.Contains("\"")) return true; // Строки с операторами

            return false;
        }

        private List<AnalysisResult> CheckControlStructures(string[] lines)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Проверка for, while, if, def, class
                if (trimmed.StartsWith("for ") || trimmed.StartsWith("while ") ||
                    trimmed.StartsWith("if ") || trimmed.StartsWith("def ") ||
                    trimmed.StartsWith("class ") || trimmed.StartsWith("with ") ||
                    trimmed.StartsWith("try:") || trimmed.StartsWith("except "))
                {
                    // Проверяем наличие двоеточия
                    if (!trimmed.EndsWith(":") && !line.Contains(":") && !IsInMiddleOfLine(line, ":"))
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Syntax",
                            "Missing colon at end of statement",
                            "Add colon",
                            trimmed,
                            trimmed + ":"
                        ));
                    }

                    // Проверяем отступ для следующей строки
                    if (i + 1 < lines.Length)
                    {
                        string nextLine = lines[i + 1];
                        if (!string.IsNullOrWhiteSpace(nextLine))
                        {
                            int currentIndent = line.Length - line.TrimStart().Length;
                            int nextIndent = nextLine.Length - nextLine.TrimStart().Length;

                            if (nextIndent <= currentIndent)
                            {
                                results.Add(new AnalysisResult(
                                    i + 2,
                                    "Indentation",
                                    "Expected an indented block",
                                    "Add indentation (4 spaces)",
                                    nextLine,
                                    new string(' ', currentIndent + 4) + nextLine.TrimStart()
                                ));
                            }
                        }
                    }
                }

                // Проверка правильности for цикла
                if (trimmed.StartsWith("for ") && trimmed.Contains(" in "))
                {
                    // Уже корректно
                }
                else if (trimmed.StartsWith("for "))
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Syntax",
                        "Invalid for loop syntax",
                        "Use 'for item in iterable:' syntax",
                        trimmed,
                        "for item in iterable:"
                    ));
                }

                // Проверка правильности while цикла
                if (trimmed.StartsWith("while ") && !trimmed.Contains(":"))
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Syntax",
                        "Invalid while loop syntax",
                        "Add condition and colon",
                        trimmed,
                        trimmed + " condition:"
                    ));
                }
            }
            return results;
        }

        private bool IsInMiddleOfLine(string line, string search)
        {
            int index = line.IndexOf(search);
            return index > 0 && index < line.Length - 1;
        }

        private List<AnalysisResult> CheckCodeDuplication(string[] lines)
        {
            var results = new List<AnalysisResult>();

            // Ищем 2+ одинаковых строк подряд
            for (int i = 0; i < lines.Length - 1; i++)
            {
                string currentLine = lines[i].Trim();

                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(currentLine) || currentLine.StartsWith("#"))
                    continue;

                // Проверяем следующую строку
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string nextLine = lines[j].Trim();

                    if (string.IsNullOrWhiteSpace(nextLine) || nextLine.StartsWith("#"))
                        continue;

                    if (currentLine == nextLine)
                    {
                        // Нашли дубликат
                        results.Add(new AnalysisResult(
                            j + 1,
                            "Code Quality",
                            "Duplicate line of code detected",
                            "Remove duplicate line",
                            nextLine,
                            "" // Удаляем строку
                        ));

                        // После удаления переходим к следующей строки
                        break;
                    }
                    else
                    {
                        // Строки разные, прекращаем поиск дубликатов для этой строки
                        break;
                    }
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckEmptyIfStatements(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("if ") && line.EndsWith(":"))
                {
                    // Проверяем следующие строки до следующего отступа того же уровня
                    bool hasContent = false;
                    int ifIndent = lines[i].Length - lines[i].TrimStart().Length;

                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[j])) continue;

                        int currentIndent = lines[j].Length - lines[j].TrimStart().Length;
                        string currentLine = lines[j].Trim();

                        // Если отступ меньше или равен, значит мы вышли из блока if
                        if (currentIndent <= ifIndent)
                        {
                            break;
                        }

                        // Если строка не комментарий и не пустая
                        if (!currentLine.StartsWith("#") && !string.IsNullOrWhiteSpace(currentLine))
                        {
                            hasContent = true;
                            break;
                        }
                    }

                    if (!hasContent)
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Code Quality",
                            "Empty if statement",
                            "Add code to if block or remove it",
                            line,
                            line + "  # Add code here"
                        ));
                    }
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckImports(string[] lines)
        {
            var results = new List<AnalysisResult>();
            bool foundNonImport = false;
            List<int> importLines = new List<int>();
            List<string> imports = new List<string>();

            // Сначала собираем все импорты
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("import ") || line.StartsWith("from "))
                {
                    importLines.Add(i);
                    imports.Add(line);

                    // Проверяем, не было ли уже не-импортов перед этим
                    if (foundNonImport)
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "PEP8",
                            "Import should be at the top of the file",
                            "Move import to the top",
                            line,
                            line // Будут перенесены позже
                        ));
                    }

                    // Проверка нескольких импортов на одной строке
                    if (line.StartsWith("import ") && line.Contains(","))
                    {
                        string fixedImports = FixMultipleImports(line);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "PEP8",
                            "Multiple imports on one line",
                            "Put each import on separate line",
                            line,
                            fixedImports
                        ));
                    }
                }
                else if (!line.StartsWith("#!/") && !line.StartsWith("# -*-")) // Игнорируем shebang и кодировку
                {
                    foundNonImport = true;
                }
            }

            return results;
        }

        private string FixMultipleImports(string importLine)
        {
            // Пример: "import os, sys, math" -> "import os\nimport sys\nimport math"
            if (!importLine.StartsWith("import ")) return importLine;

            string importsPart = importLine.Substring(7); // Убираем "import "
            var importItems = importsPart.Split(',')
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToList();

            if (importItems.Count <= 1) return importLine;

            return string.Join("\n", importItems.Select(item => $"import {item}"));
        }

        private List<AnalysisResult> CheckNamingConvention(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                // 1. Проверка имен классов (CamelCase)
                var classMatch = Regex.Match(trimmedLine, @"^class\s+([a-zA-Z_]\w*)\s*(?:\(|:)");
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[1].Value;
                    if (!IsCamelCase(className))
                    {
                        string fixedName = ToCamelCase(className);
                        string fixedLine = trimmedLine.Replace(className, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Class name '{className}' should use CamelCase",
                            "Rename class to use CamelCase",
                            trimmedLine,
                            fixedLine
                        ));
                    }
                }

                // 2. Проверка имен функций (snake_case)
                var funcMatch = Regex.Match(trimmedLine, @"^def\s+([a-zA-Z_]\w*)\s*\(");
                if (funcMatch.Success)
                {
                    string funcName = funcMatch.Groups[1].Value;
                    if (!IsSnakeCase(funcName))
                    {
                        string fixedName = ToSnakeCase(funcName);
                        string fixedLine = trimmedLine.Replace(funcName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Function name '{funcName}' should use snake_case",
                            "Rename function to use snake_case",
                            trimmedLine,
                            fixedLine
                        ));
                    }
                }

                // 3. Проверка констант (UPPER_SNAKE_CASE) - проверяем ПЕРЕД переменными, т.к. константы - более специфичный случай
                var constMatch = Regex.Match(trimmedLine, @"^\s*([A-Z][A-Z_0-9]*)\s*=");
                if (constMatch.Success)
                {
                    string constName = constMatch.Groups[1].Value;
                    // Проверяем только если имя НЕ является уже корректным UPPER_SNAKE_CASE
                    if (!IsUpperSnakeCase(constName))
                    {
                        string fixedName = ToUpperSnakeCase(constName);
                        string fixedLine = line.Replace(constName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Constant name '{constName}' should use UPPER_SNAKE_CASE",
                            "Rename constant to use UPPER_SNAKE_CASE",
                            line.Trim(),
                            fixedLine.Trim()
                        ));
                    }
                    // Если это уже корректная константа, пропускаем проверку переменных для этой строки
                    continue;
                }

                // 4. Проверка имен переменных (snake_case)
                var varMatch = Regex.Match(trimmedLine, @"^\s*([a-zA-Z_]\w*)\s*=");
                if (varMatch.Success)
                {
                    string varName = varMatch.Groups[1].Value;
                    // Пропускаем стандартные имена, специальные случаи, ключевые слова и уже корректные UPPER_SNAKE_CASE
                    if (!IsSpecialVariable(varName) && !IsSnakeCase(varName) && !_keywords.Contains(varName.ToLower()) && !IsUpperSnakeCase(varName))
                    {
                        string fixedName = ToSnakeCase(varName);
                        string fixedLine = line.Replace(varName, fixedName);
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Naming",
                            $"Variable name '{varName}' should use snake_case",
                            "Rename variable to use snake_case",
                            line.Trim(),
                            fixedLine.Trim()
                        ));
                    }
                }
            }

            return results;
        }

        // Вспомогательные методы для проверки именования
        private bool IsCamelCase(string name)
        {
            return Regex.IsMatch(name, @"^[A-Z][a-zA-Z0-9]*$");
        }

        private bool IsSnakeCase(string name)
        {
            return Regex.IsMatch(name, @"^[a-z_][a-z0-9_]*$") && !name.Contains("__");
        }

        private bool IsUpperSnakeCase(string name)
        {
            return Regex.IsMatch(name, @"^[A-Z_][A-Z0-9_]*$") && name.Contains("_");
        }

        private bool IsSpecialVariable(string name)
        {
            // Специальные имена в Python
            var specialNames = new HashSet<string>
            {
                "self", "cls", "_", "__name__", "__main__", "__init__",
                "__str__", "__repr__", "__len__", "__getitem__"
            };

            return specialNames.Contains(name) ||
                   name.StartsWith("__") && name.EndsWith("__") ||
                   name.StartsWith("_");
        }

        private string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Убираем подчеркивания и делаем каждое слово с заглавной буквы
            var parts = name.Split('_')
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower())
                .ToArray();

            return string.Join("", parts);
        }

        private string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Если имя уже в UPPER_SNAKE_CASE, просто конвертируем в нижний регистр
            if (IsUpperSnakeCase(name))
            {
                return name.ToLower();
            }

            // Если имя уже содержит подчеркивания и в нижнем регистре, возможно уже snake_case
            if (name.Contains('_') && name == name.ToLower())
            {
                return name; // Уже в snake_case
            }

            // Разделяем CamelCase или PascalCase на слова
            var words = new List<string>();
            var currentWord = new StringBuilder();

            foreach (char c in name)
            {
                if (char.IsUpper(c) && currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString().ToLower());
                    currentWord.Clear();
                }
                currentWord.Append(c);
            }

            if (currentWord.Length > 0)
            {
                words.Add(currentWord.ToString().ToLower());
            }

            return string.Join("_", words);
        }

        private string ToUpperSnakeCase(string name)
        {
            // Если имя уже в UPPER_SNAKE_CASE, возвращаем как есть
            if (IsUpperSnakeCase(name))
            {
                return name;
            }

            // Если имя уже в snake_case (lowercase with underscores), просто конвертируем в верхний регистр
            if (name.Contains('_') && name == name.ToLower())
            {
                return name.ToUpper();
            }

            // Иначе конвертируем через ToSnakeCase и затем в верхний регистр
            string snakeCase = ToSnakeCase(name);
            return snakeCase.ToUpper();
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            if (!string.IsNullOrEmpty(fileExtension) && fileExtension.ToLower() == ".py")
                return true;

            return !string.IsNullOrEmpty(code) &&
                   (code.Contains("def ") || code.Contains("import ") ||
                    code.Contains("print(") || code.Contains("if __name__") ||
                    code.Trim().StartsWith("#!") || code.Contains(":") &&
                    (code.Contains("for ") || code.Contains("while ") || code.Contains("class ")));
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

            // Сначала обрабатываем импорты, которые нужно перенести
            var importErrors = errorsToFix
                .Where(e => e.Message.Contains("Import should be at the top"))
                .ToList();

            if (importErrors.Any())
            {
                // Собираем все импорты
                var imports = new List<string>();
                var otherLines = new List<string>();
                bool inImportSection = true;

                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i].Trim();

                    if (inImportSection && (line.StartsWith("import ") || line.StartsWith("from ")))
                    {
                        imports.Add(lines[i]); // Сохраняем с отступами
                    }
                    else
                    {
                        inImportSection = false;
                        otherLines.Add(lines[i]);
                    }
                }

                // Обновляем список строк
                lines = imports.Concat(otherLines).ToList();

                // Удаляем обработанные ошибки импортов
                errorsToFix = errorsToFix.Except(importErrors).ToList();
            }

            // Обрабатываем остальные ошибки
            foreach (var error in errorsToFix)
            {
                try
                {
                    int lineIndex = error.LineNumber - 1;

                    if (lineIndex >= 0 && lineIndex < lines.Count)
                    {
                        if (error.FixedCode == "")
                        {
                            // Удаляем строку
                            lines.RemoveAt(lineIndex);
                        }
                        else if (!string.IsNullOrWhiteSpace(error.FixedCode))
                        {
                            // Сохраняем отступ оригинальной строки
                            string originalLine = lines[lineIndex];
                            string originalIndent = new string(' ', originalLine.Length - originalLine.TrimStart().Length);
                            string fixedCode = error.FixedCode;

                            // Если FixedCode содержит несколько строк (например, разделенные импорты)
                            if (fixedCode.Contains("\n"))
                            {
                                var fixedLines = fixedCode.Split('\n').ToList();

                                // Добавляем отступ к каждой строке
                                for (int j = 0; j < fixedLines.Count; j++)
                                {
                                    if (!string.IsNullOrWhiteSpace(fixedLines[j]))
                                    {
                                        // Если строка уже имеет отступ, используем его, иначе добавляем оригинальный
                                        string lineIndent = new string(' ', fixedLines[j].Length - fixedLines[j].TrimStart().Length);
                                        if (lineIndent.Length == 0 && originalIndent.Length > 0)
                                        {
                                            fixedLines[j] = originalIndent + fixedLines[j].TrimStart();
                                        }
                                    }
                                }

                                // Заменяем одну строку несколькими
                                lines.RemoveAt(lineIndex);
                                lines.InsertRange(lineIndex, fixedLines);
                            }
                            else
                            {
                                // Одна строка - добавляем отступ если нужно
                                if (!fixedCode.StartsWith(" ") && !fixedCode.StartsWith("\t") && originalIndent.Length > 0)
                                {
                                    fixedCode = originalIndent + fixedCode.TrimStart();
                                }

                                // Заменяем строку
                                lines[lineIndex] = fixedCode;
                            }
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки для отдельных строк
                }
            }

            return string.Join("\n", lines);
        }
    }
}