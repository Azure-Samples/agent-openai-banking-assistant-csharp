using FluentValidation.Results;
using System.Collections.Concurrent;
using System.Runtime.Serialization;

namespace BankingAssistant.Exceptions;

/// <summary>
/// Validation exception which captures validation details
/// </summary>
[Serializable]
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Validation Errors
    /// </summary>
    public IDictionary<string, string[]> Errors { get; } = new ConcurrentDictionary<string, string[]>();

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/>
    /// </summary>
    public ValidationException() : base("One or more validation failures have occurred")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/>
    /// </summary>
    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(validationFailure => validationFailure.PropertyName, validationFailure => validationFailure.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/>
    /// </summary>
    public ValidationException(Exception inner)
        : base("One or more validation failures have occurred", inner)
    {
    }

#pragma warning disable SYSLIB0051
    private ValidationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#pragma warning restore SYSLIB0051
}
