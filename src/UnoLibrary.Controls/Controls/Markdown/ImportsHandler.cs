using Markdig;
using Markdig.Syntax;
using Symptum.Markdown.Embedding;
using Symptum.UI.Markdown.Renderers;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown;

public class ImportsHandler
{
    private Dictionary<string, ImportBlockElement> importBlocks = [];

    public void RegisterForImport(string importId, ImportBlockElement importBlockElement)
    {
        importBlocks.TryAdd(importId, importBlockElement);
    }

    internal void ResolveImports(IEnumerable<ExportBlock>? availableExports, WinUIRenderer renderer, MarkdownPipeline pipeline)
    {
        foreach (var kvp in importBlocks)
        {
            string id = kvp.Key;
            ExportBlock? match = null;
            // Resource-backed imports ("Symptum?..." ids) are not wired up in this fork;
            // only same-document exports can be resolved.
            if (!id.StartsWith(nameof(Symptum)))
            {
                match = availableExports?.FirstOrDefault(e => kvp.Key.Equals(e.Id.ToString(), StringComparison.InvariantCulture));
            }

            if (match != null)
            {
                var importBlock = kvp.Value;
                renderer.Push(importBlock);
                renderer.WriteChildren(match);
                renderer.Pop(false);
            }
        }
        importBlocks.Clear();
    }
}
