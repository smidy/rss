using System.CommandLine;
using Rss.Services;
using Spectre.Console;

namespace Rss.Commands;

public static class RemoveCommand
{
    public static Command Build(ConfigService configService)
    {
        var urlArg = new Argument<string>("url") { Description = "RSS/Atom feed URL to remove" };
        var cmd = new Command("remove", "Remove a feed URL from the config") { urlArg };

        cmd.SetAction(parseResult =>
        {
            var url = parseResult.GetValue(urlArg)!;
            var config = configService.Load();
            if (!config.Feeds.Remove(url))
            {
                AnsiConsole.MarkupLine("[yellow]Feed not found:[/] {0}", url);
                return;
            }
            configService.Save(config);
            AnsiConsole.MarkupLine("[red]Removed:[/] {0}", url);
        });

        return cmd;
    }
}
