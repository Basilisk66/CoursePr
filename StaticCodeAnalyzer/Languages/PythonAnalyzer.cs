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
            results.AddRange(CheckIndentation(lines, 4));
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckKeywordsAsNames(lines));
            results.AddRange(CheckControlStructures(lines));
            results.AddRange(CheckCodeDuplication(lines)); // Измененный метод
            results.AddRange(CheckEmptyIfStatements(lines));
            results.AddRange(CheckImports(lines)); // Новый метод
            results.AddRange(CheckNamingConvention(lines)); // Новый метод

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

                // Проверяем операторы без пробелов (PEP 8)
                var operators = new[] { "=", "==", "!=", "<", ">", "<=", ">=", "+", "-", "*", "/", "%", "and", "or", "is", "in" };

                foreach (var op in operators)
                {
                    if (op.Length > 1)
                    {
                        // Для словесных операторов
                        var pattern = $@"(\w)({op})(\w)";
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
                        }
                    }
                    else
                    {
                        // Для символьных операторов
                        var pattern = $@"(\S)({Regex.Escape(op)})(\S)";
                        if (Regex.IsMatch(trimmed, pattern) && !IsException(trimmed, op))
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
                        }
                    }
                }
            }
            return results;
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

                        // После удаления переходим к следующей строке
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

                // 3. Проверка имен переменных (snake_case)
                var varMatch = Regex.Match(trimmedLine, @"^\s*([a-zA-Z_]\w*)\s*=");
                if (varMatch.Success)
                {
                    string varName = varMatch.Groups[1].Value;
                    // Пропускаем стандартные имена и специальные случаи
                    if (!IsSpecialVariable(varName) && !IsSnakeCase(varName) && !_keywords.Contains(varName.ToLower()))
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

                // 4. Проверка констант (UPPER_SNAKE_CASE)
                var constMatch = Regex.Match(trimmedLine, @"^\s*([A-Z][A-Z_0-9]*)\s*=");
                if (constMatch.Success)
                {
                    string constName = constMatch.Groups[1].Value;
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
                            // Заменяем строку
                            lines[lineIndex] = error.FixedCode;
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