using System.Collections.Generic;

namespace StaticCodeAnalyzer.Models.StyleGuides
{
    public interface IStyleGuide
    {
        string Name { get; }
        string Description { get; }
        List<StyleRule> GetRules();
    }
}