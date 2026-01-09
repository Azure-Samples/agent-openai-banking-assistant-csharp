using System.Text.Json.Serialization;

namespace BankingAssistant.Models;

/// <summary>
/// Represents a message in the chat conversation.
/// </summary>
/// <param name="Content">The text content of the message.</param>
/// <param name="Role">The role of the message sender (e.g., "user", "assistant", "system").</param>
/// <param name="Attachments">Optional list of attachment identifiers or URLs.</param>
public record ResponseMessage(string Content, string Role, List<string>? Attachments);

/// <summary>
/// Configuration overrides for chat request behavior.
/// </summary>
/// <param name="SemanticRanker">Whether to use semantic ranking for search results.</param>
/// <param name="SemanticCaptions">Whether to generate semantic captions.</param>
/// <param name="ExcludeCategory">Category to exclude from search results.</param>
/// <param name="Top">Number of top results to return.</param>
/// <param name="Temperature">Temperature parameter for response generation (0.0-1.0).</param>
/// <param name="PromptTemplate">Custom prompt template to use.</param>
/// <param name="PromptTemplatePrefix">Prefix to add to the prompt template.</param>
/// <param name="PromptTemplateSuffix">Suffix to add to the prompt template.</param>
/// <param name="SuggestFollowupQuestions">Whether to suggest follow-up questions.</param>
/// <param name="UseOidSecurityFilter">Whether to apply OID-based security filtering.</param>
/// <param name="UseGroupsSecurityFilter">Whether to apply groups-based security filtering.</param>
public record ChatAppRequestOverrides(
    bool? SemanticRanker,
    bool? SemanticCaptions,
    string? ExcludeCategory,
    int? Top,
    float? Temperature,
    string? PromptTemplate,
    string? PromptTemplatePrefix,
    string? PromptTemplateSuffix,
    bool? SuggestFollowupQuestions,
    bool? UseOidSecurityFilter,
    bool? UseGroupsSecurityFilter);

/// <summary>
/// Context information for a chat request, including configuration overrides.
/// </summary>
public record ChatAppRequestContext
{
    /// <summary>
    /// Gets or initializes the configuration overrides for this request.
    /// </summary>
    public ChatAppRequestOverrides? Overrides { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAppRequestContext"/> class.
    /// </summary>
    public ChatAppRequestContext() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAppRequestContext"/> class with the specified overrides.
    /// </summary>
    /// <param name="overrides">The configuration overrides for the request.</param>
    [JsonConstructor]
    public ChatAppRequestContext(ChatAppRequestOverrides? overrides)
    {
        Overrides = overrides;
    }
}

/// <summary>
/// Represents a chat request from the client.
/// </summary>
/// <param name="Messages">The conversation history as a list of messages.</param>
/// <param name="Stream">Whether to stream the response back to the client.</param>
/// <param name="Context">The request context including configuration overrides.</param>
/// <param name="Attachments">Optional list of attachments sent with the request.</param>
/// <param name="Approach">The approach or strategy to use for handling the request.</param>
public record ChatAppRequest(List<ResponseMessage> Messages, bool Stream, ChatAppRequestContext Context, List<string>? Attachments, string Approach);

/// <summary>
/// Represents a single choice in the chat response.
/// </summary>
/// <param name="Index">The index of this choice in the response.</param>
/// <param name="Message">The complete message for this choice.</param>
/// <param name="Context">Additional context information about this choice.</param>
/// <param name="Delta">The delta message for streaming responses.</param>
public record ResponseChoice(int Index, ResponseMessage Message, ResponseContext Context, ResponseMessage Delta);

/// <summary>
/// Provides additional context information about a response.
/// </summary>
/// <param name="Thoughts">The reasoning or thought process behind the response.</param>
/// <param name="DataPoints">List of data points or sources used to generate the response.</param>
public record ResponseContext(string Thoughts, List<string> DataPoints);

/// <summary>
/// Represents a chat response to be sent to the client.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChatResponse"/> class.
/// </remarks>
/// <param name="Choices">The list of response choices.</param>
public class ChatResponse(List<ResponseChoice> Choices)
{
    /// <summary>
    /// Gets or sets the list of response choices.
    /// </summary>
    public List<ResponseChoice> Choices { get; set; } = Choices;

    /// <summary>
    /// Builds a chat response from agent chat messages and context.
    /// </summary>
    /// <param name="chatMessages">The list of chat messages from the agent conversation.</param>
    /// <param name="context">Additional context data including thoughts, data points, and attachments.</param>
    /// <returns>A <see cref="ChatResponse"/> object formatted for the client.</returns>
    public static ChatResponse BuildChatResponse(List<ChatMessage> chatMessages, Dictionary<string, object> context)
    {
        var dataPoints = new List<string>();
        var thoughts = string.Empty;
        var attachments = new List<string>();

        if (context.TryGetValue("dataPoints", out object? value) && value != null)
        {
            dataPoints.AddRange((List<string>)value);
        }

        if (context.TryGetValue("thoughts", out object? thoughtsValue) && thoughtsValue != null)
        {
            thoughts = (string)thoughtsValue;
        }

        if (context.TryGetValue("attachments", out object? attachmentsValue) && attachmentsValue != null)
        {
            attachments.AddRange((List<string>)attachmentsValue);
        }

        // Get the last assistant message
        var lastMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        var content = lastMessage?.Text ?? string.Empty;

        return new ChatResponse(
			[
				new(0,
                    new ResponseMessage(
                        content,
                        "assistant",
                        attachments),
                    new ResponseContext(thoughts, dataPoints),
                    new ResponseMessage(
                        content,
                        "assistant",
                        attachments))
            ]);
    }
}