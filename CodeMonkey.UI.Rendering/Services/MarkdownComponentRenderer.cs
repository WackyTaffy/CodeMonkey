using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using CodeMonkey.UI.Rendering.Models;

namespace CodeMonkey.UI.Rendering.Services
{
    public class MarkdownComponentRenderer : IMarkdownComponentRenderer
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownComponentRenderer()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        public IEnumerable<MarkdownElement> Render(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return Enumerable.Empty<MarkdownElement>();

            var document = Markdown.Parse(markdown, _pipeline);
            var elements = new List<MarkdownElement>();

            foreach (var block in document)
            {
                var element = ProcessBlock(block);
                if (element != null)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }

        private MarkdownElement? ProcessBlock(Block block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    return new MarkdownHeaderElement
                    {
                        Level = heading.Level,
                        Text = GetInlineText(heading.Inline)
                    };

                case FencedCodeBlock fencedCode:
                    var fencedSb = new StringBuilder();
                    foreach (var line in fencedCode.Lines)
                    {
                        fencedSb.AppendLine(line?.ToString() ?? string.Empty);
                    }
                    return new MarkdownCodeElement
                    {
                        Code = fencedSb.ToString().TrimEnd(),
                        Language = fencedCode.Info?.Trim() ?? string.Empty
                    };

                case CodeBlock code:
                    var codeSb = new StringBuilder();
                    foreach (var line in code.Lines)
                    {
                        codeSb.AppendLine(line?.ToString() ?? string.Empty);
                    }
                    return new MarkdownCodeElement
                    {
                        Code = codeSb.ToString().TrimEnd(),
                        Language = string.Empty
                    };

                case ParagraphBlock paragraph:
                    var text = GetInlineText(paragraph.Inline);
                    return new MarkdownTextElement { Text = text };

                case ListBlock list:
                    var listElement = new MarkdownListElement { IsOrdered = list.IsOrdered };
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock listItem)
                        {
                            foreach (var child in listItem)
                            {
                                if (child is ParagraphBlock p)
                                {
                                    listElement.Items.Add(GetInlineText(p.Inline));
                                    break; 
                                }
                            }
                        }
                    }
                    return listElement;

                default:
                    return null;
            }
        }

        private string GetInlineText(Inline? inline)
        {
            if (inline == null) return string.Empty;

            var sb = new StringBuilder();
            
            if (inline is ContainerInline container)
            {
                foreach (var child in container)
                {
                    sb.Append(GetInlineText(child));
                }
            }
            else if (inline is LiteralInline literal)
            {
                sb.Append(literal.Content.ToString());
            }
            else
            {
                sb.Append(inline.ToString());
            }

            return sb.ToString();
        }
    }
}
