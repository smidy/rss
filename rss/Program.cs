using System.CommandLine;
using Rss.Commands;
using Rss.Services;

var configService = new ConfigService();
var feedService = new FeedService();
var articleService = new ArticleService();

var root = new RootCommand("RSS feed summarizer — fetch and summarize articles using a local LLM");
root.Add(AddCommand.Build(configService));
root.Add(RemoveCommand.Build(configService));
root.Add(ListCommand.Build(configService));
root.Add(ConfigCommand.Build(configService));
root.Add(FetchCommand.Build(configService, feedService, articleService));

var parseResult = root.Parse(args);
return await parseResult.InvokeAsync();
