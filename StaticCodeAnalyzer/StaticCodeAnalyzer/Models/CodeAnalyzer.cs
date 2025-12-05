using StaticCodeAnalyzer.Models.StyleGuides;
using System.Collections.Generic;

namespace StaticCodeAnalyzer.Models
{
    public abstract class CodeAnalyzer
    {
        protected List<IStyleGuide> _styleGuides = new List<IStyleGuide>();

        public CodeAnalyzer()
        {
            InitializeStyleGuides();
        }

        protected virtual void InitializeStyleGuides()
        {
            // Базовые стилевые руководства
            _styleGuides.Add(new GoogleStyleGuide());
        }

        public abstract List<AnalysisResult> Analyze(string code);

        public virtual string FixCode(string code, List<AnalysisResult> errors)
        {
            var lines = code.Split('\n');
            foreach (var error in errors)
            {
                if (error.LineNumber > 0 && error.LineNumber <= lines.Length && !string.IsNullOrEmpty(error.FixedCode))
                {
                    lines[error.LineNumber - 1] = error.FixedCode;
                }
            }
            return string.Join("\n", lines);
        }

        public abstract bool CanHandle(string code, string fileExtension = "");

        // Новый метод для применения стилевых правил
        protected List<AnalysisResult> ApplyStyleRules(string code, string language)
        {
            var results = new List<AnalysisResult>();
            var lines = code.Split('\n');

            foreach (var styleGuide in _styleGuides)
            {
                foreach (var rule in styleGuide.GetRules())
                {
                    // Применяем правила в зависимости от языка
                    var ruleResults = CheckStyleRule(lines, rule, language, styleGuide.Name);
                    results.AddRange(ruleResults);
                }
            }

            return results;
        }

        protected virtual List<AnalysisResult> CheckStyleRule(string[] lines, StyleRule rule, string language, string styleGuideName)
        {
            // Базовая реализация - должна быть переопределена в конкретных анализаторах
            return new List<AnalysisResult>();
        }
    }
}