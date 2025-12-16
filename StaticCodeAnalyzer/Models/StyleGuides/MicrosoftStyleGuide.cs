using System.Collections.Generic;

namespace StaticCodeAnalyzer.Models.StyleGuides
{
    public class MicrosoftStyleGuide : IStyleGuide
    {
        public string Name => "Microsoft C# Coding Conventions";
        public string Description => "Official Microsoft coding conventions for C#";

        public List<StyleRule> GetRules()
        {
            return new List<StyleRule>
            {
                new StyleRule(
                    "MS_001",
                    "Naming",
                    "Use PascalCase for class names and method names",
                    "Class name should use PascalCase",
                    "Rename class to use PascalCase",
                    "public class MyClass { }",
                    "public class myClass { }",
                    "public class MyClass { }"
                ),
                new StyleRule(
                    "MS_002",
                    "Code Style",
                    "Use implicit typing for local variables when the type is obvious",
                    "Consider using 'var' for implicit typing",
                    "Replace explicit type with 'var'",
                    "var items = new List<string>();",
                    "List<string> items = new List<string>();",
                    "var items = new List<string>();"
                )
            };
        }
    }
}