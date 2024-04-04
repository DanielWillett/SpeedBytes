using System.Diagnostics.Contracts;
using System.Globalization;

namespace DanielWillett.SpeedBytes.Formatting;

/// <summary>
/// Contains methods for turning spans of bytes or byte counts into string representations.
/// </summary>
public static class ByteFormatter
{
    private static string[]? _sizeCodes1024;
    private static double[]? _sizeIncrements1024;
    private static void SetSizeCodes()
    {
        _sizeCodes1024 =
        [
            Properties.Localization.SizeCodeB,
            Properties.Localization.SizeCodeKiB,
            Properties.Localization.SizeCodeMiB,
            Properties.Localization.SizeCodeGiB,
            Properties.Localization.SizeCodeTiB,
            Properties.Localization.SizeCodePiB,
            Properties.Localization.SizeCodeEiB
        ];

        _sizeIncrements1024 = new double[_sizeCodes1024.Length];
        for (int i = 0; i < _sizeCodes1024.Length; ++i)
            _sizeIncrements1024[i] = Math.Pow(1024, i);
    }

    /// <summary>
    /// Get the max length of formatted binary data to write using <see cref="FormatBinary(ReadOnlySpan{byte}, Span{char},ByteStringFormat)"/>
    /// </summary>
    [Pure]
    public static int GetMaxBinarySize(int byteCt, ByteStringFormat format)
    {
        AssertValidBinaryLoggingFormat(format, nameof(format));
        return GetBytesToStringMaxSizeIntl((uint)byteCt, format);
    }

    /// <summary>
    /// Convert binary data to a formatted display string.
    /// </summary>
    [Pure]
    public static string FormatBinary(ReadOnlySpan<byte> bytes, ByteStringFormat format)
    {
        AssertValidBinaryLoggingFormat(format, nameof(format));
        int ct = GetBytesToStringMaxSizeIntl((uint)bytes.Length, format);
        Span<char> span = ct > 384 ? new char[ct] : stackalloc char[ct];
        int len = FormatBinary(bytes, span, format);
#if NETFRAMEWORK
        return span.Slice(0, len).ToString();
#else
        return new string(span[..len]);
#endif
    }

