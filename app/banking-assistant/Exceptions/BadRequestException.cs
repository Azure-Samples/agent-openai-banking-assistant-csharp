using System.Runtime.Serialization;

namespace BankingAssistant.Exceptions;

/// <summary>
/// Represents a Bad Request Exception
/// </summary>
[Serializable]
public sealed class BadRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="BadRequestException"/>
    /// </summary>
    public BadRequestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BadRequestException"/>
    /// </summary>
    public BadRequestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BadRequestException"/>
    /// </summary>
    public BadRequestException(string message, Exception inner)
        : base(message, inner)
    {
    }

#pragma warning disable SYSLIB0051
    private BadRequestException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#pragma warning restore SYSLIB0051
}
