# RSS Summarizer — Claude Instructions

## Project

.NET 9 global CLI tool (`rss`) that fetches RSS/Atom feeds, scrapes full article HTML, and summarizes via an OpenAI-compatible LLM endpoint (LM Studio).

## Structure

```
rss/          ← .NET project (rss.csproj)
  Commands/   ← one static class per subcommand
  Models/     ← AppConfig, FeedEntry
  Services/   ← ConfigService, FeedService, ArticleService, LlmService
Plans/        ← planning docs
```

## Build & Run

```bash
dotnet build
dotnet run -- <command>
```

## System.CommandLine 3.0-preview API

This project uses `3.0.0-preview` which has breaking changes from 2.x:

- `Argument<T>` constructor takes only `name`; set `.Description` as a property
- Use `cmd.SetAction(ParseResult => { ... })` — **not** `SetHandler`
- For async: `cmd.SetAction(async (ParseResult, CancellationToken) => { ... })`
- Read values: `parseResult.GetValue(argVar)`
- Invoke: `root.Parse(args)` → `await parseResult.InvokeAsync()`

## Config

Stored at `~/.config/rss/config.json`. Created automatically on first run.

## Commands

| Command | Description |
|---------|-------------|
| `rss add <url>` | Add feed to config |
| `rss remove <url>` | Remove feed from config |
| `rss list` | List configured feeds |
| `rss config set endpoint\|model\|apiKey <value>` | Update LLM settings |
| `rss config show` | Show current LLM settings |
| `rss fetch [urls...]` | Fetch and summarize (uses config feeds if no URLs given) |
