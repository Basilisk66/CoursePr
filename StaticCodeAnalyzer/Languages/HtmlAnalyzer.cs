using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class HtmlAnalyzer : CodeAnalyzer
    {
        private readonly HashSet<string> _htmlTags = new HashSet<string>
        {
            "html", "head", "body", "div", "span", "p", "a", "img", "table", "tr", "td",
            "th", "ul", "ol", "li", "h1", "h2", "h3", "h4", "h5", "h6", "form", "input",
            "button", "select", "option", "textarea", "label", "script", "style", "link",
            "meta", "title", "header", "footer", "nav", "section", "article", "aside",
            "main", "figure", "figcaption", "details", "summary", "mark", "time", "audio",
            "video", "source", "canvas", "svg", "path", "circle", "rect", "line", "polygon"
        };

        private readonly HashSet<string> _selfClosingTags = new HashSet<string>
        {
            "img", "br", "hr", "meta", "link", "input", "area", "base", "col", "embed",
            "keygen", "param", "source", "track", "wbr"
        };

        private readonly Dictionary<string, string[]> _requiredAttributes = new Dictionary<string, string[]>
        {
            { "img", new[] { "src", "alt" } },
            { "a", new[] { "href" } },
            { "link", new[] { "rel", "href" } },
            { "script", new[] { "src" } },
            { "form", new[] { "action" } },
            { "input", new[] { "type" } }
        };

        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            // Применяем все правила HTML
            results.AddRange(CheckEmptyLines(lines));
            results.AddRange(CheckDocumentStructure(code, lines));
            results.AddRange(CheckTagClosing(code, lines));
            results.AddRange(CheckTagAndAttributeCase(lines));
            results.AddRange(CheckAttributeQuotes(lines));
            results.AddRange(CheckIndentationAndLineBreaks(lines));
            results.AddRange(CheckEmptyTags(lines));
            results.AddRange(CheckImageDimensions(lines));
            results.AddRange(CheckTagSpelling(lines));
            results.AddRange(CheckTableStructure(lines));
            results.AddRange(CheckRequiredAttributes(lines));

            return results;
        }

        private List<AnalysisResult> CheckDocumentStructure(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();

            // Проверяем наличие DOCTYPE
            if (!Regex.IsMatch(code, @"<!DOCTYPE\s+html\s*>", RegexOptions.IgnoreCase))
            {
                results.Add(new AnalysisResult(
                    1,
                    "Structure",
                    "Missing DOCTYPE declaration",
                    "Add <!DOCTYPE html> at the beginning",
                    "",
                    "<!DOCTYPE html>"
                ));
            }

            // Проверяем наличие html тега
            if (!Regex.IsMatch(code, @"<html[^>]*>", RegexOptions.IgnoreCase))
            {
                int lineNumber = FindLineNumberForPattern(lines, "<");
                results.Add(new AnalysisResult(
                    lineNumber,
                    "Structure",
                    "Missing <html> tag",
                    "Add <html> tag",
                    "",
                    "<html>"
                ));
            }

            // Проверяем наличие head
            if (!Regex.IsMatch(code, @"<head[^>]*>", RegexOptions.IgnoreCase))
            {
                int lineNumber = FindLineNumberForPattern(lines, "<html");
                results.Add(new AnalysisResult(
                    lineNumber,
                    "Structure",
                    "Missing <head> section",
                    "Add <head> section inside <html>",
                    "",
                    "<head>\n    <title>Document</title>\n</head>"
                ));
            }

            // Проверяем наличие title в head
            if (Regex.IsMatch(code, @"<head[^>]*>", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(code, @"<title[^>]*>.*</title>", RegexOptions.IgnoreCase))
            {
                int lineNumber = FindLineNumberForPattern(lines, "<head");
                results.Add(new AnalysisResult(
                    lineNumber,
                    "Structure",
                    "Missing <title> in <head>",
                    "Add <title>Document</title> inside <head>",
                    "",
                    "<title>Document</title>"
                ));
            }

            // Проверяем наличие body
            if (!Regex.IsMatch(code, @"<body[^>]*>", RegexOptions.IgnoreCase))
            {
                int headEndLine = FindLineNumberForPattern(lines, "</head>");
                if (headEndLine == 1) headEndLine = FindLineNumberForPattern(lines, "<html");

                results.Add(new AnalysisResult(
                    headEndLine,
                    "Structure",
                    "Missing <body> section",
                    "Add <body> section after </head>",
                    "",
                    "<body>\n    <!-- Content goes here -->\n</body>"
                ));
            }

            // Проверяем правильный порядок: DOCTYPE -> html -> head -> body
            int doctypeLine = FindLineNumberForPattern(lines, "!DOCTYPE");
            int htmlLine = FindLineNumberForPattern(lines, "<html");
            int headLine = FindLineNumberForPattern(lines, "<head");
            int bodyLine = FindLineNumberForPattern(lines, "<body");

            if (doctypeLine > 0 && htmlLine > 0 && doctypeLine > htmlLine)
            {
                results.Add(new AnalysisResult(
                    htmlLine,
                    "Structure",
                    "DOCTYPE should be before <html>",
                    "Move DOCTYPE to the first line",
                    lines[htmlLine - 1],
                    "<!DOCTYPE html>\n" + lines[htmlLine - 1]
                ));
            }

            if (headLine > 0 && bodyLine > 0 && headLine > bodyLine)
            {
                results.Add(new AnalysisResult(
                    bodyLine,
                    "Structure",
                    "<head> should be before <body>",
                    "Move <head> section before <body>",
                    lines[bodyLine - 1],
                    "<!-- <head> should come before <body> -->"
                ));
            }

            return results;
        }

        private List<AnalysisResult> CheckTagClosing(string code, string[] lines)
        {
            var results = new List<AnalysisResult>();
            var tagStack = new Stack<(string tag, int line)>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Находим все теги в строке
                var tagMatches = Regex.Matches(line, @"</?(\w+)[^>]*>");
                foreach (Match match in tagMatches)
                {
                    string fullTag = match.Value;
                    string tagName = match.Groups[1].Value.ToLower();

                    // Пропускаем самозакрывающиеся теги
                    if (_selfClosingTags.Contains(tagName) || fullTag.EndsWith("/>"))
                        continue;

                    if (fullTag.StartsWith("</"))
                    {
                        // Закрывающий тег
                        if (tagStack.Count > 0 && tagStack.Peek().tag == tagName)
                        {
                            tagStack.Pop();
                        }
                        else if (tagStack.Any(t => t.tag == tagName))
                        {
                            // Находим, где был открыт этот тег
                            var matchingTag = tagStack.FirstOrDefault(t => t.tag == tagName);
                            if (matchingTag.line > 0)
                            {
                                results.Add(new AnalysisResult(
                                    i + 1,
                                    "Syntax",
                                    $"Mismatched closing tag </{tagName}> (opened at line {matchingTag.line})",
                                    "Check tag nesting order",
                                    fullTag,
                                    fullTag + " <!-- Check nesting with line " + matchingTag.line + " -->"
                                ));
                            }
                        }
                        else
                        {
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Syntax",
                                $"Unexpected closing tag </{tagName}> (no opening tag found)",
                                "Remove closing tag or add opening tag",
                                fullTag,
                                "<!-- Remove unexpected closing tag -->"
                            ));
                        }
                    }
                    else
                    {
                        // Открывающий тег
                        tagStack.Push((tagName, i + 1));
                    }
                }
            }

            // Проверяем незакрытые теги
            while (tagStack.Count > 0)
            {
                var (tag, lineNumber) = tagStack.Pop();

                // Некоторые теги могут не закрываться (например, в HTML5)
                var tagsThatCanBeUnclosed = new HashSet<string> { "html", "head", "body" };
                if (!tagsThatCanBeUnclosed.Contains(tag))
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Syntax",
                        $"Unclosed <{tag}> tag",
                        $"Add closing </{tag}> tag",
                        lines[lineNumber - 1],
                        lines[lineNumber - 1] + $"\n<!-- Add </{tag}> here -->"
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckTagAndAttributeCase(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Проверяем теги в верхнем регистре
                var tagMatches = Regex.Matches(line, @"</?([A-Z][A-Za-z]*)[^>]*>");
                foreach (Match match in tagMatches)
                {
                    string tag = match.Groups[1].Value;
                    string fullMatch = match.Value;
                    string fixedTag = tag.ToLower();
                    string fixedFull = fullMatch.Replace(tag, fixedTag);

                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        $"HTML tags should be lowercase: '{tag}'",
                        "Convert tag to lowercase",
                        fullMatch,
                        fixedFull
                    ));
                }

                // Проверяем атрибуты в верхнем регистре
                var attrMatches = Regex.Matches(line, @"\s+([A-Z][A-Za-z-]*)\s*=");
                foreach (Match match in attrMatches)
                {
                    string attribute = match.Groups[1].Value;
                    string fixedAttribute = attribute.ToLower();

                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        $"HTML attributes should be lowercase: '{attribute}'",
                        "Convert attribute to lowercase",
                        match.Value,
                        match.Value.Replace(attribute, fixedAttribute)
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckAttributeQuotes(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Ищем атрибуты без кавычек или с одинарными кавычками
                var attributePattern = @"\s+(\w[\w-]*)\s*=\s*'([^']*)'";
                var singleQuoteMatches = Regex.Matches(line, attributePattern);

                foreach (Match match in singleQuoteMatches)
                {
                    string attribute = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    string fullMatch = match.Value;
                    string fixedMatch = $" {attribute}=\"{value}\"";

                    results.Add(new AnalysisResult(
                        i + 1,
                        "Validation",
                        $"Attribute '{attribute}' uses single quotes",
                        "Use double quotes for HTML attributes",
                        fullMatch,
                        fixedMatch
                    ));
                }

                // Ищем атрибуты без кавычек
                var noQuotePattern = @"\s+(\w[\w-]*)\s*=\s*([^'""\s>][^>]*?)(?=\s+\w+=|\s*\/?>|\s*$)";
                var noQuoteMatches = Regex.Matches(line, noQuotePattern);

                foreach (Match match in noQuoteMatches)
                {
                    string attribute = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();

                    // Исключения: boolean атрибуты
                    var booleanAttributes = new HashSet<string> { "checked", "disabled", "readonly", "required", "multiple" };
                    if (booleanAttributes.Contains(attribute.ToLower()) &&
                        (string.IsNullOrEmpty(value) || value == attribute))
                        continue;

                    string fullMatch = match.Value;
                    string fixedMatch = $" {attribute}=\"{value}\"";

                    results.Add(new AnalysisResult(
                        i + 1,
                        "Validation",
                        $"Attribute '{attribute}' missing quotes",
                        "Add double quotes around attribute value",
                        fullMatch,
                        fixedMatch
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckIndentationAndLineBreaks(string[] lines)
        {
            var results = new List<AnalysisResult>();
            Stack<int> indentStack = new Stack<int>();
            indentStack.Push(0);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Пропускаем однострочные элементы
                if (IsSingleLineElement(trimmed))
                    continue;

                int actualIndent = line.Length - line.TrimStart().Length;
                int expectedIndent = indentStack.Peek();

                // Проверяем отступ
                if (actualIndent != expectedIndent && actualIndent % 4 != 0)
                {
                    string fixedLine = new string(' ', expectedIndent) + trimmed;
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Formatting",
                        $"Incorrect indentation. Expected {expectedIndent}, got {actualIndent}",
                        "Fix indentation to multiple of 4 spaces",
                        line,
                        fixedLine
                    ));
                }

                // Обновляем стек отступов для следующей строки
                UpdateIndentStack(line, trimmed, indentStack);
            }

            return results;
        }

        private bool IsSingleLineElement(string line)
        {
            // Элементы, которые обычно пишут в одну строку
            var singleLineElements = new HashSet<string>
            {
                "<!DOCTYPE",
                "<meta",
                "<link",
                "<br",
                "<hr",
                "<img",
                "<input",
                "<!--"
            };

            return singleLineElements.Any(e => line.StartsWith(e)) ||
                   line.Length < 80 && line.Contains("<") && line.Contains(">");
        }

        private void UpdateIndentStack(string line, string trimmed, Stack<int> indentStack)
        {
            // Определяем, увеличивается или уменьшается отступ
            bool isOpeningTag = trimmed.StartsWith("<") && !trimmed.StartsWith("</") &&
                               !trimmed.EndsWith("/>") && !_selfClosingTags.Any(t => trimmed.StartsWith($"<{t}"));
            bool isClosingTag = trimmed.StartsWith("</");

            if (isOpeningTag)
            {
                indentStack.Push(indentStack.Peek() + 4);
            }
            else if (isClosingTag)
            {
                if (indentStack.Count > 1)
                    indentStack.Pop();
            }
        }

        private List<AnalysisResult> CheckEmptyTags(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // Ищем пустые теги
                var emptyTagPattern = @"<(\w+)[^>]*>\s*</\1>";
                var matches = Regex.Matches(trimmed, emptyTagPattern);

                foreach (Match match in matches)
                {
                    string tagName = match.Groups[1].Value.ToLower();

                    // Некоторые теги могут быть пустыми по дизайну
                    var allowedEmptyTags = new HashSet<string>
                    {
                        "div", "span", "section", "article", "aside", "header",
                        "footer", "nav", "main", "template"
                    };

                    if (!allowedEmptyTags.Contains(tagName))
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Code Quality",
                            $"Empty {tagName} tag without content",
                            "Remove empty tag or add content",
                            match.Value,
                            $"<!-- Consider removing empty {tagName} tag -->"
                        ));
                    }
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckImageDimensions(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("<img") && (line.Contains(" width=") || line.Contains(" height=")))
                {
                    bool hasWidth = Regex.IsMatch(line, @"\bwidth\s*=");
                    bool hasHeight = Regex.IsMatch(line, @"\bheight\s*=");

                    if (!hasWidth || !hasHeight)
                    {
                        string missing = !hasWidth ? "width" : "height";
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Performance",
                            $"Image missing {missing} attribute",
                            $"Add {missing} attribute for better page rendering",
                            line,
                            line.Replace("<img", $"<img {missing}=\"\"").TrimEnd('>') + ">"
                        ));
                    }
                }
                else if (line.Contains("<img") && !line.Contains(" width=") && !line.Contains(" height="))
                {
                    results.Add(new AnalysisResult(
                        i + 1,
                        "Performance",
                        "Image missing both width and height attributes",
                        "Add width and height attributes to prevent layout shifts",
                        line,
                        line.Replace("<img", "<img width=\"\" height=\"\"")
                    ));
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckTagSpelling(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Ищем потенциально опечатанные теги
                var tagMatches = Regex.Matches(line, @"</?(\w+)[^>]*>");
                foreach (Match match in tagMatches)
                {
                    string tagName = match.Groups[1].Value.ToLower();

                    // Проверяем, известен ли тег
                    if (!_htmlTags.Contains(tagName) && tagName.Length > 2)
                    {
                        // Ищем похожие теги
                        var similarTags = FindSimilarTags(tagName);

                        if (similarTags.Any())
                        {
                            string suggestion = string.Join(", ", similarTags);
                            string fixedTag = similarTags.First();
                            string fixedLine = line.Replace($"<{tagName}", $"<{fixedTag}")
                                                  .Replace($"</{tagName}", $"</{fixedTag}");

                            results.Add(new AnalysisResult(
                                i + 1,
                                "Validation",
                                $"Unknown or misspelled tag '{tagName}'",
                                $"Did you mean: {suggestion}?",
                                line,
                                fixedLine
                            ));
                        }
                    }
                }
            }

            return results;
        }

        private List<string> FindSimilarTags(string tag)
        {
            var similar = new List<string>();
            int minDistance = int.MaxValue;

            foreach (var htmlTag in _htmlTags)
            {
                int distance = LevenshteinDistance(tag, htmlTag);
                if (distance <= 2 && distance < minDistance)
                {
                    minDistance = distance;
                    similar.Add(htmlTag);
                }
            }

            return similar;
        }

        private List<AnalysisResult> CheckTableStructure(string[] lines)
        {
            var results = new List<AnalysisResult>();
            bool inTable = false;
            int tableStartLine = 0;
            bool hasThead = false;
            bool hasTbody = false;
            int rowCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim().ToLower();

                if (trimmed.StartsWith("<table"))
                {
                    inTable = true;
                    tableStartLine = i + 1;
                    hasThead = false;
                    hasTbody = false;
                    rowCount = 0;
                }
                else if (trimmed.StartsWith("</table>") && inTable)
                {
                    // Проверяем структуру таблицы перед закрытием
                    if (rowCount == 0)
                    {
                        results.Add(new AnalysisResult(
                            tableStartLine,
                            "Structure",
                            "Empty table (no rows)",
                            "Add table rows with <tr>",
                            lines[tableStartLine - 1],
                            "<table>\n    <tr>\n        <td>Data</td>\n    </tr>\n</table>"
                        ));
                    }

                    if (!hasThead && rowCount > 0)
                    {
                        results.Add(new AnalysisResult(
                            tableStartLine,
                            "Structure",
                            "Table missing <thead> for headers",
                            "Consider adding <thead> with <th> cells",
                            lines[tableStartLine - 1],
                            "<!-- Consider adding: <thead><tr><th>Header</th></tr></thead> -->"
                        ));
                    }

                    if (!hasTbody && rowCount > 0)
                    {
                        results.Add(new AnalysisResult(
                            tableStartLine,
                            "Structure",
                            "Table missing <tbody> for data",
                            "Consider adding <tbody> around data rows",
                            lines[tableStartLine - 1],
                            "<!-- Consider adding: <tbody> around your data rows -->"
                        ));
                    }

                    inTable = false;
                }
                else if (inTable)
                {
                    if (trimmed.StartsWith("<thead>")) hasThead = true;
                    if (trimmed.StartsWith("<tbody>")) hasTbody = true;

                    if (trimmed.StartsWith("<tr"))
                    {
                        rowCount++;

                        // Проверяем, есть ли ячейки в строке
                        bool hasCells = false;
                        for (int j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                        {
                            string nextLine = lines[j].Trim().ToLower();
                            if (nextLine.StartsWith("<td") || nextLine.StartsWith("<th"))
                            {
                                hasCells = true;
                                break;
                            }
                            if (nextLine.StartsWith("</tr>"))
                                break;
                        }

                        if (!hasCells)
                        {
                            results.Add(new AnalysisResult(
                                i + 1,
                                "Structure",
                                "Table row without cells",
                                "Add <td> or <th> cells inside <tr>",
                                trimmed,
                                "<tr>\n    <td>Cell content</td>\n</tr>"
                            ));
                        }
                    }

                    // Проверяем th вне thead
                    if (trimmed.StartsWith("<th") && !hasThead)
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Structure",
                            "<th> should be inside <thead>",
                            "Move <th> cells to <thead> section",
                            trimmed,
                            "<!-- Move to: <thead><tr>" + trimmed + "</tr></thead> -->"
                        ));
                    }

                    // Проверяем tr непосредственно в table (без tbody)
                    if (trimmed.StartsWith("<tr") && !hasTbody && !hasThead)
                    {
                        results.Add(new AnalysisResult(
                            i + 1,
                            "Structure",
                            "Table rows should be in <tbody>",
                            "Wrap rows in <tbody>",
                            trimmed,
                            "<tbody>\n    " + trimmed + "\n</tbody>"
                        ));
                    }
                }
            }

            return results;
        }

        private List<AnalysisResult> CheckRequiredAttributes(string[] lines)
        {
            var results = new List<AnalysisResult>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim().ToLower();

                foreach (var tag in _requiredAttributes.Keys)
                {
                    if (trimmed.StartsWith($"<{tag}"))
                    {
                        var requiredAttrs = _requiredAttributes[tag];
                        foreach (var attr in requiredAttrs)
                        {
                            if (!Regex.IsMatch(trimmed, $@"\b{attr}\s*="))
                            {
                                string tagName = tag.ToUpper();
                                results.Add(new AnalysisResult(
                                    i + 1,
                                    "Validation",
                                    $"<{tagName}> missing required attribute '{attr}'",
                                    $"Add {attr}=\"\" attribute",
                                    trimmed,
                                    trimmed.Replace($"<{tag}", $"<{tag} {attr}=\"\"")
                                ));
                            }
                        }
                    }
                }
            }

            return results;
        }

        private int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] matrix = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) matrix[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) matrix[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[a.Length, b.Length];
        }

        public override bool CanHandle(string code, string fileExtension = "")
        {
            if (!string.IsNullOrEmpty(fileExtension))
            {
                string ext = fileExtension.ToLower();
                return ext == ".html" || ext == ".htm";
            }

            if (string.IsNullOrEmpty(code))
                return false;

            string cleanCode = code.Trim();

            // Проверяем по характерным признакам HTML
            bool hasHtmlStructure =
                cleanCode.Contains("<!DOCTYPE") ||
                cleanCode.Contains("<html") ||
                cleanCode.Contains("<head") ||
                cleanCode.Contains("<body") ||
                cleanCode.Contains("<div") ||
                cleanCode.Contains("<p>") ||
                cleanCode.Contains("</") ||
                (cleanCode.Contains("<") && cleanCode.Contains(">") &&
                 Regex.IsMatch(cleanCode, @"<\w+[^>]*>.*</\w+>"));

            // Исключаем другие языки
            bool notOtherLanguage =
                !cleanCode.Contains("<?xml") &&
                !cleanCode.Contains("<?php") &&
                !cleanCode.Contains("<%") &&
                !cleanCode.Contains("public class") &&
                !cleanCode.Contains("using ") &&
                !cleanCode.Contains("def ") &&
                !cleanCode.Contains("function ");

            return hasHtmlStructure && notOtherLanguage;
        }
    }
}