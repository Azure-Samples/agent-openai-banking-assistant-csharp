using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATGPT_DEPLOYMENT")
    ?? Environment.GetEnvironmentVariable("AzureOpenAI__Deployment");
var endpoint =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint");

if (string.IsNullOrWhiteSpace(deploymentName) || string.IsNullOrWhiteSpace(endpoint))
{
    throw new InvalidOperationException(
        "Set AZURE_OPENAI_CHATGPT_DEPLOYMENT and AZURE_OPENAI_ENDPOINT before running the intent evaluation.");
}

// Use the same production classifier and Azure OpenAI deployment as the application.
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: deploymentName,
    endpoint: endpoint,
    credentials: new DefaultAzureCredential());
var kernel = kernelBuilder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var intentExtractorAgent = new IntentExtractorAgent(
    kernel,
    chatCompletionService,
    loggerFactory.CreateLogger<IntentExtractorAgent>());

// Keep expected labels explicit so classification results can be checked consistently.
var evalCases = new[]
{
    new IntentEvalCase("Bill payment", "Can you pay my electricity bill for $100?", IntentType.BillPayment),
    new IntentEvalCase("Bill payment", "Can you pay this bill for me?", IntentType.BillPayment),
    new IntentEvalCase("Repeat transaction", "Repeat my last payment to Acme.", IntentType.RepeatTransaction),
    new IntentEvalCase("Repeat transaction", "Make the same payment to Acme again.", IntentType.RepeatTransaction),
    new IntentEvalCase("Transaction history", "When was the last time I paid Acme?", IntentType.TransactionHistory),
    new IntentEvalCase("Transaction history", "Show me my recent transactions.", IntentType.TransactionHistory),
    new IntentEvalCase("Account info", "What is my account balance?", IntentType.AccountInfo),
    new IntentEvalCase("Account info", "Show my account details and payment methods.", IntentType.AccountInfo),
    new IntentEvalCase("Unsupported request", "Can you buy stocks for me?", IntentType.None)
};

var results = new List<IntentEvalResult>();

foreach (var evalCase in evalCases)
{
    var chatHistory = new ChatHistory();
    chatHistory.AddUserMessage(evalCase.Input);

    var response = await intentExtractorAgent.Run(chatHistory);
    var passed = response.intentType == evalCase.ExpectedIntent;
    results.Add(new IntentEvalResult(evalCase, response.intentType, passed));

    Console.WriteLine(
        $"{(passed ? "PASS" : "FAIL")} | {evalCase.Name} | expected={evalCase.ExpectedIntent} | actual={response.intentType}");
}

Console.WriteLine();
Console.WriteLine($"Overall accuracy: {results.Count(result => result.Passed)}/{results.Count} " +
                  $"({results.Count(result => result.Passed) * 100.0 / results.Count:0.0}%)");

foreach (var group in results.GroupBy(result => result.EvalCase.ExpectedIntent).OrderBy(group => group.Key))
{
    var passed = group.Count(result => result.Passed);
    Console.WriteLine($"  {group.Key}: {passed}/{group.Count()}");
}

var failures = results.Where(result => !result.Passed).ToList();
if (failures.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Intent classification evaluation failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(
            $"- \"{failure.EvalCase.Input}\" expected {failure.EvalCase.ExpectedIntent}, got {failure.ActualIntent}");
    }

    return 1;
}

return 0;

internal sealed record IntentEvalCase(string Name, string Input, IntentType ExpectedIntent);

internal sealed record IntentEvalResult(IntentEvalCase EvalCase, IntentType ActualIntent, bool Passed);
