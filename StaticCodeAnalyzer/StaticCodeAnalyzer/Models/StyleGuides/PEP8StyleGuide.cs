using System.Collections.Generic;

namespace StaticCodeAnalyzer.Models.StyleGuides
{
    public class PEP8StyleGuide : IStyleGuide
    {
        public string Name => "PEP 8";
        public string Description => "Python Enhancement Proposal 8 - Style Guide for Python Code";

        public List<StyleRule> GetRules()
        {
            return new List<StyleRule>
            {
                new StyleRule(
                    "PEP8_001",
                    "Indentation",
                    "Use 4 spaces per indentation level",
                    "Use 4 spaces for indentation instead of tabs",
                    "Replace tabs with 4 spaces",
                    "def example():\n    print('indented with 4 spaces')"
                ),
                new StyleRule(
                    "PEP8_002",
                    "Line Length",
                    "Limit all lines to a maximum of 79 characters",
                    "Line exceeds 79 characters",
                    "Break the line into multiple lines",
                    "result = (value1 + value2 + value3 + \n          value4 + value5)"
                ),
                new StyleRule(
                    "PEP8_003",
                    "Naming",
                    "Function names should be lowercase, with words separated by underscores",
                    "Function name should use snake_case",
                    "Rename function to use snake_case",
                    "def my_function_name():",
                    "def myFunctionName():",
                    "def my_function_name():"
                ),
                new StyleRule(
                    "PEP8_004",
                    "Imports",
                    "Imports should usually be on separate lines",
                    "Multiple imports on one line",
                    "Put each import on a separate line",
                    "import os\nimport sys",
                    "import os, sys",
                    "import os\nimport sys"
                ),
                new StyleRule(
                    "PEP8_005",
                    "Whitespace",
                    "Surround operators with a single space on either side",
                    "Missing spaces around operator",
                    "Add spaces around the operator",
                    "x = 5 + 3",
                    "x=5+3",
                    "x = 5 + 3"
                )
            };
        }
    }
}