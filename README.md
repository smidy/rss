# rss

A .NET CLI tool that fetches RSS/Atom feeds, scrapes the full article content, and summarizes each article using a local LLM via an [OpenAI-compatible endpoint](https://lmstudio.ai/docs/developer/openai-compat/chat-completions) (e.g. LM Studio).

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [LM Studio](https://lmstudio.ai) (or any OpenAI-compatible server) running locally

## Installation

### Run directly

```bash
git clone https://github.com/smidy/rss.git
cd rss/rss
dotnet run -- <command>
```

### Install as a global tool

```bash
cd rss/rss
dotnet pack
dotnet tool install -g --add-source ./bin/Release rss
```

Then use `rss` from anywhere.

## Setup

**1. Start LM Studio** and load a model, then enable the local server (default: `http://localhost:1234`).

**2. Set your model name:**

```bash
rss config set model <model-identifier>
```

The model identifier matches what LM Studio shows (e.g. `lmstudio-community/Meta-Llama-3-8B-Instruct-GGUF`).

**3. Add feeds:**

```bash
rss add https://feeds.arstechnica.com/arstechnica/index
rss add https://hnrss.org/frontpage
```

**4. Fetch and summarize:**

```bash
rss fetch
```

## Commands

| Command | Description |
|---------|-------------|
| `rss add <url>` | Add a feed URL to the config |
| `rss remove <url>` | Remove a feed URL from the config |
| `rss list` | List all configured feed URLs |
| `rss fetch [urls...] [--limit N] [--offset N] [--format rich\|markdown] [--max-words N]` | Fetch and summarize articles |
| `rss config set endpoint <value>` | Set the LLM API endpoint |
| `rss config set model <value>` | Set the model identifier |
| `rss config set apiKey <value>` | Set the API key (default: `lm-studio`) |
| `rss config set maxSummaryWords <value>` | Set max summary length in words (0 = unlimited) |
| `rss config show` | Show current LLM settings |

### One-off feeds

Pass URLs directly to `fetch` without adding them to the config:

```bash
rss fetch https://hnrss.org/frontpage
```

### Limit and offset

Control how many articles are processed per feed:

```bash
rss fetch --limit 5            # first 5 articles
rss fetch --offset 10          # skip the first 10
rss fetch --limit 5 --offset 10  # articles 11–15
```

### Markdown output

Use `--format markdown` to get clean markdown on stdout — useful for piping to files or agents:

```bash
rss fetch --format markdown
rss fetch --format markdown > summary.md
rss fetch --format markdown --limit 3 | my-agent
```

Output structure:

```markdown
# Feed Title

## Article Title
*2026-04-07 09:39*

Summary text here...

---
```

The default `--format rich` renders with Spectre.Console formatting for terminal viewing.

### Summary length

Limit how many words the LLM uses per summary:

```bash
rss config set maxSummaryWords 100   # persist across all fetches
rss fetch --max-words 50             # override for one run
rss config set maxSummaryWords 0     # restore unlimited (default)
```

`--max-words` takes priority over the config value when both are set.

### Image items (vision models)

When an RSS entry links directly to an image (`.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.avif`, `.bmp`), the tool detects this automatically and sends the image to the LLM instead of scraping article text. No configuration needed — it works per-entry alongside normal article entries in the same feed.

Images are downloaded, resized to a maximum of 1024px on the longest side, and re-encoded as JPEG before being sent. The model must be a vision-capable model loaded in LM Studio for image entries to produce meaningful summaries.

## Configuration

Config is stored at `~/.config/rss/config.json` and created automatically on first run.

```json
{
  "feeds": [],
  "llm": {
    "endpoint": "http://localhost:1234/v1",
    "model": "model-identifier",
    "apiKey": "lm-studio",
    "maxSummaryWords": 0
  }
}
```

## How it works

1. Parses RSS/Atom feeds via [CodeHollow.FeedReader](https://github.com/CodeHollow/FeedReader)
2. Fetches the full HTML of each article and extracts paragraph text via [HtmlAgilityPack](https://html-agility-pack.net)
3. Sends the text (truncated to 8 000 chars) to the LLM with a summarization prompt
4. Detects entries that link directly to images and sends them as vision input instead of scraping text (resized to ≤1024px via [SkiaSharp](https://github.com/mono/SkiaSharp))
5. Prints formatted summaries to the terminal via [Spectre.Console](https://spectreconsole.net)
