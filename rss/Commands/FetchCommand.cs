using System.CommandLine;
using Rss.Services;
using Spectre.Console;

namespace Rss.Commands;

public static class FetchCommand
{
    public static Command Build(ConfigService configService, FeedService feedService,
        ArticleService articleService)
    {
        var urlsArg = new Argument<string[]>("urls")
        {
            Description = "One-off feed URLs (optional; uses config feeds if omitted)",
            Arity = ArgumentArity.ZeroOrMore,
        };

        var limitOpt = new Option<int?>("--limit") { Description = "Max number of articles to process per feed" };
        var offsetOpt = new Option<int>("--offset") { Description = "Number of articles to skip per feed" };
        var formatOpt = new Option<string>("--format") { Description = "Output format: rich (default) or markdown" };

        var cmd = new Command("fetch", "Fetch feeds and summarize articles via LLM") { urlsArg, limitOpt, offsetOpt, formatOpt };

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var urls = parseResult.GetValue(urlsArg) ?? [];
            var limit = parseResult.GetValue(limitOpt);
            var offset = parseResult.GetValue(offsetOpt);
            var format = parseResult.GetValue(formatOpt) ?? "rich";
            var markdown = format.Equals("markdown", StringComparison.OrdinalIgnoreCase);
            var config = configService.Load();
            var llmService = new LlmService(config.Llm);

            var feedUrls = urls.Length > 0 ? urls.ToList() : config.Feeds;

            if (feedUrls.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No feed URLs provided and no feeds in config.[/] Use [bold]rss add <url>[/] first.");
                return;
            }

            foreach (var feedUrl in feedUrls)
            {
                (string feedTitle, var entries) = await feedService.ReadAsync(feedUrl);

                var page = entries.Skip(offset);
                if (limit.HasValue) page = page.Take(limit.Value);
                var selected = page.ToList();

                if (markdown)
                {
                    Console.WriteLine($"# {feedTitle}");
                    Console.WriteLine();
                }
                else
                {
                    AnsiConsole.MarkupLine("\n[bold blue]Fetching:[/] {0}", feedUrl);
                    AnsiConsole.MarkupLine("[bold]{0}[/] — {1} article(s)", Markup.Escape(feedTitle), selected.Count);
                }

                foreach (var entry in selected)
                {
                    string summary = string.Empty;
                    var isImage = ImageService.IsImageUrl(entry.Url);

                    if (markdown)
                    {
                        // No spinner — stdout must stay clean for piping
                        if (isImage)
                        {
                            var (imageBytes, imageMediaType) = await ImageService.FetchAndPrepareAsync(entry.Url);
                            summary = await llmService.SummarizeAsync(entry.Title, string.Empty, imageBytes, imageMediaType);
                        }
                        else
                        {
                            var text = await articleService.FetchTextAsync(entry.Url);
                            summary = await llmService.SummarizeAsync(entry.Title, text);
                        }

                        Console.WriteLine($"## [{entry.Title}]({entry.Url})");
                        if (entry.Published.HasValue)
                            Console.WriteLine($"*{entry.Published.Value:yyyy-MM-dd HH:mm}*");
                        Console.WriteLine();
                        Console.WriteLine(summary);
                        Console.WriteLine();
                        Console.WriteLine("---");
                        Console.WriteLine();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("\n  [bold yellow]{0}[/]", Markup.Escape(entry.Title));
                        if (entry.Published.HasValue)
                            AnsiConsole.MarkupLine("  [grey]{0}[/]", entry.Published.Value.ToString("yyyy-MM-dd HH:mm"));
                        AnsiConsole.MarkupLine("  [grey]{0}[/]", entry.Url);

                        await AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .StartAsync(isImage ? "Downloading image..." : "Fetching article...", async ctx =>
                            {
                                if (isImage)
                                {
                                    var (imageBytes, imageMediaType) = await ImageService.FetchAndPrepareAsync(entry.Url);
                                    ctx.Status("Summarizing...");
                                    summary = await llmService.SummarizeAsync(entry.Title, string.Empty, imageBytes, imageMediaType);
                                }
                                else
                                {
                                    var text = await articleService.FetchTextAsync(entry.Url);
                                    ctx.Status("Summarizing...");
                                    summary = await llmService.SummarizeAsync(entry.Title, text);
                                }
                            });

                        AnsiConsole.Write(new Panel(Markup.Escape(summary))
                            .Header(isImage ? "[green]Summary (image)[/]" : "[green]Summary[/]")
                            .Expand()
                            .BorderColor(Color.Grey));
                    }
                }
            }
        });

        return cmd;
    }
}
