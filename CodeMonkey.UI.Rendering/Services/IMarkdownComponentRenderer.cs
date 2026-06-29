using System.Collections.Generic;

namespace CodeMonkey.UI.Rendering.Services
{
    public interface IMarkdownComponentRenderer
    {
        IEnumerable<CodeMonkey.UI.Rendering.Models.MarkdownElement> Render(string markdown);
    }
}
