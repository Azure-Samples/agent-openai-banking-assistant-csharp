# Intent classification evaluation

This runner evaluates the production `IntentExtractorAgent` against labeled
sample prompts using the configured Azure OpenAI deployment. It makes live
Azure OpenAI calls, so results can vary between runs.

## Prerequisites

- .NET 9 SDK
- An Azure OpenAI deployment accessible to the current identity
- Azure credentials available to `DefaultAzureCredential` (for example, sign in
  with `az login`)

Set these environment variables before running:

- `AZURE_OPENAI_CHATGPT_DEPLOYMENT`: Azure OpenAI deployment name
- `AZURE_OPENAI_ENDPOINT`: Azure OpenAI resource endpoint

## Run

From the repository root:

```sh
dotnet run --project app/evals/intent-classification-eval/intent-classification-eval.csproj
```

The runner prints overall and per-intent accuracy, lists misclassified prompts,
and exits with a non-zero status if any case fails.
