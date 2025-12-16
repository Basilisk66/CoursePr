using StaticCodeAnalyzer.Models.StyleGuides;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public abstract class CodeAnalyzer
    {
        protected List<IStyleGuide> _styleGuides = new List<IStyleGuide>();
        protected HashSet<string> _keywords = new HashSet<string>();

        public CodeAnalyzer()
        {
            InitializeStyleGuides();
            InitializeKeywords();
        }

        protected virtual void InitializeStyleGuides()
        {
            _styleGuides.Add(new GoogleStyleGuide());
        }

        protected virtual void InitializeKeywords()
        {
            // Базовые ключевые слова - будут переопределены в наследниках
        }

        public abstract List<AnalysisResult> Analyze(string code);

        public virtual string FixCode(string code, List<AnalysisResult> errors)
        {
            if (errors == null || !errors.Any())
                return code;

            var lines = code.Split('\n');
            var errorsToFix = errors
                .Where(e => e.LineNumber > 0 && e.LineNumber <= lines.Length)
                .OrderByDescending(e => e.LineNumber)
                .ToList();

            foreach (var error in errorsToFix)
            {
                try
                {
                    int lineIndex = error.LineNumber - 1;

                    if (error.FixedCode == "")
                    {
                        // Удаляем пустую строку
                        var newLines = new List<string>();
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i != lineIndex)
                                newLines.Add(lines[i]);
                        }
                        lines = newLines.ToArray();
                    }
                    else if (error.FixedCode != null)
                    {
                        // Заменяем строку
                        lines[lineIndex] = error.FixedCode;
                    }
                }
                catch
                {
                    // Игнорируем ошибки
                }
            }

            return string.Join("\n", lines);
        }

        public abstract bool CanHandle(string code, string fileExtension = "");

        // Общие методы для всех анализаторов
        protected List<AnalysisResult> CheckEmptyLines(string[] lines)
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
                        ""
                    ));
                }
            }
            return results;
        }

        protected List<AnalysisResult> CheckIndentation(string[] lines, int spaces = 4)
        {
            var results = new List<AnalysisResult>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                int leadingSpaces = lines[i].TakeWhile(c => c == ' ').Count();
                if (leadingSpaces % spaces != 0)
                {
                    int correctIndent = (leadingSpaces / spaces) * spaces;
                    string fixedLine = new string(' ', correctIndent) + lines[i].TrimStart();
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        $"Indent must be multiple of {spaces} spaces (got {leadingSpaces})",
                        "Fix indentation",
                        lines[i],
                        fixedLine
                    ));
                }
            }
            return results;
        }

        protected List<AnalysisResult> CheckKeywordsAsNames(string[] lines)
        {
            var results = new List<AnalysisResult>();
            if (!_keywords.Any()) return results;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();

                // Проверка имен переменных, функций, классов
                var patterns = new[]
                {
                    @"\b(int|string|bool|double|float|char|var|let|const)\s+(\w+)\s*[=;]",
                    @"\b(class|struct|enum|interface)\s+(\w+)",
                    @"\b(def|function)\s+(\w+)\s*\(",
                    @"^\s*(\w+)\s*=\s*"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(trimmedLine, pattern);
                    if (match.Success)
                    {
                        string name = match.Groups.Count > 2 ? match.Groups[2].Value : match.Groups[1].Value;
                        if (_keywords.Contains(name.ToLower()))
                        {
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Naming",
                                $"Name '{name}' conflicts with language keyword",
                                $"Rename '{name}' to avoid keyword conflict",
                                trimmedLine,
                                trimmedLine // Автоматическое исправление сложно
                            ));
                        }
                    }
                }
            }
            return results;
        }

        protected int FindLineNumberForPattern(string[] lines, string pattern)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(pattern))
                    return i + 1;
            }
            return 1;
        }
    }
}