namespace StaticCodeAnalyzer.Models.StyleGuides
{
    public class StyleRule
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
        public string Example { get; set; }
        public string BadExample { get; set; }
        public string GoodExample { get; set; }

        public StyleRule(string id, string category, string description, string message, string suggestion,
                        string example = "", string badExample = "", string goodExample = "")
        {
            Id = id;
            Category = category;
            Description = description;
            Message = message;
            Suggestion = suggestion;
            Example = example;
            BadExample = badExample;
            GoodExample = goodExample;
        }
    }
}