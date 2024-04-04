using System.Runtime.Serialization;

namespace DanielWillett.SpeedBytes.Compression;
/// <summary>
/// Thrown when a read method from <see cref="ZeroCompressedExtensions"/> gets data with an invalid format.
/// </summary>
[Serializable]
public class ZeroCompressedFormatException : ByteEncoderException
{
    /// <inheritdoc />
    public ZeroCompressedFormatException() : base(Properties.Localization.ZeroCompressedFormatException) { }
    /// <inheritdoc />
    public ZeroCompressedFormatException(string message) : base(message) { }
    /// <inheritdoc />
    public ZeroCompressedFormatException(string message, Exception inner) : base(message, inner) { }
    /// <inheritdoc />
    protected ZeroCompressedFormatException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
