using System.Collections.Generic;

namespace CodeMonkey.UI.Rendering.Models
{
    public abstract class MarkdownElement { }

    public class MarkdownTextElement : MarkdownElement
    {
        public string Text { get; set; } = string.Empty;
    }

    public class MarkdownHeaderElement : MarkdownElement
    {
        public int Level { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class MarkdownCodeElement : MarkdownElement
    {
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    public class MarkdownLinkElement : MarkdownElement
    {
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class MarkdownTableElement : MarkdownElement
    {
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }

    public class MarkdownListElement : MarkdownElement
    {
        public bool IsOrdered { get; set; }
        public List<string> Items { get; set; } = new();
    }
}
