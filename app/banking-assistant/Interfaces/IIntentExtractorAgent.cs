public interface IIntentExtractorAgent
{
    Task<IntentResponse> Run(ChatHistory userChatHistory);
}