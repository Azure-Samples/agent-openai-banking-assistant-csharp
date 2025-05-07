public interface IAgentRouter
{
    Task Run(ChatHistory chatHistory, AgentContext agentContext);
}