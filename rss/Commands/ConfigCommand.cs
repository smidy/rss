using System.CommandLine;
using Rss.Services;
using Spectre.Console;

namespace Rss.Commands;

public static class ConfigCommand
{
    public static Command Build(ConfigService configService)
    {
        var keyArg = new Argument<string>("key") { Description = "Setting key: endpoint | model | apiKey" };
        var valueArg = new Argument<string>("value") { Description = "New value" };
        var setCmd = new Command("set", "Update an LLM setting") { keyArg, valueArg };

        setCmd.SetAction(parseResult =>
        {
            var key = parseResult.GetValue(keyArg)!;
            var value = parseResult.GetValue(valueArg)!;
            var config = configService.Load();
            switch (key.ToLowerInvariant())
            {
                case "endpoint":
                    config.Llm.Endpoint = value;
                    break;
                case "model":
                    config.Llm.Model = value;
                    break;
                case "apikey":
                    config.Llm.ApiKey = value;
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]Unknown key:[/] {0}. Valid keys: endpoint, model, apiKey", key);
                    return;
            }
            configService.Save(config);
            AnsiConsole.MarkupLine("[green]Set[/] {0} = {1}", key, value);
        });

        var showCmd = new Command("show", "Show current LLM settings");
        showCmd.SetAction(_ =>
        {
            var config = configService.Load();
            var table = new Table().AddColumns("Key", "Value");
            table.AddRow("endpoint", config.Llm.Endpoint);
            table.AddRow("model", config.Llm.Model);
            table.AddRow("apiKey", config.Llm.ApiKey);
            AnsiConsole.Write(table);
        });

        var cmd = new Command("config", "View or update LLM configuration") { setCmd, showCmd };
        return cmd;
    }
}
