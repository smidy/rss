using CodeHollow.FeedReader;
using Rss.Models;

namespace Rss.Services;

public class FeedService
{
    public async Task<(string FeedTitle, List<FeedEntry> Entries)> ReadAsync(string url)
    {
        var feed = await FeedReader.ReadAsync(url);
        var feedTitle = feed.Title ?? url;

        var entries = feed.Items.Select(item => new FeedEntry
        {
            Title = item.Title ?? "(no title)",
            Url = item.Link ?? string.Empty,
            Published = item.PublishingDate.HasValue
                ? new DateTimeOffset(item.PublishingDate.Value)
                : null,
            FeedTitle = feedTitle,
        }).ToList();

        return (feedTitle, entries);
    }
}
