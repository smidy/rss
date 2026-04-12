namespace Rss.Models;

public class FeedEntry
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset? Published { get; set; }
    public string FeedTitle { get; set; } = string.Empty;
}
