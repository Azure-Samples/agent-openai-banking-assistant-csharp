using BankingAssistant.Models;
using FluentValidation;

namespace BankingAssistant.Validators;

/// <summary>
/// Validator for chat application requests.
/// </summary>
public class ChatAppRequestValidator : AbstractValidator<ChatAppRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAppRequestValidator"/> class.
    /// </summary>
    public ChatAppRequestValidator()
    {
        RuleFor(x => x.Messages)
            .NotNull()
            .WithMessage("Messages are required in the chat request.")
            .Must(messages => messages.Count > 0)
            .WithMessage("Messages cannot be empty. At least one message is required.");

        RuleFor(x => x.Messages)
            .ForEach(messageRule =>
                messageRule.SetValidator(new ResponseMessageValidator()))
            .When(x => x.Messages != null);

        RuleFor(x => x.Stream)
            .Equal(false)
            .WithMessage("Streaming is not supported. Please use application/ndjson content-type for streaming responses.");

        RuleFor(x => x.Approach)
            .NotEmpty()
            .WithMessage("Approach is required and cannot be empty.");
    }
}

/// <summary>
/// Validator for response messages.
/// </summary>
public class ResponseMessageValidator : AbstractValidator<ResponseMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseMessageValidator"/> class.
    /// </summary>
    public ResponseMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content cannot be empty.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Message role is required.")
            .Must(role => role.Equals("user", StringComparison.OrdinalIgnoreCase) || 
                         role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ||
                         role.Equals("system", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Message role must be 'user', 'assistant', or 'system'.");
    }
}

/// <summary>
/// Validator for chat application request context.
/// </summary>
public class ChatAppRequestContextValidator : AbstractValidator<ChatAppRequestContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAppRequestContextValidator"/> class.
    /// </summary>
    public ChatAppRequestContextValidator()
    {
        RuleFor(x => x.Overrides)
            .SetValidator(new ChatAppRequestOverridesValidator()!)
            .When(x => x.Overrides != null);
    }
}

/// <summary>
/// Validator for chat application request overrides.
/// </summary>
public class ChatAppRequestOverridesValidator : AbstractValidator<ChatAppRequestOverrides>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAppRequestOverridesValidator"/> class.
    /// </summary>
    public ChatAppRequestOverridesValidator()
    {
        RuleFor(x => x.Top)
            .GreaterThan(0)
            .WithMessage("Top must be greater than 0.")
            .When(x => x.Top.HasValue);

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0f, 1f)
            .WithMessage("Temperature must be between 0.0 and 1.0.")
            .When(x => x.Temperature.HasValue);
    }
}
