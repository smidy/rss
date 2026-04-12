using System.CommandLine;
using Rss.Services;
using Spectre.Console;

namespace Rss.Commands;

public static class ListCommand
{
    public static Command Build(ConfigService configService)
    {
        var cmd = new Command("list", "List all configured feed URLs");

        cmd.SetAction(_ =>
        {
            var config = configService.Load();
            if (config.Feeds.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No feeds configured. Use [/][bold]rss add <url>[/][grey] to add one.[/]");
                return;
            }
            foreach (var feed in config.Feeds)
                AnsiConsole.MarkupLine("  [blue]{0}[/]", feed);
        });

        return cmd;
    }
}
