namespace Rss.Models;

public class AppConfig
{
    public List<string> Feeds { get; set; } = [];
    public LlmConfig Llm { get; set; } = new();
}

public class LlmConfig
{
    public string Endpoint { get; set; } = "http://localhost:1234/v1";
    public string Model { get; set; } = "model-identifier";
    public string ApiKey { get; set; } = "lm-studio";
}
