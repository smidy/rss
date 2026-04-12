using System.CommandLine;
using Rss.Services;
using Spectre.Console;

namespace Rss.Commands;

public static class AddCommand
{
    public static Command Build(ConfigService configService)
    {
        var urlArg = new Argument<string>("url") { Description = "RSS/Atom feed URL to add" };
        var cmd = new Command("add", "Add a feed URL to the config") { urlArg };

        cmd.SetAction(parseResult =>
        {
            var url = parseResult.GetValue(urlArg)!;
            var config = configService.Load();
            if (config.Feeds.Contains(url))
            {
                AnsiConsole.MarkupLine("[yellow]Feed already exists:[/] {0}", url);
                return;
            }
            config.Feeds.Add(url);
            configService.Save(config);
            AnsiConsole.MarkupLine("[green]Added:[/] {0}", url);
        });

        return cmd;
    }
}
