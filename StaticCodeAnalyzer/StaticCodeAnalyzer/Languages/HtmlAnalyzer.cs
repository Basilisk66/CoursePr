// Models/HtmlAnalyzer.cs
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Models
{
    public class HtmlAnalyzer : CodeAnalyzer
    {
        public override List<AnalysisResult> Analyze(string code)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                // Правило 1: Проверка на незакрытые теги (упрощенно)
                var openTags = Regex.Matches(line, @"<(\w+)(?![^>]*\/>)>");
                var closeTags = Regex.Matches(line, @"</(\w+)>");

                // Простейшая проверка: если открывающих тегов в строке больше, чем закрывающих
                if (openTags.Count > closeTags.Count)
                {
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Syntax",
                        "Possible unclosed HTML tag.",
                        "Ensure all tags are properly closed.",
                        line.Trim(),
                        line.Trim() + " <!-- Check closing tag -->"
                    ));
                }

                // Правило 2: Проверка атрибутов на двойные кавычки
                var attrWithoutQuotes = Regex.Match(line, @"<\w+\s+[^>]*?\b(\w+)=([^'""\s>][^>]*?)(\s|>)");
                if (attrWithoutQuotes.Success)
                {
                    string fixedLine = Regex.Replace(line, @"(\b\w+=)([^'""\s>][^>]*?)(\s|>)", "$1\"$2\"$3");
                    results.Add(new AnalysisResult(
                        lineNumber,
                        "Validation",
                        "HTML attributes should be in double quotes.",
                        "Enclose the attribute value in double quotes.",
                        line.Trim(),
                        fixedLine.Trim()
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
                if (fileExtension.ToLower() == ".html" || fileExtension.ToLower() == ".htm")
                    return true;
            }

            // Проверяем по содержимому кода
            if (string.IsNullOrEmpty(code))
                return false;

            string cleanCode = code.Trim();

            // HTML теги
            bool hasHtmlTags =
                cleanCode.Contains("<!DOCTYPE html>") ||
                cleanCode.Contains("<html") ||
                cleanCode.Contains("<head") ||
                cleanCode.Contains("<body") ||
                cleanCode.Contains("<div") ||
                cleanCode.Contains("<p>") ||
                cleanCode.Contains("<span") ||
                cleanCode.Contains("<a href") ||
                cleanCode.Contains("<img") ||
                cleanCode.Contains("<table") ||
                cleanCode.Contains("<style") ||
                cleanCode.Contains("<script") && cleanCode.Contains("</script>");

            // Проверяем структуру HTML
            bool hasHtmlStructure =
                cleanCode.StartsWith("<") ||
                (cleanCode.Contains("<") && cleanCode.Contains(">")) ||
                cleanCode.Contains("</");

            // Исключаем XML и другие языки разметки
            bool notXml =
                !cleanCode.Contains("<?xml") &&
                !cleanCode.Contains("<xml") &&
                !cleanCode.StartsWith("<?php") &&
                !cleanCode.Contains("<%");

            // Для HTML должно быть достаточно тегов и структуры
            return (hasHtmlTags || hasHtmlStructure) && notXml;
        }
    }
}