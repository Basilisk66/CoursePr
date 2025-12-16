using System.Collections.Generic;

namespace StaticCodeAnalyzer.Models.StyleGuides
{
    public class GoogleStyleGuide : IStyleGuide
    {
        public string Name => "Google Style Guide";
        public string Description => "Google's coding standards for various languages";

        public List<StyleRule> GetRules()
        {
            return new List<StyleRule>
            {
                new StyleRule(
                    "GOOGLE_001",
                    "Naming",
                    "Class names use UpperCamelCase",
                    "Class name should use UpperCamelCase",
                    "Rename class to use UpperCamelCase",
                    "class MyClass { }",
                    "class my_class { }",
                    "class MyClass { }"
                ),
                new StyleRule(
                    "GOOGLE_002",
                    "Constants",
                    "Constant names use UPPER_CASE with underscores",
                    "Constant should be in UPPER_CASE",
                    "Rename constant to UPPER_CASE",
                    "const int MAX_SIZE = 100;",
                    "const int maxSize = 100;",
                    "const int MAX_SIZE = 100;"
                )
            };
        }
    }
}