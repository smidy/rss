# Plan: RSS Feed Summarizer CLI (.NET)

## Context

Build a .NET global CLI tool (`rss`) that fetches RSS/Atom feeds, scrapes the full article HTML for each entry, sends the content to a local LLM via an OpenAI-compatible endpoint (LM Studio), and prints summaries to stdout. Feed URLs are persisted in a config file but can also be passed as one-off CLI arguments.

---

## Architecture

```
rss/
├── rss.csproj
├── Program.cs                  ← entry point, DI setup, command wiring
├── Commands/
│   ├── FetchCommand.cs         ← `rss fetch [url...]`
│   ├── AddCommand.cs           ← `rss add <url>`
│   ├── RemoveCommand.cs        ← `rss remove <url>`
│   ├── ListCommand.cs          ← `rss list`
│   └── ConfigCommand.cs        ← `rss config set <key> <value>`
├── Services/
│   ├── ConfigService.cs        ← read/write ~/.config/rss/config.json
│   ├── FeedService.cs          ← parse RSS/Atom via CodeHollow.FeedReader
│   ├── ArticleService.cs       ← HTTP fetch + HtmlAgilityPack text extraction
│   └── LlmService.cs           ← OpenAI SDK with custom base URL
└── Models/
    ├── AppConfig.cs
    └── FeedEntry.cs
```

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `System.CommandLine` | CLI argument/subcommand parsing |
| `CodeHollow.FeedReader` | RSS and Atom feed parsing |
| `OpenAI` (official .NET SDK) | OpenAI-compatible chat completions |
| `HtmlAgilityPack` | HTML → plain text for articles |
| `Spectre.Console` | Formatted terminal output (panels, progress) |

---

## Config File

Location: `~/.config/rss/config.json`

```json
{
  "feeds": [],
  "llm": {
    "endpoint": "http://localhost:1234/v1",
    "model": "model-identifier",
    "apiKey": "lm-studio"
  }
}
```

---

## Commands

### `rss add <url>`
Append URL to `config.feeds`, save config.

### `rss remove <url>`
Remove URL from `config.feeds`, save config.

### `rss list`
Print all configured feed URLs.

### `rss config set endpoint|model|apiKey <value>`
Update LLM settings in config.

### `rss fetch [url...]`
1. If URLs provided: use those; else use all from config.
2. For each feed URL → parse via `FeedService` → get list of `FeedEntry` (title, link, published).
3. For each entry → `ArticleService.FetchTextAsync(link)` → strip HTML to plain text.
4. For each entry → `LlmService.SummarizeAsync(title, text)` → get summary string.
5. Print formatted output via Spectre.Console (feed title header, then per-article panel).

---

## Key Implementation Details

### FeedService
Use `FeedReader.ReadAsync(url)` from `CodeHollow.FeedReader`. Map items to `FeedEntry { Title, Url, Published, Description }`.

### ArticleService
`HttpClient.GetStringAsync(url)` → `HtmlDocument.LoadHtml()` → select `//body//p` text nodes → join into plain text. Truncate to ~8000 chars to stay within context limits.

### LlmService
Use the official `OpenAI` .NET SDK:
```csharp
var client = new OpenAIClient(
    new ApiKeyCredential(config.Llm.ApiKey),
    new OpenAIClientOptions { Endpoint = new Uri(config.Llm.Endpoint) });
var chat = client.GetChatClient(config.Llm.Model);
var response = await chat.CompleteChatAsync(messages);
```
System prompt: "You are a concise news summarizer. Summarize the article in 3-5 sentences."

### ConfigService
Read/write `AppConfig` as JSON using `System.Text.Json`. Config path: `Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), ".config", "rss", "config.json")`. Create dir/file on first run.

---

## .csproj Settings

```xml
<OutputType>Exe</OutputType>
<TargetFramework>net9.0</TargetFramework>
<AssemblyName>rss</AssemblyName>
<PackAsTool>true</PackAsTool>
<ToolCommandName>rss</ToolCommandName>
```

---

## Verification

1. `dotnet build` — zero errors/warnings
2. `dotnet run -- add https://feeds.arstechnica.com/arstechnica/index`
3. `dotnet run -- list` — shows the URL
4. `dotnet run -- config set model <your-loaded-model-id>`
5. `dotnet run -- fetch` — fetches feed, scrapes articles, prints summaries
6. `dotnet run -- fetch https://hnrss.org/frontpage` — one-off URL
7. `dotnet run -- remove <url>` — removes feed

---

## Errors Log
_none yet_
