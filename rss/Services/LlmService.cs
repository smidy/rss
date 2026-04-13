using OpenAI;
using OpenAI.Chat;
using Rss.Models;
using System.ClientModel;

namespace Rss.Services;

public class LlmService
{
    private const string SystemPrompt =
        "You are a concise news summarizer. Summarize the article in 3-5 sentences, " +
        "focusing on the key facts and takeaways. Be direct and avoid filler phrases.";

    private readonly LlmConfig _config;

    public LlmService(LlmConfig config)
    {
        _config = config;
    }

    public async Task<string> SummarizeAsync(string title, string articleText,
        byte[]? imageBytes = null, string? imageMediaType = null)
    {
        if (string.IsNullOrWhiteSpace(articleText) && imageBytes is null)
            return "(no article text or image could be retrieved)";

        var client = new OpenAIClient(
            new ApiKeyCredential(_config.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(_config.Endpoint) });

        var chat = client.GetChatClient(_config.Model);

        ChatMessageContentPart textPart = ChatMessageContentPart.CreateTextPart(
            $"Title: {title}\n\n{articleText}");

        UserChatMessage userMessage = imageBytes is not null
            ? new UserChatMessage(
                textPart,
                ChatMessageContentPart.CreateImagePart(
                    BinaryData.FromBytes(imageBytes),
                    imageMediaType ?? "image/jpeg"))
            : new UserChatMessage(textPart);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            userMessage,
        };

        var response = await chat.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }
}
