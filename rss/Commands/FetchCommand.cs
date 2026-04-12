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

        var cmd = new Command("fetch", "Fetch feeds and summarize articles via LLM") { urlsArg };

        cmd.SetAction(async (parseResult, cancellationToken) =>
        {
            var urls = parseResult.GetValue(urlsArg) ?? [];
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
                AnsiConsole.MarkupLine("\n[bold blue]Fetching:[/] {0}", feedUrl);

                (string feedTitle, var entries) = await feedService.ReadAsync(feedUrl);

                AnsiConsole.MarkupLine("[bold]{0}[/] — {1} article(s)", Markup.Escape(feedTitle), entries.Count);

                foreach (var entry in entries)
                {
                    AnsiConsole.MarkupLine("\n  [bold yellow]{0}[/]", Markup.Escape(entry.Title));
                    if (entry.Published.HasValue)
                        AnsiConsole.MarkupLine("  [grey]{0}[/]", entry.Published.Value.ToString("yyyy-MM-dd HH:mm"));

                    string summary = string.Empty;
                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Fetching article...", async ctx =>
                        {
                            var text = await articleService.FetchTextAsync(entry.Url);
                            ctx.Status("Summarizing...");
                            summary = await llmService.SummarizeAsync(entry.Title, text);
                        });

                    AnsiConsole.Write(new Panel(summary)
                        .Header("[green]Summary[/]")
                        .Expand()
                        .BorderColor(Color.Grey));
                }
            }
        });

        return cmd;
    }
}
