namespace DanielWillett.SpeedBytes.Compression;

/// <summary>
/// Extension containing methods for <see cref="ByteReader"/> and <see cref="ByteWriter"/> that perform compression of integer arrays which have many zeros in sequence.
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

    /// <summary>
    /// Read a compressed unsigned 8-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static byte[] ReadZeroCompressedUInt8Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<byte>();
        byte[] output = new byte[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                byte next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadUInt8();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadUInt8();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed 8-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static sbyte[] ReadZeroCompressedInt8Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<sbyte>();
        sbyte[] output = new sbyte[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                sbyte next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadInt8();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadInt8();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed unsigned 16-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static ushort[] ReadZeroCompressedUInt16Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<ushort>();
        ushort[] output = new ushort[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                ushort next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadUInt16();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadUInt16();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed 16-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static short[] ReadZeroCompressedInt16Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<short>();
        short[] output = new short[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                short next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadInt16();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadInt16();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed 32-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static int[] ReadZeroCompressedInt32Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<int>();
        int[] output = new int[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                int next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadInt32();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadInt32();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed unsigned 32-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static uint[] ReadZeroCompressedUInt32Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<uint>();
        uint[] output = new uint[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                uint next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadUInt32();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadUInt32();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed unsigned 64-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static ulong[] ReadZeroCompressedUInt64Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<ulong>();
        ulong[] output = new ulong[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                ulong next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadUInt64();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadUInt64();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }

    /// <summary>
    /// Read a compressed 64-bit integer array which may have many zeros in sequence.
    /// </summary>
    /// <param name="long">Read length as an Int32 instead of UInt16.</param>
    /// <exception cref="ZeroCompressedFormatException">Data read from a <see cref="ByteReader"/> is invalid.</exception>
    public static long[] ReadZeroCompressedInt64Array(this ByteReader reader, bool @long = false)
    {
        int len = @long ? reader.ReadInt32() : reader.ReadUInt16();
        if (len == 0) return Array.Empty<long>();
        long[] output = new long[len];
        for (int i = 0; i < len; ++i)
        {
            byte b = reader.ReadUInt8();
            if (b == 255)
            {
                long next;
                for (int j = reader.ReadUInt8(); j > 0; --j)
                {
                    next = reader.ReadInt64();
                    if (i < len)
                        output[i] = next;
                    else
                        reader.FailZeroCompressed();
                    ++i;
                }
                next = reader.ReadInt64();
                if (i < len)
                    output[i] = next;
                else
                    reader.FailZeroCompressed();
            }
            else
            {
                i += b;
                if (i >= len)
                    reader.FailZeroCompressed();
            }
        }
        return output;
    }
}
