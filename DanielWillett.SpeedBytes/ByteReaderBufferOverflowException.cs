using System.Runtime.Serialization;

namespace DanielWillett.SpeedBytes;

/// <summary>
/// Error thrown when a <see cref="ByteReader"/> tries to read past it's available data.
/// </summary>
[Serializable]
public class ByteBufferOverflowException : ByteEncoderException
{
    /// <inheritdoc />
    public ByteBufferOverflowException() : base(Properties.Localization.ByteBufferOverflowException) { }

    /// <inheritdoc />
    public ByteBufferOverflowException(string message) : base(message) { }

    /// <inheritdoc />
    public ByteBufferOverflowException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
    protected ByteBufferOverflowException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}