using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Filters;
using BankingAssistant.Exceptions;

namespace BankingAssistant.Filters;

/// <summary>
/// Api exception filter which returns a proper exception with details back to the caller
/// </summary>
[ExcludeFromCodeCoverage]
public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;
    private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;

    /// <summary>
    /// Register known exception types and handlers.
    /// </summary>
    public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
        {
            { typeof(BadRequestException), HandleBadRequestException },
            { typeof(ConflictException), HandleConflictException },
            { typeof(ItemNotFoundException), HandleItemNotFoundException },
            { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
            { typeof(ValidationException), HandleValidationException }
        };
    }

    /// <summary>
    /// Handles the exception based on the <see cref="ExceptionContext"/>
    /// </summary>
    /// <param name="context"></param>
    public override void OnException(ExceptionContext context)
    {
        HandleException(context);

        base.OnException(context);
    }

    private void HandleException(ExceptionContext context)
    {
        var type = context.Exception.GetType();

        if (_exceptionHandlers.ContainsKey(type))
        {
            _exceptionHandlers[type].Invoke(context);
        }
        else if (!context.ModelState.IsValid)
        {
            HandleInvalidModelStateException(context);
        }
        else
        {
            HandleOtherException(context);
        }
    }

    private void HandleBadRequestException(ExceptionContext context)
    {
        var exception = (BadRequestException)context.Exception;

        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "The specified resource experienced a bad request.",
            Detail = exception.Message,
            Extensions = {
                { "traceId", Activity.Current?.Id },
                { "timestamp", DateTimeOffset.UtcNow }
            }
        };

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, details.Detail);

        context.Result = new BadRequestObjectResult(details);
        context.ExceptionHandled = true;
    }

    private void HandleConflictException(ExceptionContext context)
    {
        var exception = (ConflictException)context.Exception;

        var details = new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.8",
            Title = "The specified resource experienced a conflict.",
            Detail = exception.Message,
            Extensions = {
                { "traceId", Activity.Current?.Id },
                { "timestamp", DateTimeOffset.UtcNow }
            }
        };

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, details.Detail);

        context.Result = new ConflictObjectResult(details);
        context.ExceptionHandled = true;
    }

    private void HandleItemNotFoundException(ExceptionContext context)
    {
        var exception = (ItemNotFoundException)context.Exception;

        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message,
            Extensions = {
                { "traceId", Activity.Current?.Id },
                { "timestamp", DateTimeOffset.UtcNow }
            }
        };

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, details.Detail);

        context.Result = new NotFoundObjectResult(details);
        context.ExceptionHandled = true;
    }

    private void HandleInvalidModelStateException(ExceptionContext context)
    {
        var exception = context.Exception;

        var details = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
        details.Extensions.Add("traceId", Activity.Current?.Id);
        details.Extensions.Add("timestamp", DateTimeOffset.UtcNow);

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, exception.Message);

        context.Result = new BadRequestObjectResult(details);
        context.ExceptionHandled = true;
    }

    private void HandleOtherException(ExceptionContext context)
    {
        var exception = context.Exception;

        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Handled internal exception",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Detail = exception.Message,
            Extensions = {
                { "traceId", Activity.Current?.Id },
                { "timestamp", DateTimeOffset.UtcNow }
            }
        };

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, details.Detail);

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }

    private void HandleUnauthorizedAccessException(ExceptionContext context)
    {
        var exception = context.Exception;

        var details = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Detail = exception.Message,
            Extensions = {
                { "traceId", Activity.Current?.Id },
                { "timestamp", DateTimeOffset.UtcNow }
            }
        };

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, details.Detail);

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };

        context.ExceptionHandled = true;
    }

    private void HandleValidationException(ExceptionContext context)
    {
        var exception = (ValidationException)context.Exception;

        var details = new ValidationProblemDetails(exception.Errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        };
        details.Extensions.Add("traceId", Activity.Current?.Id);
        details.Extensions.Add("timestamp", DateTimeOffset.UtcNow);

        _logger.LogError(exception, "{Title}. Exception: {Message}", details.Title, details.Detail);

        context.Result = new BadRequestObjectResult(details);
        context.ExceptionHandled = true;
    }
}

