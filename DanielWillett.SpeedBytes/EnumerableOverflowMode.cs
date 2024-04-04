namespace DanielWillett.SpeedBytes;

/// <summary>
/// Defines the behavior of a <see cref="ByteWriter"/> when it encounters an array, string, or other enumerable too long to write.
/// </summary>
public enum EnumerableOverflowMode
{
    /// <summary>
    /// Throw an <see cref="ArgumentOutOfRangeException"/> when a passed enumerable is too long.
    /// </summary>
    Throw,

    /// <summary>
    /// Log the error using <see cref="ByteWriter.OnLog"/> and write a zero-length enumerable.
    /// </summary>
    LogAndWriteEmpty,

    /// <summary>
    /// Truncate the enumerable if it is too long to the max length.
    /// </summary>
    Truncate
}