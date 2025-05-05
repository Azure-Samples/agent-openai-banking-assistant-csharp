
[Route("api/chat")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;

    private IAgentRouter _agenticRouter;

    public ChatController(ILogger<ChatController> logger, IAgentRouter agenticRouter)
    {
        _logger = logger;
        _agenticRouter = agenticRouter;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Ok("Chat Controller is available.");
    }

    [HttpPost]
    [Produces("application/json")]
    public IActionResult ChatWithOpenAI([FromBody] ChatAppRequest chatRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (chatRequest.Stream)
        {
            _logger.LogWarning(
                "Requested a content-type of application/json however also requested streaming. " +
                "Please use a content-type of application/ndjson");
            return BadRequest(
                "Requested a content-type of application/json however also requested streaming. " +
                "Please use a content-type of application/ndjson");
        }

        if (chatRequest.Messages == null || !chatRequest.Messages.Any())
        {
            _logger.LogWarning("history cannot be null in Chat request");
            return BadRequest();
        }

        ChatHistory chatHistory = ConvertSKChatHistory(chatRequest);

        _logger.LogDebug("Processing chat conversation..", chatHistory[chatHistory.Count-1].Content);

        var agentContext = new AgentContext();
        agentContext.Add("requestContext", chatRequest.Context);
        agentContext.Add("attachments", chatRequest.Attachments);
        agentContext.Add("approach", chatRequest.Approach);

        _agenticRouter.Run(chatHistory, agentContext).Wait();

        ChatResponse response = ChatResponse.BuildChatResponse(chatHistory, agentContext);
        return new JsonResult(response);
    }

    private ChatHistory ConvertSKChatHistory(ChatAppRequest chatAppRequest)
    {
        ChatHistory chatHistory = new ChatHistory();
        foreach (var historyChat in chatAppRequest.Messages)
        {
            if ("user".Equals(historyChat.Role))
            {
                if (historyChat.Attachments == null || !historyChat.Attachments.Any())
                {
                    chatHistory.AddUserMessage(historyChat.Content);
                }   
                else
                {
                    string attachmentsString = string.Join(", ", historyChat.Attachments);
                    chatHistory.AddUserMessage($"{historyChat.Content} {attachmentsString}");
                }
            }
            if ("assistant".Equals(historyChat.Role))
                chatHistory.AddAssistantMessage(historyChat.Content);
        }
        // chatHistory.AddUserMessage(lastUserMessage.Content);

        return chatHistory;
    }
}


