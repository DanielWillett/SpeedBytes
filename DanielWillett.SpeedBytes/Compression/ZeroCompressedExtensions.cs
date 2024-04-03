namespace DanielWillett.SpeedBytes.Compression;

/// <summary>
/// Extension for the compression of integer arrays which can have many zeros in sequence.
/// </summary>
public static class ZeroCompressedExtensions
{
    /// <summary>
    /// Write a compressed unsigned 8-bit integer span which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<byte> n, bool @long = false)
    {
        int len = n.Length;
        if (!@long)
        {
            writer.CheckArrayLength(typeof(byte), n.Length, ushort.MaxValue, out len);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            byte c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed 32-bit integer span which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<int> n, bool @long = false)
    {
        int len = n.Length;
        if (!@long)
        {
            writer.CheckArrayLength(typeof(int), n.Length, ushort.MaxValue, out len);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            int c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed unsigned 32-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<uint> n, bool @long = false)
    {
        int len = n.Length;
        if (!@long)
        {
            writer.CheckArrayLength(typeof(uint), n.Length, ushort.MaxValue, out len);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            uint c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed 8-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<sbyte> n, bool @long = false)
    {
        int len = n.Length;
        if (!@long)
        {
            writer.CheckArrayLength(typeof(sbyte), n.Length, ushort.MaxValue, out len);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            sbyte c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed 64-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<long> n, bool @long = false)
    {
        int len = n.Length;
        if (!@long)
        {
            len = Math.Min(len, ushort.MaxValue);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            long c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed unsigned 64-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<ulong> n, bool @long = false)
    {
        int len = n.Length;
        if (!@long)
        {
            len = Math.Min(len, ushort.MaxValue);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            ulong c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed 16-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<short> n, bool @long)
    {
        int len = n.Length;
        if (!@long)
        {
            len = Math.Min(len, ushort.MaxValue);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            short c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }

    /// <summary>
    /// Write a compressed unsigned 16-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <remarks>Max length when <paramref name="long"/> is <see langword="false"/>: 65535.</remarks>
    /// <param name="long">Use 32-bit integer length data instead of a 16-bit integer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements when <paramref name="long"/> is <see langword="false"/>.</exception>
    public static void WriteZeroCompressed(this ByteWriter writer, ReadOnlySpan<ushort> n, bool @long)
    {
        int len = n.Length;
        if (!@long)
        {
            len = Math.Min(len, ushort.MaxValue);
            writer.WriteInternal((ushort)len);
        }
        else
            writer.WriteInternal(len);

        int valuesWriting = 0;
        for (int i = 0; i < len; ++i)
        {
            ushort c = n[i];

            if (valuesWriting == 0)
            {
                if (c == 0)
                {
                    int ct = 0;
                    for (int j = i + 1; j < len; ++j)
                    {
                        if (n[j] == 0 && j - i <= 254 && j != len - 1)
                            continue;

                        ct = j - i - 1;
                        break;
                    }
                    writer.WriteInternal((byte)ct);
                    i += ct;
                    continue;
                }

                writer.WriteInternal((byte)255);
                valuesWriting = Math.Min(len - i, 255);
                for (int j = i + 1; j < len; ++j)
                {
                    if (j > len - 3)
                    {
                        valuesWriting = Math.Min(len - i, 255);
                        break;
                    }

                    if (j - i <= 254 && (n[j] != 0 || n[j + 1] != 0 || n[j + 2] != 0))
                        continue;

                    valuesWriting = j - i;
                    break;
                }

                writer.WriteInternal((byte)(valuesWriting - 1));
            }

            writer.WriteInternal(c);
            --valuesWriting;
        }
    }
}
