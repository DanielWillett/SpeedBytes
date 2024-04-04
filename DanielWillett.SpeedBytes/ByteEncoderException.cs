using System.Runtime.Serialization;

namespace DanielWillett.SpeedBytes;

/// <summary>
/// Base exepctions for <see cref="ByteReader"/> and <see cref="ByteWriter"/> errors.
/// </summary>
[Serializable]
public class ByteEncoderException : Exception
{
    /// <inheritdoc />
    public ByteEncoderException() { }
    /// <inheritdoc />
    public ByteEncoderException(string message) : base(message) { }
    /// <inheritdoc />
    public ByteEncoderException(string message, Exception inner) : base(message, inner) { }
    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected ByteEncoderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
