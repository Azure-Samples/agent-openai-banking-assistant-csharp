using System.Runtime.Serialization;

namespace BankingAssistant.Exceptions;

/// <summary>
/// Represents an item not found exception
/// </summary>
[Serializable]
public sealed class ItemNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ItemNotFoundException"/>
    /// </summary>
    public ItemNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ItemNotFoundException"/>
    /// </summary>
    public ItemNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ItemNotFoundException"/>
    /// </summary>
    public ItemNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

#pragma warning disable SYSLIB0051
    private ItemNotFoundException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#pragma warning restore SYSLIB0051
}
