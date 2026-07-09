using CodeMonkey.UI.Rendering.Services;
using CodeMonkey.UI.Rendering.Models;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class MarkdownRenderingTests
    {
        private IMarkdownComponentRenderer _renderer;

        [SetUp]
        public void Setup()
        {
            _renderer = new MarkdownComponentRenderer();
        }

        [Test]
        public void Render_Header_ReturnsMarkdownHeaderElement()
        {
            var markdown = "# Hello World";
            var result = _renderer.Render(markdown).ToList();

            Assert.That(result, Has.One.TypeOf<MarkdownHeaderElement>());
            var header = (MarkdownHeaderElement)result[0];
            Assert.That(header.Level, Is.EqualTo(1));
            Assert.That(header.Text, Is.EqualTo("Hello World"));
        }

        [Test]
        public void Render_Paragraph_ReturnsMarkdownTextElement()
        {
            var markdown = "This is a simple paragraph.";
            var result = _renderer.Render(markdown).ToList();

            Assert.That(result, Has.One.TypeOf<MarkdownTextElement>());
            var text = (MarkdownTextElement)result[0];
            Assert.That(text.Text, Is.EqualTo("This is a simple paragraph."));
        }

        [Test]
        public void Render_CodeBlock_ReturnsMarkdownCodeElement()
        {
            var markdown = "```csharp\nvar x = 10;\n```";
            var result = _renderer.Render(markdown).ToList();

            Assert.That(result, Has.One.TypeOf<MarkdownCodeElement>());
            var code = (MarkdownCodeElement)result[0];
            Assert.That(code.Language, Is.EqualTo("csharp"));
            Assert.That(code.Code, Is.EqualTo("var x = 10;"));
        }

        [Test]
        public void Render_List_ReturnsMarkdownListElement()
        {
            var markdown = "- Item 1\n- Item 2";
            var result = _renderer.Render(markdown).ToList();

            Assert.That(result, Has.One.TypeOf<MarkdownListElement>());
            var list = (MarkdownListElement)result[0];
            Assert.That(list.IsOrdered, Is.False);
            Assert.That(list.Items, Has.Count.EqualTo(2));
            Assert.That(list.Items[0], Is.EqualTo("Item 1"));
            Assert.That(list.Items[1], Is.EqualTo("Item 2"));
        }

        [Test]
        public void Render_OrderedList_ReturnsMarkdownListElement()
        {
            var markdown = "1. First\n2. Second";
            var result = _renderer.Render(markdown).ToList();

            Assert.That(result, Has.One.TypeOf<MarkdownListElement>());
            var list = (MarkdownListElement)result[0];
            Assert.That(list.IsOrdered, Is.True);
            Assert.That(list.Items, Has.Count.EqualTo(2));
            Assert.That(list.Items[0], Is.EqualTo("First"));
            Assert.That(list.Items[1], Is.EqualTo("Second"));
        }

        [Test]
        public void Render_EmptyMarkdown_ReturnsEmptyList()
        {
            var markdown = "";
            var result = _renderer.Render(markdown).ToList();

            Assert.That(result, Is.Empty);
        }
    }
}