    /// <summary>
    /// Convert binary data to a formatted display string and write it to a span.
    /// </summary>
    /// <returns>Number of characters written to <paramref name="output"/>.</returns>
    /// <remarks>Use <see cref="GetMaxBinarySize"/> to get the length of the span beforehand.</remarks>
    public static int FormatBinary(ReadOnlySpan<byte> bytes, Span<char> output, ByteStringFormat format)
    {
        GetBytesToStringMetrics((uint)bytes.Length, format, out int byteSize, out int columnCount, out int first, out int last);

        ReadOnlySpan<char> newLine = Environment.NewLine.AsSpan();

        int spanOffset = 0;

        int rowLblSize = 0;
        if ((format & ByteStringFormat.RowLabels) != 0)
        {
            rowLblSize = CountNibbles((uint)bytes.Length) + 3;
        }

        if ((format & ByteStringFormat.ColumnLabels) != 0)
        {
            if ((format & ByteStringFormat.NewLineAtBeginning) != 0)
            {
                newLine.CopyTo(output.Slice(spanOffset));
                spanOffset += newLine.Length;
            }

            if (rowLblSize != 0)
            {
                for (int i = 0; i < rowLblSize; ++i)
                    output[spanOffset + i] = ' ';
                spanOffset += rowLblSize;
            }

            for (int i = 0; i < columnCount; ++i)
            {
                if (i != 0)
                {
                    int c = byteSize - 2 + 1;
                    for (int j = 0; j < c; ++j)
                        output[spanOffset + j] = ' ';
                    spanOffset += c;
                }

                WriteByteBySize((byte)(i + 1), output.Slice(spanOffset), 2);
                spanOffset += 2;
            }
        }

        uint len;
        if (first == -1 && last == -1)
        {
            len = (uint)bytes.Length;

            for (int i = 0; i < len; ++i)
            {
                if (i % columnCount == 0)
                {
                    if (i != 0 || (format & ByteStringFormat.NewLineAtBeginning) != 0 || (format & ByteStringFormat.ColumnLabels) != 0)
                    {
                        newLine.CopyTo(output.Slice(spanOffset));
                        spanOffset += newLine.Length;
                    }

                    if ((format & ByteStringFormat.RowLabels) != 0)
                    {
                        output[spanOffset] = '0';
                        output[spanOffset + 1] = 'x';
                        WriteRowLabel((uint)i, output.Slice(spanOffset + 2), rowLblSize - 3);
                        spanOffset += rowLblSize;
                        output[spanOffset - 1] = ' ';
                    }
                }
                else
                {
                    output[spanOffset] = ' ';
                    ++spanOffset;
                }

                WriteByteBySize(bytes[i], output.Slice(spanOffset), byteSize);
                spanOffset += byteSize;
            }

            return spanOffset;
        }

        if (first == -1)
            first = 0;
        if (last == -1)
            last = 0;

        len = (uint)(first + last);
        int columnOffset = 0;
        for (uint i = 0; i <= len; ++i)
        {
            uint index;
            if (i >= (uint)first)
            {
                uint indexWithinLast = (uint)last - (i - (uint)first);
                index = (uint)bytes.Length - indexWithinLast - 1;
            }
            else
            {
                index = i;
            }
            if (i == (uint)first)
            {
                if (first != 0u || (format & ByteStringFormat.ColumnLabels) != 0)
                {
                    newLine.CopyTo(output.Slice(spanOffset));
                    spanOffset += newLine.Length;
                }
                output[spanOffset] = '.';
                output[spanOffset + 1] = '.';
                output[spanOffset + 2] = '.';
                output[spanOffset + 3] = ' ';
                output[spanOffset + 4] = '(';
                spanOffset += 5;
                uint ttlLen = (uint)bytes.Length - len;
                if ((format & ByteStringFormat.ByteCountUnits) != 0)
                {
                    int capacityCount = FormatCapacity(ttlLen, output.Slice(spanOffset));
                    output[spanOffset + capacityCount] = ')';
                    spanOffset += capacityCount + 1;
                }
                else
                {
#if NETFRAMEWORK
                    string byteCt = ttlLen.ToString("N0", CultureInfo.InvariantCulture);
                    byteCt.AsSpan().CopyTo(output.Slice(spanOffset));
                    int written = byteCt.Length;
#else
                    ttlLen.TryFormat(output[spanOffset..], out int written, "N0", CultureInfo.InvariantCulture);
#endif
                    spanOffset += written;
                    output[spanOffset] = ' ';
                    output[spanOffset + 1] = 'B';
                    output[spanOffset + 2] = ')';
                    spanOffset += 3;
                }
                if (last != 0 && (index + 1) % columnCount != 0)
                {
                    newLine.CopyTo(output.Slice(spanOffset));
                    spanOffset += newLine.Length;
                    if ((format & ByteStringFormat.RowLabels) != 0)
                    {
                        output[spanOffset] = '0';
                        output[spanOffset + 1] = 'x';
                        WriteRowLabel(index, output.Slice(spanOffset + 2), rowLblSize - 3);
                        spanOffset += rowLblSize;
                        output[spanOffset - 1] = ' ';
                    }
                    columnOffset = (((int)index + 1) - ((int)index + 1) / columnCount * columnCount);
                }
                continue;
            }

            if (index % columnCount == 0)
            {
                if (index != 0 || (format & ByteStringFormat.NewLineAtBeginning) != 0 || (format & ByteStringFormat.ColumnLabels) != 0)
                {
                    newLine.CopyTo(output.Slice(spanOffset));
                    spanOffset += newLine.Length;
                }

                if ((format & ByteStringFormat.RowLabels) != 0)
                {
                    output[spanOffset] = '0';
                    output[spanOffset + 1] = 'x';
                    WriteRowLabel(index, output.Slice(spanOffset + 2), rowLblSize - 3);
                    spanOffset += rowLblSize;
                    output[spanOffset - 1] = ' ';
                }
            }
            else
            {
                output[spanOffset] = ' ';
                ++spanOffset;
            }

            if (columnOffset > 0)
            {
                int c = columnOffset * (byteSize + 1) - 1;
                for (int j = 0; j < c; ++j)
                    output[spanOffset + j] = ' ';

                spanOffset += c;
                columnOffset = 0;
            }

            WriteByteBySize(bytes[(int)index], output.Slice(spanOffset), byteSize);
            spanOffset += byteSize;
        }

        return spanOffset;
    }
    private static int GetBytesToStringMaxSizeIntl(uint byteCt, ByteStringFormat format)
    {
        GetBytesToStringMetrics(byteCt, format, out int byteSize, out int columnCount, out int first, out int last);

        int newLineLen = Environment.NewLine.Length;

        int size = 0;

        int rowLblSize = 0;
        if ((format & ByteStringFormat.RowLabels) != 0)
        {
            rowLblSize = CountNibbles(byteCt) + 3;
        }

        if ((format & ByteStringFormat.ColumnLabels) != 0)
        {
            if ((format & ByteStringFormat.NewLineAtBeginning) != 0)
                size += newLineLen;

            if (rowLblSize != 0)
                size += rowLblSize;

            size += (columnCount - 1) * (byteSize + 1) + 2;
        }

        if (first == -1 && last == -1)
        {
            uint remBytes = byteCt % (uint)columnCount;
            int rowCount = (int)(remBytes == 0u ? byteCt / (uint)columnCount : (byteCt / (uint)columnCount + 1u));
            size += (rowCount - ((format & ByteStringFormat.NewLineAtBeginning) != 0 || (format & ByteStringFormat.ColumnLabels) != 0 ? 0 : 1)) * newLineLen;
            if (rowLblSize != 0)
                size += rowCount * rowLblSize;

            if (remBytes == 0)
                size += rowCount * (columnCount - 1 + byteSize * columnCount);
            else
            {
                size += (rowCount - 1) * (columnCount - 1 + byteSize * columnCount);
                size += (int)(remBytes - 1 + byteSize * remBytes);
            }

            return size;
        }

        if (first == -1)
            first = 0;
        if (last == -1)
            last = 0;

        uint len = (uint)(first + last);

        if ((format & ByteStringFormat.NewLineAtBeginning) != 0 || (format & ByteStringFormat.ColumnLabels) != 0)
            size += newLineLen;

        int firstRowCount = 0, lastRowCount = 0;

        if (first != 0)
        {
            int firstRemBytes = first % columnCount;
            firstRowCount = (int)(firstRemBytes == 0 ? first / (uint)columnCount : (first / (uint)columnCount + 1u));

            if (firstRemBytes == 0)
                size += firstRowCount * (columnCount - 1 + byteSize * columnCount);
            else
            {
                size += (firstRowCount - 1) * (columnCount - 1 + byteSize * columnCount);
                size += firstRemBytes - 1 + byteSize * firstRemBytes;
            }
            size += newLineLen;
        }

        if ((format & ByteStringFormat.ByteCountUnits) != 0)
            size += 6 + GetCapacityLength(byteCt - len);
        else
            size += 8 + CountDigits(byteCt - len, commas: true);

        if (last != 0)
        {
            size += newLineLen;
            uint index = byteCt - (uint)last;

            uint lastRemBytes = index % (uint)columnCount;
            lastRowCount = lastRemBytes == 0u ? last / columnCount : (last / columnCount + 1);

            if (lastRemBytes == 0 || lastRowCount == 1)
                size += lastRowCount * (columnCount - 1 + byteSize * columnCount);
            else
            {
                size += (lastRowCount - 1) * (columnCount - 1 + byteSize * columnCount);
                size += (int)lastRemBytes - 1 + byteSize * (int)lastRemBytes;
            }

            if (lastRowCount == 1 && lastRemBytes != 0)
            {
                lastRemBytes = (byteCt - (index + ((uint)columnCount - lastRemBytes))) % (uint)columnCount;
                if (lastRemBytes != 0)
                {
                    size += (int)lastRemBytes - 1 + byteSize * (int)lastRemBytes + newLineLen;
                    if (rowLblSize != 0)
                        size += rowLblSize;
                }
            }
        }

        if (rowLblSize != 0)
        {
            size += firstRowCount * rowLblSize;
            size += lastRowCount * rowLblSize;
        }

        if (firstRowCount > 1)
            size += (firstRowCount - 1) * newLineLen;

        if (lastRowCount > 1)
            size += (lastRowCount - 1) * newLineLen;

        return size;
    }
    private static void WriteRowLabel(uint rowIndex, Span<char> span, int maxNiblLength = 8)
    {
        uint nibl = rowIndex & 0x0F;
        span[maxNiblLength - 1] = nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48);
        nibl = (rowIndex & 0xF0) >> 4;
        if (maxNiblLength == 1)
            return;
        span[maxNiblLength - 2] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
        nibl = (rowIndex & 0x0F00) >> 8;
        if (maxNiblLength == 2)
            return;
        span[maxNiblLength - 3] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
        nibl = (rowIndex & 0xF000) >> 12;
        if (maxNiblLength == 3)
            return;
        span[maxNiblLength - 4] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
        nibl = (rowIndex & 0x0F0000) >> 16;
        if (maxNiblLength == 4)
            return;
        span[maxNiblLength - 5] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
        nibl = (rowIndex & 0xF00000) >> 20;
        if (maxNiblLength == 5)
            return;
        span[maxNiblLength - 6] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
        nibl = (rowIndex & 0x0F000000) >> 24;
        if (maxNiblLength == 6)
            return;
        span[maxNiblLength - 7] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
        nibl = (rowIndex & 0xF0000000) >> 28;
        if (maxNiblLength == 7)
            return;
        span[maxNiblLength - 8] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';
    }

    /// <summary>
    /// Counts the amount of characters that will be returned by <see cref="FormatCapacity(long,Span{char},int)"/>
    /// </summary>
    public static int GetCapacityLength(long length, int decimals = 1)
    {
        if (_sizeCodes1024 == null)
            SetSizeCodes();

        if (decimals == 0)
            decimals = -1;

        if (length == 0)
            return 2 + decimals;

        bool neg = length < 0;
        length = Math.Abs(length);

        double incr = Math.Log(length, 1024);
        int inc;
        if (incr % 1 > 0.8)
            inc = (int)Math.Ceiling(incr);
        else
            inc = (int)Math.Floor(incr);

        if (inc >= _sizeIncrements1024!.Length)
            inc = _sizeIncrements1024.Length - 1;

        double len = length / _sizeIncrements1024[inc];
        if (neg) len = -len;

        return (neg ? 1 : 0) + CountDigits((long)Math.Floor(len), commas: true) + 2 + decimals + _sizeCodes1024![inc].Length;
    }

    /// <summary>
    /// Format a byte size into a display string. Use <see cref="GetCapacityLength"/> to get the length of <paramref name="output"/>.
    /// </summary>
    /// <returns>Number of characters written to <paramref name="output"/>.</returns>
    public static int FormatCapacity(long length, Span<char> output, int decimals = 1)
    {
        if (_sizeCodes1024 == null)
            SetSizeCodes();

#if NETFRAMEWORK
        if (length == 0)
        {
            output[0] = '0';
            if (decimals > 0)
            {
                output[1] = '.';
                for (int i = 0; i < decimals; ++i)
                    output[i + 2] = '0';
            }

            return decimals == 0 ? 1 : (decimals + 2);
        }

        string format = "N" + decimals.ToString(CultureInfo.InvariantCulture);
#else
        Span<char> format = stackalloc char[1 + CountDigits(decimals)];
        format[0] = 'N';
        decimals.TryFormat(format[1..], out _, provider: CultureInfo.InvariantCulture);
        int written;
        if (length == 0)
        {
            0.TryFormat(output, out written, format, CultureInfo.CurrentCulture);
            return written;
        }
#endif

        bool neg = length < 0;
        length = Math.Abs(length);

        double incr = Math.Log(length, 1024);
        int inc;
        if (incr % 1 > 0.8)
            inc = (int)Math.Ceiling(incr);
        else
            inc = (int)Math.Floor(incr);

        if (inc >= _sizeIncrements1024!.Length)
            inc = _sizeIncrements1024.Length - 1;

        double len = length / _sizeIncrements1024[inc];
        if (neg) len = -len;

#if NETFRAMEWORK
        string lenStr = len.ToString(format, CultureInfo.CurrentCulture);
        int written = lenStr.Length;
        lenStr.AsSpan().CopyTo(output);
#else
        len.TryFormat(output, out written, format, CultureInfo.CurrentCulture);
#endif
        output[written] = ' ';
        ReadOnlySpan<char> sizeCode = _sizeCodes1024![inc].AsSpan();
        sizeCode.CopyTo(output.Slice(written + 1));
        return written + 1 + sizeCode.Length;
    }

    /// <summary>
    /// Format a byte size into a display string.
    /// </summary>
    [Pure]
    public static string FormatCapacity(long length, int decimals = 1)
    {
        int len = GetCapacityLength(length, decimals);
        Span<char> newStr = stackalloc char[len];
#if NETFRAMEWORK
        return newStr.ToString();
#else
        return new string(newStr);
#endif
    }

    /// <summary>
    /// Writes one zero-padded byte to a span in either base 16, base 10, or base 2.
    /// </summary>
    /// <remarks>Base 16 will write 2 characters, base 10 will write 3 characters, and base 2 will write 8 characters.</remarks>
    /// <returns>Number of characters written to <paramref name="span"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Not a supported radix.</exception>
    public static int WriteByteByRadix(byte value, Span<char> span, int radix)
    {
        if (radix is not 16 and not 10 and not 2)
            throw new ArgumentOutOfRangeException(nameof(radix), Properties.Localization.ByteRadixShouldBeValidBase);

        int byteSize = radix switch
        {
            16 => 2,
            10 => 3,
            _ => 8
        };
        WriteByteBySize(value, span, byteSize);
        return byteSize;
    }

    /// <summary>
    /// Writes one zero-padded byte to a span in either base 16 (<paramref name="byteSize"/> = 2), base 10 (<paramref name="byteSize"/> = 3), or base 2 (<paramref name="byteSize"/> = 8).
    /// </summary>
    /// <returns>Number of characters written to <paramref name="span"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Not a supported byte size.</exception>
    public static void WriteByteBySize(byte value, Span<char> span, int byteSize)
    {
        switch (byteSize)
        {
            case 3:
                span[2] = (char)(value % 10 + 48);
                if (value > 9)
                {
                    span[0] = value > 99 ? (char)(value / 100 + 48) : '0';
                    span[1] = (char)(value % 100 / 10 + 48);
                }
                else
                {
                    span[0] = '0';
                    span[1] = '0';
                }

                break;

            case 2:
                int nibl = value % 16;
                span[1] = nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48);
                nibl = value / 16;
                span[0] = nibl > 0 ? nibl > 9 ? (char)(nibl + 55) : (char)(nibl + 48) : '0';

                break;

            case 8:
                span[0] = (char)(((value & 128) == 0 ? 0 : 1) + 48);
                span[1] = (char)(((value & 64) == 0 ? 0 : 1) + 48);
                span[2] = (char)(((value & 32) == 0 ? 0 : 1) + 48);
                span[3] = (char)(((value & 16) == 0 ? 0 : 1) + 48);
                span[4] = (char)(((value & 8) == 0 ? 0 : 1) + 48);
                span[5] = (char)(((value & 4) == 0 ? 0 : 1) + 48);
                span[6] = (char)(((value & 2) == 0 ? 0 : 1) + 48);
                span[7] = (char)(((value & 1) == 0 ? 0 : 1) + 48);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(byteSize), Properties.Localization.ByteSizeShouldBeValidBase);
        }
    }
    internal static void AssertValidBinaryLoggingFormat(ByteStringFormat format, string parameterName)
    {
        if (format == 0)
            return;

        int overlapCt = 0;
        if ((format & ByteStringFormat.Base10) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Base16) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Base2) != 0)
            ++overlapCt;

        if (overlapCt > 1)
            throw new ArgumentException(Properties.Localization.BinaryStringMustHaveOneBase, parameterName);

        overlapCt = 0;
        if ((format & ByteStringFormat.Columns8) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Columns16) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Columns32) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Columns64) != 0)
            ++overlapCt;

        if (overlapCt > 1)
            throw new ArgumentException(Properties.Localization.BinaryStringMustHaveOneColumn, parameterName);

        overlapCt = 0;
        if ((format & ByteStringFormat.First8) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.First16) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.First32) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.First64) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.First128) != 0)
            ++overlapCt;

        if (overlapCt > 1)
            throw new ArgumentException(Properties.Localization.BinaryStringMustHaveOneFirst, parameterName);

        overlapCt = 0;
        if ((format & ByteStringFormat.Last8) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Last16) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Last32) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Last64) != 0)
            ++overlapCt;
        if ((format & ByteStringFormat.Last128) != 0)
            ++overlapCt;

        if (overlapCt > 1)
            throw new ArgumentException(Properties.Localization.BinaryStringMustHaveOneLast, parameterName);
    }
    private static void GetBytesToStringMetrics(uint byteCt, ByteStringFormat format, out int byteSize, out int columnCount, out int first, out int last)
    {
        byteSize = (format & ByteStringFormat.Base10) == 0
            ? (format & ByteStringFormat.Base2) != 0
                ? 8
                : 2
            : 3;

        columnCount = (format & ByteStringFormat.Columns8) == 0
            ? (format & ByteStringFormat.Columns32) == 0
                ? (format & ByteStringFormat.Columns16) == 0
                    ? (format & ByteStringFormat.Columns64) == 0
                        ? byteSize switch
                        {
                            3 => 16,
                            8 => 8,
                            _ => 32
                        }
                        : 64
                    : 16
                : 32
            : 8;

        if ((format & ByteStringFormat.First8) != 0)
            first = 8;
        else if ((format & ByteStringFormat.First16) != 0)
            first = 16;
        else if ((format & ByteStringFormat.First32) != 0)
            first = 32;
        else if ((format & ByteStringFormat.First64) != 0)
            first = 64;
        else if ((format & ByteStringFormat.First128) != 0)
            first = 128;
        else
            first = -1;

        if ((format & ByteStringFormat.Last8) != 0)
            last = 8;
        else if ((format & ByteStringFormat.Last16) != 0)
            last = 16;
        else if ((format & ByteStringFormat.Last32) != 0)
            last = 32;
        else if ((format & ByteStringFormat.Last64) != 0)
            last = 64;
        else if ((format & ByteStringFormat.Last128) != 0)
            last = 128;
        else
            last = -1;

        if (last != -1 && first != -1)
        {
            if (last + first <= byteCt)
                return;

            last = -1; first = -1;
        }
        else if (last > byteCt)
            last = -1;
        else if (first > byteCt)
            first = -1;
    }
    private static int CountNibbles(uint num)
    {
        return num switch
        {
            <= 16u => 1,
            <= 256u => 2,
            <= 4096u => 3,
            <= 65536u => 4,
            <= 1048576u => 5,
            <= 16777216u => 6,
            <= 268435456u => 7,
            _ => 8
        };
    }

    private static int CountDigits(uint num, bool commas = false)
    {
        int c = num switch
        {
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            _ => 10
        };
        if (commas)
            c += (c - 1) / 3;
        return c;
    }

    private static int CountDigits(long num, bool commas = false) => CountDigits((ulong)Math.Abs(num), commas);
    private static int CountDigits(ulong num, bool commas = false)
    {
        int c = num switch
        {
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            <= 9999999999 => 10,
            <= 99999999999 => 11,
            <= 999999999999 => 12,
            <= 9999999999999 => 13,
            <= 99999999999999 => 14,
            <= 999999999999999 => 15,
            <= 9999999999999999 => 16,
            <= 99999999999999999 => 17,
            <= 999999999999999999 => 18,
            <= 9999999999999999999 => 19,
            _ => 20
        };
        if (commas)
            c += (c - 1) / 3;
        return c;
    }
}
