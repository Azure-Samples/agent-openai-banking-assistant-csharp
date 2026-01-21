using System.Runtime.Serialization;

namespace BankingAssistant.Exceptions;

/// <summary>
/// Represents a conflict exception due to already existing data.
/// </summary>
[Serializable]
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConflictException"/>
    /// </summary>
    public ConflictException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictException"/>
    /// </summary>
    public ConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictException"/>
    /// </summary>
    public ConflictException(string message, Exception inner)
        : base(message, inner)
    {
    }

#pragma warning disable SYSLIB0051
    private ConflictException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#pragma warning restore SYSLIB0051
}
