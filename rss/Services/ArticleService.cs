using HtmlAgilityPack;

namespace Rss.Services;

public class ArticleService
{
    private const int MaxChars = 8000;

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { { "User-Agent", "rss-summarizer/1.0" } },
    };

    public async Task<string> FetchTextAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        try
        {
            var html = await _http.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script, style, nav, header, footer noise
            foreach (var node in doc.DocumentNode.SelectNodes(
                "//script|//style|//nav|//header|//footer|//aside") ?? Enumerable.Empty<HtmlNode>())
            {
                node.Remove();
            }

            var paragraphs = doc.DocumentNode
                .SelectNodes("//body//p|//body//h1|//body//h2|//body//h3|//body//li")
                ?? Enumerable.Empty<HtmlNode>();

            var text = string.Join("\n\n", paragraphs
                .Select(p => HtmlEntity.DeEntitize(p.InnerText).Trim())
                .Where(t => t.Length > 20));

            return text.Length > MaxChars ? text[..MaxChars] : text;
        }
        catch
        {
            return string.Empty;
        }
    }
}
