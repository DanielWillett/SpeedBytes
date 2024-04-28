using System.Collections;
using DanielWillett.SpeedBytes.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DanielWillett.SpeedBytes;

/// <summary>
/// Fast decoding from a byte array of data. Also works with <see cref="System.IO.Stream"/>s.
/// </summary>
public class ByteReader
{
    private const int MinStreamBufferSize = 32;
    private const int GuidSize = 16;

    private static readonly bool IsBigEndian = !BitConverter.IsLittleEndian;
    private static Dictionary<Type, MethodInfo>? _nonNullableReaders;
    private static Dictionary<Type, MethodInfo>? _nullableReaders;
    private static readonly MethodInfo? ReadEnumMethod = typeof(ByteReader).GetMethod(nameof(ReadEnum), BindingFlags.Instance | BindingFlags.Public);
    private static readonly MethodInfo? ReadNullableEnumMethod = typeof(ByteReader).GetMethod(nameof(ReadNullableEnum), BindingFlags.Instance | BindingFlags.Public);

    private byte[]? _buffer;
    private Stream? _stream;
    private int _index;
    private bool _hasFailed;
    private bool _streamMode;
    private bool _streamLengthSupport;
    private int _position;
    private int _length;

    /// <summary>
    /// Invoked when <see cref="LogOnError"/> is <see langword="true"/>. Defaults to <see cref="Console.WriteLine(string)"/>.
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// Internal buffer. In stream mode not very useful, but in buffer mode can be used to get the data without allocating a new array. Use <see cref="Position"/> for the length.
    /// </summary>
    public byte[]? InternalBuffer { get => _buffer; set => _buffer = value; }

    /// <summary>
    /// Stream to read from. Setter does not reset the buffer, recommnded to use <see cref="LoadNew(System.IO.Stream)"/> instead.
    /// </summary>
    /// <remarks>Stream mode is still in beta.</remarks>
    public Stream? Stream
    {
        get => _stream;
        private set
        {
            if (value is not null && !value.CanRead)
                throw new ArgumentException(Properties.Localization.GivenStreamCanNotRead, nameof(value));
            _stream = value;
        }
    }

    /// <summary>
    /// If the reader has failed yet. Useful if <see cref="ThrowOnError"/> is set to <see langword="false"/>.
    /// </summary>
    public bool HasFailed => _hasFailed;

    /// <summary>
    /// Index of the read. In buffer mode it represents the length of data in the buffer, in stream mode it represents the position of the stream minus what hasn't been read from the buffer.
    /// </summary>
    public int Position => _streamMode ? (_position - (_length - _index)) : _index;

    // ReSharper disable once MergeConditionalExpression (pretty sure this things wrong here idk)
    /// <summary>
    /// Number of bytes left in the stream or buffer.
    /// </summary>
    /// <remarks>If the stream is unable to return a length, the amount of bytes left in the buffer is returned instead.</remarks>
    public int BytesLeft
    {
        get
        {
            if (_buffer == null)
                return 0;

            if (!_streamMode)
                return _buffer.Length - _index;

            if (_streamLengthSupport)
                return (int)Math.Min(_stream!.Length - _stream!.Position, int.MaxValue);

            return _buffer.Length - _index;
        }
    }

    /// <summary>
    /// Allocated length the current buffer.
    /// </summary>
    /// <remarks>This may not always be equal to the length of the buffer if <see cref="LoadNew(ArraySegment{byte})"/> is used to initilize the reader.</remarks>
    public int Length => _length;

    /// <summary>
    /// When <see langword="true"/>, will throw an exception when there's a read failure.
    /// Otherwise it will just set <see cref="HasFailed"/> to <see langword="true"/> and log an error if <see cref="LogOnError"/> is <see langword="true"/>.
    /// </summary>
    public bool ThrowOnError { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, will log a warning when there's a read failure. Otherwise it will just set <see cref="HasFailed"/> to <see langword="true"/> and throw an error if <see cref="ThrowOnError"/> is <see langword="true"/>.
    /// </summary>
    public bool LogOnError { get; set; } = false;

    /// <summary>
    /// Influences the size of the buffer in stream mode (how much is read at once).
    /// </summary>
    public int StreamBufferSize { get; set; } = 128;

    /// <summary>
    /// An array segment of the current read buffer.
    /// </summary>
    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public ArraySegment<byte> Data
    {
        get
        {
            if (_streamMode)
                throw new NotSupportedException(Properties.Localization.ByteReaderArraySegmentStreamModeNotSupported);

            return new ArraySegment<byte>(_buffer ?? Array.Empty<byte>(), _index, _buffer == null ? 0 : Length - _index);
        }
    }

    private static void PrepareMethods()
    {
        _nonNullableReaders ??= new Dictionary<Type, MethodInfo>(45)
        {
            { typeof(int), GetMethod(nameof(ReadInt32)) },
            { typeof(uint), GetMethod(nameof(ReadUInt32)) },
            { typeof(byte), GetMethod(nameof(ReadUInt8)) },
            { typeof(sbyte), GetMethod(nameof(ReadInt8)) },
            { typeof(bool), GetMethod(nameof(ReadBool)) },
            { typeof(long), GetMethod(nameof(ReadInt64)) },
            { typeof(ulong), GetMethod(nameof(ReadUInt64)) },
            { typeof(short), GetMethod(nameof(ReadInt16)) },
            { typeof(ushort), GetMethod(nameof(ReadUInt16)) },
            { typeof(float), GetMethod(nameof(ReadFloat)) },
            { typeof(decimal), GetMethod(nameof(ReadDecimal)) },
            { typeof(double), GetMethod(nameof(ReadDouble)) },
            { typeof(char), GetMethod(nameof(ReadChar)) },
            { typeof(string), GetMethod(nameof(ReadString)) },
#if NET5_0_OR_GREATER
            { typeof(Half), GetMethod(nameof(ReadHalf)) },
#endif
            { typeof(Type), typeof(ByteReader)
                .GetMethod(nameof(ReadType), BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null)
                ?? throw new ByteEncoderException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, "ReadType"))
            },
            { typeof(Type[]), GetMethod(nameof(ReadTypeArray)) },
            { typeof(DateTime), GetMethod(nameof(ReadDateTime)) },
            { typeof(DateTimeOffset), GetMethod(nameof(ReadDateTimeOffset)) },
            { typeof(TimeSpan), GetMethod(nameof(ReadTimeSpan)) },
            { typeof(Guid), GetMethod(nameof(ReadGuid)) },
            { typeof(Guid[]), GetMethod(nameof(ReadGuidArray)) },
            { typeof(DateTime[]), GetMethod(nameof(ReadDateTimeArray)) },
            { typeof(DateTimeOffset[]), GetMethod(nameof(ReadDateTimeOffsetArray)) },
            { typeof(byte[]), GetMethod(nameof(ReadUInt8Array)) },
            { typeof(sbyte[]), GetMethod(nameof(ReadInt8Array)) },
            { typeof(int[]), GetMethod(nameof(ReadInt32Array)) },
            { typeof(uint[]), GetMethod(nameof(ReadUInt32Array)) },
            { typeof(bool[]), GetMethod(nameof(ReadBoolArray)) },
            { typeof(long[]), GetMethod(nameof(ReadInt64Array)) },
            { typeof(ulong[]), GetMethod(nameof(ReadUInt64Array)) },
            { typeof(short[]), GetMethod(nameof(ReadInt16Array)) },
            { typeof(ushort[]), GetMethod(nameof(ReadUInt16Array)) },
            { typeof(float[]), GetMethod(nameof(ReadFloatArray)) },
            { typeof(double[]), GetMethod(nameof(ReadDoubleArray)) },
            { typeof(decimal[]), GetMethod(nameof(ReadDecimalArray)) },
            { typeof(char[]), GetMethod(nameof(ReadCharArray)) },
            { typeof(string[]), GetMethod(nameof(ReadStringArray)) }
        };

        _nullableReaders ??= new Dictionary<Type, MethodInfo>(44)
        {
            { typeof(int?), GetMethod(nameof(ReadNullableInt32)) },
            { typeof(uint?), GetMethod(nameof(ReadNullableUInt32)) },
            { typeof(byte?), GetMethod(nameof(ReadNullableUInt8)) },
            { typeof(sbyte?), GetMethod(nameof(ReadNullableInt8)) },
            { typeof(bool?), GetMethod(nameof(ReadNullableBool)) },
            { typeof(long?), GetMethod(nameof(ReadNullableInt64)) },
            { typeof(ulong?), GetMethod(nameof(ReadNullableUInt64)) },
            { typeof(short?), GetMethod(nameof(ReadNullableInt16)) },
            { typeof(ushort?), GetMethod(nameof(ReadNullableUInt16)) },
            { typeof(float?), GetMethod(nameof(ReadNullableFloat)) },
            { typeof(decimal?), GetMethod(nameof(ReadNullableDecimal)) },
            { typeof(double?), GetMethod(nameof(ReadNullableDouble)) },
            { typeof(char?), GetMethod(nameof(ReadNullableChar)) },
            { typeof(string), GetMethod(nameof(ReadNullableString)) },
#if NET5_0_OR_GREATER
            { typeof(Half?), GetMethod(nameof(ReadNullableHalf)) },
#endif
            { typeof(Type), typeof(ByteReader)
                .GetMethod(nameof(ReadType), BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null)
                ?? throw new ByteEncoderException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, "ReadType"))
            },
            { typeof(Type[]), GetMethod(nameof(ReadTypeArray)) },
            { typeof(DateTime?), GetMethod(nameof(ReadNullableDateTime)) },
            { typeof(DateTimeOffset?), GetMethod(nameof(ReadNullableDateTimeOffset)) },
            { typeof(TimeSpan?), GetMethod(nameof(ReadNullableTimeSpan)) },
            { typeof(Guid?), GetMethod(nameof(ReadNullableGuid)) },
            { typeof(Guid[]), GetMethod(nameof(ReadNullableGuidArray)) },
            { typeof(DateTime[]), GetMethod(nameof(ReadNullableDateTimeArray)) },
            { typeof(DateTimeOffset[]), GetMethod(nameof(ReadNullableDateTimeOffsetArray)) },
            { typeof(byte[]), GetMethod(nameof(ReadNullableUInt8Array)) },
            { typeof(sbyte[]), GetMethod(nameof(ReadNullableInt8Array)) },
            { typeof(int[]), GetMethod(nameof(ReadNullableInt32Array)) },
            { typeof(uint[]), GetMethod(nameof(ReadNullableUInt32Array)) },
            { typeof(bool[]), GetMethod(nameof(ReadNullableBoolArray)) },
            { typeof(long[]), GetMethod(nameof(ReadNullableInt64Array)) },
            { typeof(ulong[]), GetMethod(nameof(ReadNullableUInt64Array)) },
            { typeof(short[]), GetMethod(nameof(ReadNullableInt16Array)) },
            { typeof(ushort[]), GetMethod(nameof(ReadNullableUInt16Array)) },
            { typeof(float[]), GetMethod(nameof(ReadNullableFloatArray)) },
            { typeof(double[]), GetMethod(nameof(ReadNullableDoubleArray)) },
            { typeof(decimal[]), GetMethod(nameof(ReadNullableDecimalArray)) },
            { typeof(char[]), GetMethod(nameof(ReadNullableCharArray)) },
            { typeof(string[]), GetMethod(nameof(ReadNullableStringArray)) }
        };

        MethodInfo GetMethod(string name) => typeof(ByteReader).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
                                             ?? throw new ByteEncoderException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, name));
    }

    internal static void AddReaderMethod<T>(Reader<T> reader)
    {
        if (_nonNullableReaders == null)
            PrepareMethods();
        _nonNullableReaders!.Add(typeof(T), reader.Method);
    }
    internal static void AddNullableReaderStructMethod<T>(Reader<T?> reader) where T : struct
    {
        if (_nullableReaders == null)
            PrepareMethods();
        _nullableReaders!.Add(typeof(T?), reader.Method);
    }
    internal static void AddNullableReaderClassMethod<T>(Reader<T?> reader) where T : class
    {
        if (_nullableReaders == null)
            PrepareMethods();
        _nullableReaders!.Add(typeof(T), reader.Method);
    }
    internal void Log(string msg)
    {
        if (OnLog == null)
            Console.WriteLine(msg);
        else
            OnLog.Invoke(msg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetStreamBufferLength() => !_streamLengthSupport
        ? StreamBufferSize
        : (int)Math.Min(StreamBufferSize, Math.Max(_stream!.Length - _stream.Position, MinStreamBufferSize));

    internal void FailZeroCompressed()
    {
        _hasFailed = true;
        if (LogOnError)
            Log(Properties.Localization.ZeroCompressedFormatException);
        if (ThrowOnError)
            throw new ZeroCompressedFormatException();
    }
    private void Overflow(Type readType, int size)
    {
        _hasFailed = true;
        string message = string.Format(Properties.Localization.ByteReaderOverflow, size, _index, _length, BytesLeft, readType.Name);
        if (LogOnError)
            Log(message);
        if (ThrowOnError)
            throw new ByteBufferOverflowException(message);
    }
    private void Overflow(string readType, int size)
    {
        _hasFailed = true;
        string message = string.Format(Properties.Localization.ByteReaderOverflow, size, _index, _length, BytesLeft, readType);
        if (LogOnError)
            Log(message);
        if (ThrowOnError)
            throw new ByteBufferOverflowException(message);
    }

    /// <summary>
    /// Loads a <see cref="System.IO.Stream"/> to be read from. Stream must be able to read.
    /// </summary>
    /// <remarks>Seek the stream to where you want to start before passing it here.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void LoadNew(Stream stream)
    {
        _hasFailed = false;
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!_stream!.CanSeek)
        {
            _streamLengthSupport = false;
            if (_buffer is null || _buffer.Length < StreamBufferSize)
                _buffer = new byte[StreamBufferSize];
            else
                Unsafe.InitBlock(ref _buffer[0], 0, (uint)_buffer.Length);
        }
        else
        {
            _ = stream.Length;
            _ = stream.Position;
            _streamLengthSupport = true;
            int min = GetStreamBufferLength();
            if (_buffer is not { Length: > 0 })
            {
                _buffer = new byte[min];
            }
            else if (_buffer.Length > min)
            {
                Unsafe.InitBlock(ref _buffer[0], 0, (uint)_buffer.Length);
            }
            else
            {
                _buffer = new byte[min];
            }
        }

        _length = 0;
        _position = 0;
        _index = 0;
        _streamMode = true;
    }

    /// <summary>
    /// Loads a new byte array to be read from with an offset.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public void LoadNew(byte[] bytes, int index = 0)
    {
        if (index > bytes.Length || bytes.Length != 0 && index == bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (index < 0)
            index = 0;

        LoadNew(new ArraySegment<byte>(bytes, index, bytes.Length - index));
    }

    /// <summary>
    /// Loads a new byte array to be read from.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public void LoadNew(ArraySegment<byte> bytes)
    {
        _buffer = bytes.Array ?? throw new ArgumentNullException(nameof(bytes));
        _length = bytes.Offset + bytes.Count;
        _streamMode = false;
        _index = bytes.Offset;
        _position = bytes.Offset;
        _hasFailed = false;
    }
    private static unsafe void Reverse(byte* litEndStrt, byte* stack, int size)
    {
        Unsafe.CopyBlock(stack, litEndStrt, (uint)size);
        for (int i = 0; i < size; i++)
            litEndStrt[i] = stack[size - i - 1];
    }
    private static unsafe void Reverse(byte[] buffer, int index, byte* stack, int size)
    {
        for (int i = 0; i < size; i++)
            stack[size - i - 1] = buffer[index +i];
    }

    private unsafe T Read<T>() where T : unmanaged
    {
        T rtn;
        int size = sizeof(T);
        if (!IsBigEndian || size == 1)
        {
            rtn = Unsafe.ReadUnaligned<T>(ref _buffer![_index]);
        }
        else
        {
            byte* ptr = stackalloc byte[size];
            Unsafe.CopyBlock(ref Unsafe.AsRef<byte>(ptr), ref _buffer![_index], (uint)size);
            Reverse(_buffer, _index, ptr, size);
            rtn = Unsafe.ReadUnaligned<T>(ptr);
        }

        _index += size;
        return rtn;
    }
    private static unsafe T ReadFromBuffer<T>(byte[] buffer, int index) where T : unmanaged
    {
        T rtn;
        int size = sizeof(T);
        if (!IsBigEndian || size == 1)
        {
            rtn = Unsafe.ReadUnaligned<T>(ref buffer[index]);
        }
        else
        {
            byte* ptr = stackalloc byte[size];
            Reverse(buffer, index, ptr, size);
            rtn = Unsafe.ReadUnaligned<T>(ptr);
        }

        return rtn;
    }
    private static unsafe T ReadFromBuffer<T>(byte* bufferPtr) where T : unmanaged
    {
        T rtn;
        int size = sizeof(T);
        if (!IsBigEndian || size == 1)
        {
            rtn = Unsafe.ReadUnaligned<T>(bufferPtr);
        }
        else
        {
            byte* ptr = stackalloc byte[size];
            Reverse(bufferPtr, ptr, size);
            rtn = Unsafe.ReadUnaligned<T>(ptr);
        }

        return rtn;
    }

    /// <summary>
    /// Ensures that <paramref name="byteCt"/> bytes are in the buffer to be read.
    /// </summary>
    protected unsafe bool EnsureMoreLength(int byteCt)
    {
        if (_streamMode)
        {
            if (_buffer is not null && _buffer.Length > _index + byteCt)
            {
                if (_length == 0)
                {
                    _length = _stream!.Read(_buffer, 0, _buffer.Length - _length);
                    if (_length == 0)
                        goto fail;
                    _position += _length;
                }
                return _length >= _index + byteCt - 1;
            }
            if (_stream is null)
                goto fail;
            int l = GetStreamBufferLength();
            if (byteCt > l) l = byteCt;
            int rl;
            // buffer not initialized
            if (_buffer is not { Length: > 0 })
            {
                _buffer = new byte[l];
                if (!_stream.CanRead)
                    goto fail;
                rl = _stream.Read(_buffer, 0, l);
                _index = 0;
                _position += rl;
                _length = rl;
                if (rl < byteCt)
                    goto fail;
            }
            else // partially or fully processed buffer
            {
                int remaining = _buffer.Length - _index;
                if (byteCt <= _buffer.Length) // space for remaining and needed bytes in a new buffer
                {
                    if (remaining != 0)
                    {
                        fixed (byte* ptr = _buffer)
                            Buffer.MemoryCopy(ptr + _index, ptr, _buffer.Length, remaining);
                    }
                }
                else // not enough space for needed bytes
                {
                    byte* st = stackalloc byte[remaining];
                    fixed (byte* ptr = _buffer)
                        Buffer.MemoryCopy(ptr + _index, st, remaining, remaining);
                    _buffer = new byte[byteCt];
                    fixed (byte* ptr = _buffer)
                        Buffer.MemoryCopy(st, ptr, remaining, remaining);
                }
                _index = 0;
                if (!_stream.CanRead)
                    goto fail;
                rl = _stream.Read(_buffer, remaining, _buffer.Length - remaining);
                _length = remaining + rl;
                _position += rl;
                if (rl < remaining - byteCt)
                    goto fail;
            }
            return true;
        }

        if (_buffer is not null && _index + byteCt <= _length) return true;

        fail:
        return false;
    }

    /// <summary>
    /// Reads as many bytes as can fit into <paramref name="buffer"/> to the span.
    /// </summary>
    public bool ReadBlockTo(Span<byte> buffer)
    {
        if (_streamMode)
        {
            int length;
#if NETFRAMEWORK
            int bytesLeft;
            byte[] tempBuffer;
#endif
            if (_buffer is { Length: > 0 })
            {
                if (_length - _index >= buffer.Length)
                {
                    Unsafe.CopyBlock(ref buffer[0], ref _buffer[_index], (uint)buffer.Length);
                    _index += buffer.Length;
                    return true;
                }

                int offset = _length - _index;
                Unsafe.CopyBlock(ref buffer[0], ref _buffer[_index], (uint)offset);
                _index = _length;
                _position += offset;
#if NETFRAMEWORK
                bytesLeft = buffer.Length - offset;
                length = 0;
                tempBuffer = new byte[Math.Min(bytesLeft, 512)];
                do
                {
                    int read = Stream!.Read(tempBuffer, 0, tempBuffer.Length);
                    if (read == 0)
                        break;
                    tempBuffer.AsSpan(0, read).CopyTo(buffer.Slice(offset + length, bytesLeft));
                    bytesLeft -= read;
                    length += read;
                }
                while (bytesLeft > 0);
#else
                length = Stream!.Read(buffer.Slice(offset, buffer.Length - offset));
#endif
                _position += length;
                if (length + offset >= buffer.Length)
                    return true;

                Overflow(typeof(byte[]), length);
                return false;
            }

#if NETFRAMEWORK
            bytesLeft = buffer.Length;
            length = 0;
            tempBuffer = new byte[Math.Min(bytesLeft, 512)];
            do
            {
                int read = Stream!.Read(tempBuffer, 0, tempBuffer.Length);
                if (read == 0)
                    break;
                tempBuffer.AsSpan(0, read).CopyTo(buffer.Slice(length, bytesLeft));
                bytesLeft -= read;
                length += read;
            }
            while (bytesLeft > 0);
#else
            length = Stream!.Read(buffer);
#endif
            _position += length;
            if (length >= buffer.Length)
                return true;

            Overflow(typeof(byte[]), length);
            return false;
        }

        if (!EnsureMoreLength(buffer.Length))
        {
            Overflow(typeof(byte[]), buffer.Length);
            buffer.Clear();
            return false;
        }

        Unsafe.CopyBlock(ref buffer[0], ref _buffer![_index], (uint)buffer.Length);
        _index += buffer.Length;
        return true;
    }

    /// <summary>
    /// Reads <paramref name="blockSize"/> bytes into <paramref name="block"/> starting at <paramref name="blockOffset"/>.
    /// </summary>
    public bool ReadBlockTo(byte[] block, int blockOffset = 0, int blockSize = -1)
    {
        if (block == null)
            throw new ArgumentNullException(nameof(block));
        if (blockSize == 0)
            return true;
        if (blockOffset < 0)
            blockOffset = 0;
        if (blockSize < 0)
            blockSize = block.Length - blockOffset;
        if (blockOffset >= block.Length)
            throw new ArgumentOutOfRangeException(nameof(blockOffset));
        if (blockOffset + blockSize > block.Length)
            throw new ArgumentOutOfRangeException(nameof(blockSize));
        if (_streamMode)
        {
            int length;
            if (_buffer is { Length: > 0 })
            {
                if (_length - _index >= block.Length)
                {
                    Buffer.BlockCopy(_buffer, _index, block, blockOffset, blockSize);
                    _index += block.Length;
                    return true;
                }

                int offset = _length - _index;
                Buffer.BlockCopy(_buffer, _index, block, blockOffset, offset);
                _index = _length;
                _position += offset;
                length = Stream!.Read(block, offset + blockOffset, blockSize - offset);
                _position += length;
                if (length + offset >= blockSize)
                    return true;

                Overflow(typeof(byte[]), length);
                return false;
            }

            length = Stream!.Read(block, blockOffset, blockSize);
            _position += length;
            if (length >= blockSize)
                return true;

            Overflow(typeof(byte[]), blockSize);
            return false;
        }

        if (!EnsureMoreLength(blockSize))
        {
            Overflow(typeof(byte[]), blockSize);
            Unsafe.InitBlock(ref block[blockOffset], 0, (uint)blockSize);
            return false;
        }

        Buffer.BlockCopy(_buffer!, _index, block, blockOffset, blockSize);
        _index += blockSize;
        return true;
    }

    /// <summary>
    /// Reads a byte array of length <paramref name="length"/>, without reading a length. Consider using <see cref="ReadBlockTo(byte[], int, int)"/> or <see cref="ReadBlockTo(Span{byte})"/> instead.
    /// </summary>
    public byte[] ReadBlock(int length)
    {
        byte[] block = new byte[length];
        ReadBlockTo(block, 0, length);
        return block;
    }

    /// <summary>
    /// Reads a generic unmanaged struct. Not recommended as there are no endianness checks, only use for local storage.
    /// </summary>
    public unsafe T ReadStruct<T>() where T : unmanaged
    {
        if (EnsureMoreLength(sizeof(T)))
            return Read<T>();

        Overflow(typeof(T), sizeof(T));
        return default;
    }

    /// <summary>
    /// Reads a byte array and its length (as a UInt16).
    /// </summary>
    public byte[] ReadUInt8Array()
    {
        ushort length = ReadUInt16();
        return length == 0 ? Array.Empty<byte>() : ReadBlock(length);
    }

    /// <summary>
    /// Skips a certain number of bytes without reading them.
    /// </summary>
    public void Skip(int bytes)
    {
        if (!EnsureMoreLength(bytes))
        {
            Overflow("skipped bytes", bytes);
        }
        else
        {
            _index += bytes;
        }
    }

    /// <summary>
    /// Goto an index in the buffer.
    /// </summary>
    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public void Goto(int toPosition)
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteReaderNavMethodsStreamModeNotSupported);
        if (Position < toPosition)
            Skip(Position - toPosition);
        else
        {
            _index = toPosition;
        }
    }

    /// <summary>
    /// Reads a byte array that can be null. Length is read as a UInt16.
    /// </summary>
    public byte[]? ReadNullableUInt8Array()
    {
        if (!ReadBool()) return null;
        return ReadUInt8Array();
    }

    /// <summary>
    /// Reads a byte array. Length is read as a Int32.
    /// </summary>
    public byte[] ReadLongUInt8Array()
    {
        int length = ReadInt32();
        if (length == 0) return Array.Empty<byte>();
        return ReadBlock(length);
    }

    /// <summary>
    /// Reads a byte array that can be null. Length is read as a Int32.
    /// </summary>
    public byte[]? ReadNullableLongBytes()
    {
        if (!ReadBool()) return null;
        return ReadLongUInt8Array();
    }

    /// <summary>
    /// Reads an <see cref="int"/> from the buffer.
    /// </summary>
    public int ReadInt32()
    {
        if (EnsureMoreLength(sizeof(int)))
            return Read<int>();

        Overflow(typeof(int), sizeof(int));
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="int"/> from the buffer.
    /// </summary>
    public int? ReadNullableInt32()
    {
        if (!ReadBool()) return null;
        return ReadInt32();
    }

    /// <summary>
    /// Reads a <see cref="uint"/> from the buffer.
    /// </summary>
    public uint ReadUInt32()
    {
        if (EnsureMoreLength(sizeof(uint)))
            return Read<uint>();

        Overflow(typeof(uint), sizeof(uint));
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="uint"/> from the buffer.
    /// </summary>
    public uint? ReadNullableUInt32()
    {
        if (!ReadBool()) return null;
        return ReadUInt32();
    }

    /// <summary>
    /// Reads a <see cref="byte"/> from the buffer.
    /// </summary>
    public byte ReadUInt8()
    {
        if (!EnsureMoreLength(1))
        {
            Overflow(typeof(byte), 1);
            return default;
        }

        byte rtn = _buffer![_index];
        ++_index;
        return rtn;
    }

    /// <summary>
    /// Reads a nullable <see cref="byte"/> from the buffer.
    /// </summary>
    public byte? ReadNullableUInt8()
    {
        if (!ReadBool()) return null;
        return ReadUInt8();
    }

    /// <summary>
    /// Reads a <see cref="sbyte"/> from the buffer.
    /// </summary>
    public sbyte ReadInt8()
    {
        if (!EnsureMoreLength(1))
        {
            Overflow(typeof(sbyte), 1);
            return default;
        }

        sbyte rtn = unchecked((sbyte)_buffer![_index]);
        ++_index;
        return rtn;
    }

    /// <summary>
    /// Reads a nullable <see cref="sbyte"/> from the buffer.
    /// </summary>
    public sbyte? ReadNullableInt8()
    {
        if (!ReadBool()) return null;
        return ReadInt8();
    }

    /// <summary>
    /// Reads a <see cref="bool"/> from the buffer.
    /// </summary>
    public bool ReadBool()
    {
        if (!EnsureMoreLength(1))
        {
            Overflow(typeof(bool), 1);
            return default;
        }

        bool rtn = _buffer![_index] > 0;
        ++_index;
        return rtn;
    }

    /// <summary>
    /// Reads a nullable <see cref="bool"/> from the buffer.
    /// </summary>
    public bool? ReadNullableBool()
    {
        if (!ReadBool()) return null;
        return ReadBool();
    }

    /// <summary>
    /// Reads a <see cref="long"/> from the buffer.
    /// </summary>
    public long ReadInt64()
    {
        if (EnsureMoreLength(sizeof(long)))
            return Read<long>();

        Overflow(typeof(long), sizeof(long));
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="long"/> from the buffer.
    /// </summary>
    public long? ReadNullableInt64()
    {
        if (!ReadBool()) return null;
        return ReadInt64();
    }

    /// <summary>
    /// Reads a <see cref="ulong"/> from the buffer.
    /// </summary>
    public ulong ReadUInt64()
    {
        if (EnsureMoreLength(sizeof(ulong)))
            return Read<ulong>();

        Overflow(typeof(ulong), sizeof(ulong));
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="ulong"/> from the buffer.
    /// </summary>
    public ulong? ReadNullableUInt64()
    {
        if (!ReadBool()) return null;
        return ReadUInt64();
    }

    /// <summary>
    /// Reads a <see cref="short"/> from the buffer.
    /// </summary>
    public short ReadInt16()
    {
        if (EnsureMoreLength(sizeof(short)))
            return Read<short>();

        Overflow(typeof(short), sizeof(short));
        return default;
    }

    /// <summary>
    /// Reads a 24-bit (3 byte) <see cref="int"/> from the buffer.
    /// </summary>
    /// <remarks>Range: <seealso cref="ByteEncoders.Int24MinValue"/>-<seealso cref="ByteEncoders.Int24MaxValue"/>.</remarks>
    public int ReadInt24()
    {
        if (!EnsureMoreLength(3))
        {
            Overflow("Int24", sizeof(short));
            return default;
        }

        ushort sh = Read<ushort>();
        byte bt = _buffer![_index];
        ++_index;
        return (sh | (bt << 16)) - ByteEncoders.Int24MaxValue;
    }

    /// <summary>
    /// Reads a 24-bit (3 byte) <see cref="uint"/> from the buffer.
    /// </summary>
    /// <remarks>Range: 0-2*<seealso cref="ByteEncoders.Int24MinValue"/>.</remarks>
    public uint ReadUInt24()
    {
        if (!EnsureMoreLength(3))
        {
            Overflow("UInt24", sizeof(short));
            return default;
        }

        ushort sh = Read<ushort>();
        byte bt = _buffer![_index];
        ++_index;
        int i = (sh | (bt << 16)) - ByteEncoders.Int24MaxValue;

        if (i < 0)
            return (uint)-(i - ByteEncoders.Int24MaxValue);
        return (uint)i;
    }

    /// <summary>
    /// Reads a nullable 24-bit (3 byte) <see cref="int"/> from the buffer.
    /// </summary>
    /// <remarks>Range: <seealso cref="ByteEncoders.Int24MinValue"/>-<seealso cref="ByteEncoders.Int24MaxValue"/>.</remarks>
    public int? ReadNullableInt24()
    {
        if (!ReadBool()) return null;
        return ReadInt24();
    }

    /// <summary>
    /// Reads a nullable <see cref="short"/> from the buffer.
    /// </summary>
    public short? ReadNullableInt16()
    {
        if (!ReadBool()) return null;
        return ReadInt16();
    }

    /// <summary>
    /// Reads a <see cref="ushort"/> from the buffer.
    /// </summary>
    public ushort ReadUInt16()
    {
        if (EnsureMoreLength(sizeof(ushort)))
            return Read<ushort>();

        Overflow(typeof(ushort), sizeof(ushort));
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="ushort"/> from the buffer.
    /// </summary>
    public ushort? ReadNullableUInt16()
    {
        if (!ReadBool()) return null;
        return ReadUInt16();
    }

    /// <summary>
    /// Reads a <see cref="float"/> from the buffer.
    /// </summary>
    public float ReadFloat()
    {
        if (EnsureMoreLength(sizeof(float)))
            return Read<float>();

        Overflow(typeof(float), sizeof(float));
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="float"/> from the buffer.
    /// </summary>
    public float? ReadNullableFloat()
    {
        if (!ReadBool()) return null;
        return ReadFloat();
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Reads a <see cref="Half"/> from the buffer.
    /// </summary>
    public Half ReadHalf()
    {
        if (EnsureMoreLength(2))
            return Read<Half>();

        Overflow(typeof(Half), 2);
        return default;
    }

    /// <summary>
    /// Reads a nullable <see cref="Half"/> from the buffer.
    /// </summary>
    public Half? ReadNullableHalf()
    {
        if (!ReadBool()) return null;
        return ReadHalf();
    }
#endif

    /// <summary>
    /// Reads a <see cref="decimal"/> from the buffer.
    /// </summary>
    public unsafe decimal ReadDecimal()
    {
        if (!EnsureMoreLength(16))
        {
            Overflow(typeof(decimal), 16);
            return default;
        }

#if NET5_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
#else
        int[] bits = new int[4];
#endif

        fixed (byte* ptr = &_buffer![_index])
        {
            for (int i = 0; i < 4; ++i)
                bits[i] = ReadFromBuffer<int>(ptr + i * 4);
        }

        return new decimal(bits);
    }

    /// <summary>
    /// Reads a <see cref="decimal"/> from the buffer.
    /// </summary>
    public decimal? ReadNullableDecimal()
    {
        if (!ReadBool()) return null;
        return ReadDecimal();
    }

    /// <summary>
    /// Reads a <see cref="double"/> from the buffer.
    /// </summary>
    public double ReadDouble()
    {
        if (EnsureMoreLength(sizeof(double)))
            return Read<double>();

        Overflow(typeof(double), sizeof(double));
        return default;
    }

    /// <summary>
    /// Reads a <see cref="double"/> from the buffer.
    /// </summary>
    public double? ReadNullableDouble()
    {
        if (!ReadBool()) return null;
        return ReadDouble();
    }

    /// <summary>
    /// Reads a <see cref="char"/> from the buffer.
    /// </summary>
    public char ReadChar()
    {
        if (EnsureMoreLength(sizeof(char)))
            return Read<char>();

        Overflow(typeof(char), sizeof(char));
        return default;
    }

    /// <summary>
    /// Reads a <see cref="char"/> from the buffer.
    /// </summary>
    public char? ReadNullableChar()
    {
        if (!ReadBool()) return null;
        return ReadChar();
    }

    /// <summary>
    /// Reads a <see cref="Type"/> array from the buffer with nullable elements.
    /// </summary>
    public Type?[] ReadTypeArray()
    {
        int len = ReadUInt16();
        Type?[] rtn = new Type?[len];
        for (int i = 0; i < len; ++i)
        {
            rtn[i] = ReadType();
        }

        return rtn;
    }

    /// <summary>
    /// Follows the template for reading a type but only outputs the string value with assembly info prepended <see cref="Type"/>.
    /// </summary>
    /// <remarks>For more info about the type, use the overload <see cref="ReadTypeInfo(out byte)"/>.</remarks>
    public string? ReadTypeInfo() => ReadTypeInfo(out _);

    /// <summary>
    /// Follows the template for reading a type but only outputs the string value with assembly info prepended <see cref="Type"/>.
    /// </summary>
    /// <remarks>Also outputs the internal flag used for compression. If <c>(<paramref name="flag"/> &amp; 128) != 0</c>, the type was written as <see langword="null"/>.</remarks>
    public string? ReadTypeInfo(out byte flag)
    {
        const string nsSystem = "System";

        flag = ReadUInt8();
        if ((flag & 128) != 0)
            return null;

        string ns = ReadString();
        if ((flag & 64) != 0)
        {
            // mscorlib
            ns = "[mscorlib.dll] " + (ns.Length == 0 ? nsSystem : nsSystem + "." + ns);
        }
        return ns;
    }

    /// <summary>
    /// Reads a nullable <see cref="Type"/> from the buffer. See <see cref="ReadTypeInfo()"/> for debugging type reading/writing.
    /// </summary>
    /// <remarks>To differentiate between a not-found type and a type that was <see langword="null"/> to begin with, use the overload <see cref="ReadType(out bool)"/>.</remarks>
    public Type? ReadType() => ReadType(out _);

    /// <summary>
    /// Reads a nullable <see cref="Type"/> from the buffer. See <see cref="ReadTypeInfo()"/> for debugging type reading/writing.
    /// </summary>
    /// <remarks>If the type isn't found, <paramref name="wasPassedNull"/> will be set as <see langword="true"/>.</remarks>
    /// <param name="wasPassedNull"><see langword="True"/> if the written type was <see langword="null"/>, otherwise the type was just not found.</param>
    public Type? ReadType(out bool wasPassedNull)
    {
        const string nsSystem = "System";
        wasPassedNull = false;
        byte flag = ReadUInt8();
        if ((flag & 128) != 0)
        {
            wasPassedNull = true;
            return null;
        }

        string ns = ReadString();
        if ((flag & 64) != 0)
        {
            // mscorlib
            return ByteEncoders.MSCoreLib.GetType(ns.Length == 0 ? nsSystem : nsSystem + "." + ns);
        }

        return Type.GetType(ns);
    }

    /// <summary>
    /// Reads a <see cref="string"/> from the buffer.
    /// </summary>
    public string ReadString()
    {
        ushort length = ReadUInt16();

        if (length == 0)
            return string.Empty;

        if (!EnsureMoreLength(length))
        {
            Overflow(typeof(string), length);
            return string.Empty;
        }

        string str = Encoding.UTF8.GetString(_buffer!, _index, length);
        _index += length;
        return str;
    }

    /// <summary>
    /// Reads a nullable <see cref="string"/> from the buffer.
    /// </summary>
    public string? ReadNullableString()
    {
        return !ReadBool() ? null : ReadString();
    }

    /// <summary>
    /// Reads an ASCII <see cref="string"/> from the buffer.
    /// </summary>
    [Obsolete("Renamed to ReadShortAsciiString for consistancy.")]
    public string ReadAsciiSmall() => ReadShortAsciiString();

    /// <summary>
    /// Reads a nullable ASCII <see cref="string"/> from the buffer.
    /// </summary>
    [Obsolete("Renamed to ReadNullableShortAsciiString for consistancy.")]
    public string? ReadNullableAsciiSmall() => ReadNullableShortAsciiString();

    /// <summary>
    /// Reads an ASCII <see cref="string"/> from the buffer.
    /// </summary>
    public string ReadShortAsciiString()
    {
        int length = ReadUInt8();

        if (length == 0)
            return string.Empty;

        if (!EnsureMoreLength(length))
        {
            Overflow(typeof(string), length);
            return string.Empty;
        }

        string str = Encoding.ASCII.GetString(_buffer!, _index, length);
        _index += length;
        return str;
    }

    /// <summary>
    /// Reads a nullable ASCII <see cref="string"/> from the buffer.
    /// </summary>
    public string? ReadNullableShortAsciiString()
    {
        if (!ReadBool()) return null;
        return ReadShortAsciiString();
    }

    /// <summary>
    /// Reads a <see cref="string"/> from the buffer with a max length of <see cref="byte.MaxValue"/>.
    /// </summary>
    public string ReadShortString()
    {
        byte length = ReadUInt8();
        if (length == 0)
            return string.Empty;

        if (!EnsureMoreLength(length))
        {
            Overflow(typeof(string), length);
            return string.Empty;
        }

        string str = Encoding.UTF8.GetString(_buffer!, _index, length);
        _index += length;
        return str;
    }

    /// <summary>
    /// Reads a nullable <see cref="string"/> from the buffer with a max length of <see cref="byte.MaxValue"/>.
    /// </summary>
    public string? ReadNullableShortString()
    {
        return !ReadBool() ? null : ReadShortString();
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> from the buffer. Keeps <see cref="DateTimeKind"/> information.
    /// </summary>
    public DateTime ReadDateTime()
    {
        if (EnsureMoreLength(sizeof(long)))
            return DateTime.FromBinary(Read<long>());

        Overflow(typeof(DateTime), sizeof(long));
        return default;
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> from the buffer. Keeps <see cref="DateTimeKind"/> information.
    /// </summary>
    public DateTime? ReadNullableDateTime()
    {
        if (!ReadBool()) return null;
        return ReadDateTime();
    }

    /// <summary>
    /// Reads a <see cref="DateTimeOffset"/> from the buffer. Keeps <see cref="DateTimeKind"/> and offset information.
    /// </summary>
    public unsafe DateTimeOffset ReadDateTimeOffset()
    {
        if (!EnsureMoreLength(sizeof(long) + sizeof(short)))
        {
            Overflow(typeof(DateTimeOffset), sizeof(long) + sizeof(short));
            return default;
        }

        fixed (byte* ptr = &_buffer![_index])
        {
            long v = ReadFromBuffer<long>(ptr);
            long offset = ReadFromBuffer<short>(ptr + sizeof(long)) * 600000000L;
            return new DateTimeOffset(DateTime.FromBinary(v), *(TimeSpan*)&offset);
        }
    }

    /// <summary>
    /// Reads a <see cref="DateTimeOffset"/> from the buffer. Keeps <see cref="DateTimeKind"/> and offset information.
    /// </summary>
    public DateTimeOffset? ReadNullableDateTimeOffset()
    {
        if (!ReadBool()) return null;
        return ReadDateTimeOffset();
    }

    /// <summary>
    /// Reads a <see cref="TimeSpan"/> from the buffer.
    /// </summary>
    public unsafe TimeSpan ReadTimeSpan()
    {
        if (!EnsureMoreLength(sizeof(long)))
        {
            Overflow(typeof(TimeSpan), sizeof(long));
            return default;
        }

        long ticks = Read<long>();
        return *(TimeSpan*)&ticks;
    }

    /// <summary>
    /// Reads a <see cref="TimeSpan"/> from the buffer.
    /// </summary>
    public TimeSpan? ReadNullableTimeSpan()
    {
        if (!ReadBool()) return null;
        return ReadTimeSpan();
    }

    /// <summary>
    /// Reads a <see cref="Guid"/> from the buffer.
    /// </summary>
    public Guid ReadGuid()
    {
        if (!EnsureMoreLength(GuidSize))
        {
            Overflow(typeof(Guid), GuidSize);
            return default;
        }

#if NETFRAMEWORK
        byte[] buffer = ReadBlock(GuidSize);
#else
        Span<byte> buffer = stackalloc byte[GuidSize];
        ReadBlockTo(buffer);
#endif
        if (!IsBigEndian)
            return new Guid(buffer);

        byte temp = buffer[0];
        buffer[0] = buffer[3];
        buffer[3] = temp;

        temp = buffer[1];
        buffer[1] = buffer[2];
        buffer[2] = temp;

        temp = buffer[4];
        buffer[4] = buffer[5];
        buffer[5] = temp;

        temp = buffer[6];
        buffer[6] = buffer[7];
        buffer[7] = temp;

        return new Guid(buffer);
    }

    /// <summary>
    /// Reads a <see cref="Guid"/> from the buffer.
    /// </summary>
    public Guid? ReadNullableGuid() => !ReadBool() ? null : ReadGuid();

    /// <summary>
    /// Reads a <typeparamref name="TEnum"/> from the buffer. Size is based on the underlying type.
    /// </summary>
    /// <remarks>Don't use this if the underlying type is subject to change when data vesioning matters.</remarks>
    public unsafe TEnum ReadEnum<TEnum>() where TEnum : unmanaged, Enum
    {
        if (EnsureMoreLength(sizeof(TEnum)))
            return Read<TEnum>();

        Overflow(typeof(TEnum), sizeof(TEnum));
        return default;
    }

    /// <summary>
    /// Reads a nullable <typeparamref name="TEnum"/> from the buffer. Size is based on the underlying type.
    /// </summary>
    /// <remarks>Don't use this if the underlying type is subject to change when data vesioning matters.</remarks>
    public TEnum? ReadNullableEnum<TEnum>() where TEnum : unmanaged, Enum => !ReadBool() ? null : ReadEnum<TEnum>();

    /// <summary>
    /// Reads a <see cref="Guid"/> array and its length (as a UInt16).
    /// </summary>
    // ReSharper disable once RedundantUnsafeContext
    public unsafe Guid[] ReadGuidArray()
    {
        ushort len = ReadUInt16();
        int size = len * GuidSize;
        if (len == 0) return Array.Empty<Guid>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(Guid[]), size);
            return Array.Empty<Guid>();
        }

        Guid[] rtn = new Guid[len];
#if NETFRAMEWORK
        byte[] guidBuffer = new byte[GuidSize];
#else
        byte* guidBuffer = stackalloc byte[GuidSize];
#endif
        for (int i = 0; i < len; i++)
        {
#if NETFRAMEWORK
            ReadBlockTo(guidBuffer.AsSpan());
#else
            ReadBlockTo(new Span<byte>(guidBuffer, GuidSize));
#endif

            if (IsBigEndian)
            {
                byte temp = guidBuffer[0];
                guidBuffer[0] = guidBuffer[3];
                guidBuffer[3] = temp;

                temp = guidBuffer[1];
                guidBuffer[1] = guidBuffer[2];
                guidBuffer[2] = temp;

                temp = guidBuffer[4];
                guidBuffer[4] = guidBuffer[5];
                guidBuffer[5] = temp;

                temp = guidBuffer[6];
                guidBuffer[6] = guidBuffer[7];
                guidBuffer[7] = temp;
            }

#if NETFRAMEWORK
            Guid guid = new Guid(guidBuffer);
#else
            Guid guid = new Guid(new Span<byte>(guidBuffer, GuidSize));
#endif
            rtn[i] = guid;
        }
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="Guid"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public Guid[]? ReadNullableGuidArray() => !ReadBool() ? null : ReadGuidArray();

    /// <summary>
    /// Reads a <see cref="DateTime"/> array and its length (as a UInt16). Keeps <see cref="DateTimeKind"/> information.
    /// </summary>
    public unsafe DateTime[] ReadDateTimeArray()
    {
        ushort len = ReadUInt16();
        int size = len * sizeof(long);
        if (len == 0) return Array.Empty<DateTime>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(DateTime[]), size);
            return Array.Empty<DateTime>();
        }

        DateTime[] rtn = new DateTime[len];
        fixed (byte* ptr = &_buffer![_index])
        {
            for (int i = 0; i < len; i++)
            {
                rtn[i] = DateTime.FromBinary(ReadFromBuffer<long>(ptr + i * sizeof(long)));
            }
        }
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="DateTime"/> array that can be null and its length (as a UInt16). Keeps <see cref="DateTimeKind"/> information.
    /// </summary>
    public DateTime[]? ReadNullableDateTimeArray() => !ReadBool() ? null : ReadDateTimeArray();

    /// <summary>
    /// Reads a <see cref="DateTimeOffset"/> array and its length (as a UInt16). Keeps <see cref="DateTimeKind"/> and offset information.
    /// </summary>
    public unsafe DateTimeOffset[] ReadDateTimeOffsetArray()
    {
        ushort len = ReadUInt16();
        int size = len * (sizeof(long) + sizeof(short));
        if (len == 0) return Array.Empty<DateTimeOffset>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(DateTimeOffset[]), size);
            return Array.Empty<DateTimeOffset>();
        }

        DateTimeOffset[] rtn = new DateTimeOffset[len];
        fixed (byte* ptr = &_buffer![_index])
        {
            for (int i = 0; i < len; i++)
            {
                DateTime dt = DateTime.FromBinary(ReadFromBuffer<long>(ptr + i * (sizeof(long) + sizeof(short))));
                long offset = ReadFromBuffer<short>(ptr + i * (sizeof(long) + sizeof(short)) + sizeof(long)) * 600000000L;
                rtn[i] = new DateTimeOffset(dt, *(TimeSpan*)&offset);
            }
        }
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="DateTimeOffset"/> array that can be null and its length (as a UInt16). Keeps <see cref="DateTimeKind"/> and offset information.
    /// </summary>
    public DateTimeOffset[]? ReadNullableDateTimeOffsetArray() => !ReadBool() ? null : ReadDateTimeOffsetArray();

    /// <summary>
    /// Reads a <see cref="int"/> array and its length (as a UInt16).
    /// </summary>
    public int[] ReadInt32Array()
    {
        int len = ReadUInt16();
        int size = len * sizeof(int);
        if (len == 0) return Array.Empty<int>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(int[]), size);
            return Array.Empty<int>();
        }

        int[] rtn = new int[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<int>(_buffer!, _index + i * sizeof(int));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="int"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public int[]? ReadNullableInt32Array() => !ReadBool() ? null : ReadInt32Array();

    /// <summary>
    /// Reads a <see cref="uint"/> array and its length (as a UInt16).
    /// </summary>
    public uint[] ReadUInt32Array()
    {
        int len = ReadUInt16();
        int size = len * sizeof(uint);
        if (len == 0) return Array.Empty<uint>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(uint[]), size);
            return Array.Empty<uint>();
        }

        uint[] rtn = new uint[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<uint>(_buffer!, _index + i * sizeof(uint));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="uint"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public uint[]? ReadNullableUInt32Array() => !ReadBool() ? null : ReadUInt32Array();

    /// <summary>
    /// Reads a <see cref="sbyte"/> array and its length (as a UInt16).
    /// </summary>
    public unsafe sbyte[] ReadInt8Array()
    {
        ushort len = ReadUInt16();
        if (len == 0) return Array.Empty<sbyte>();
        if (!EnsureMoreLength(len))
        {
            Overflow(typeof(sbyte[]), len);
            return Array.Empty<sbyte>();
        }

        sbyte[] rtn = new sbyte[len];
        fixed (void* ptr = rtn)
        fixed (void* bfr = &_buffer![_index])
        {
            Unsafe.CopyBlock(ptr, bfr, len);
        }
        _index += len;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="sbyte"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public sbyte[]? ReadNullableInt8Array() => !ReadBool() ? null : ReadInt8Array();

    /// <summary>
    /// Reads a <see cref="bool"/> array and its length (as a UInt16).
    /// </summary>
    /// <remarks>Compresses into bits.</remarks>
    public unsafe bool[] ReadBoolArray()
    {
        ushort len = ReadUInt16();
        if (len < 1) return Array.Empty<bool>();
        int blen = (int)Math.Ceiling(len / 8d);
        if (!EnsureMoreLength(blen))
        {
            Overflow(typeof(bool[]), blen);
            return Array.Empty<bool>();
        }

        bool[] rtn = new bool[len];
        fixed (byte* ptr = &_buffer![_index])
        {
            byte* ptr2 = ptr;
            byte current = *ptr2;
            for (int i = 0; i < len; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ptr2++;
                    current = *ptr2;
                }
                rtn[i] = (1 & (current >> mod)) == 1;
            }
        }

        _index += blen;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="BitArray"/> and its length (as a UInt16).
    /// </summary>
    /// <remarks>Compresses into bits.</remarks>
    public unsafe BitArray ReadBitArray()
    {
        ushort len = ReadUInt16();
        if (len < 1)
            return new BitArray(0);

        int blen = (int)Math.Ceiling(len / 8d);
        if (!EnsureMoreLength(blen))
        {
            Overflow(typeof(BitArray), blen);
            return new BitArray(0);
        }

        BitArray rtn = new BitArray(len);
        fixed (byte* ptr = &_buffer![_index])
        {
            byte* ptr2 = ptr;
            byte current = *ptr2;
            for (int i = 0; i < len; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ptr2++;
                    current = *ptr2;
                }
                rtn[i] = (1 & (current >> mod)) == 1;
            }
        }

        _index += blen;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="BitArray"/> and its length (as a Int32).
    /// </summary>
    /// <remarks>Compresses into bits.</remarks>
    public unsafe BitArray ReadLongBitArray()
    {
        int len = ReadInt32();
        if (len < 1)
            return new BitArray(0);

        int blen = (int)Math.Ceiling(len / 8d);
        if (!EnsureMoreLength(blen))
        {
            Overflow(typeof(BitArray), blen);
            return new BitArray(0);
        }

        BitArray rtn = new BitArray(len);
        fixed (byte* ptr = &_buffer![_index])
        {
            byte* ptr2 = ptr;
            byte current = *ptr2;
            for (int i = 0; i < len; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ptr2++;
                    current = *ptr2;
                }
                rtn[i] = (1 & (current >> mod)) == 1;
            }
        }

        _index += blen;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="bool"/> array and its length (as a UInt32).
    /// </summary>
    /// <remarks>Compresses into bits.</remarks>
    public unsafe bool[] ReadLongBoolArray()
    {
        int len = ReadInt32();
        if (len < 1) return Array.Empty<bool>();
        int blen = (int)Math.Ceiling(len / 8d);
        if (!EnsureMoreLength(blen))
            return null!;
        bool[] rtn = new bool[len];
        fixed (byte* ptr = &_buffer![_index])
        {
            byte* ptr2 = ptr;
            byte current = *ptr2;
            for (int i = 0; i < len; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ptr2++;
                    current = *ptr2;
                }
                rtn[i] = (1 & (current >> mod)) == 1;
            }
        }

        _index += blen;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="bool"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public bool[]? ReadNullableBoolArray() => !ReadBool() ? null : ReadBoolArray();

    /// <summary>
    /// Reads a <see cref="BitArray"/>  that can be null and its length (as a UInt16).
    /// </summary>
    public BitArray? ReadNullableBitArray() => !ReadBool() ? null : ReadBitArray();

    /// <summary>
    /// Reads a <see cref="long"/> array and its length (as a UInt16).
    /// </summary>
    public long[] ReadInt64Array()
    {
        int len = ReadUInt16();
        int size = len * sizeof(long);
        if (len == 0) return Array.Empty<long>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(long[]), size);
            return Array.Empty<long>();
        }

        long[] rtn = new long[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<long>(_buffer!, _index + i * sizeof(long));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="long"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public long[]? ReadNullableInt64Array() => !ReadBool() ? null : ReadInt64Array();

    /// <summary>
    /// Reads a <see cref="ulong"/> array and its length (as a UInt16).
    /// </summary>
    public ulong[] ReadUInt64Array()
    {
        int len = ReadUInt16();
        int size = len * sizeof(ulong);
        if (len == 0) return Array.Empty<ulong>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(ulong[]), size);
            return Array.Empty<ulong>();
        }

        ulong[] rtn = new ulong[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<ulong>(_buffer!, _index + i * sizeof(ulong));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="ulong"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public ulong[]? ReadNullableUInt64Array() => !ReadBool() ? null : ReadUInt64Array();

    /// <summary>
    /// Reads a <see cref="short"/> array and its length (as a UInt16).
    /// </summary>
    public short[] ReadInt16Array()
    {
        int len = ReadUInt16();
        int size = len * sizeof(short);
        if (len == 0) return Array.Empty<short>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(short[]), size);
            return Array.Empty<short>();
        }

        short[] rtn = new short[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<short>(_buffer!, _index + i * sizeof(short));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="short"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public short[]? ReadNullableInt16Array() => !ReadBool() ? null : ReadInt16Array();

    /// <summary>
    /// Reads a <see cref="ushort"/> array and its length (as a UInt16).
    /// </summary>
    public ushort[] ReadUInt16Array()
    {
        int len = ReadUInt16();
        int size = len * sizeof(ushort);
        if (len == 0) return Array.Empty<ushort>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(ushort[]), size);
            return Array.Empty<ushort>();
        }

        ushort[] rtn = new ushort[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<ushort>(_buffer!, _index + i * sizeof(ushort));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="ushort"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public ushort[]? ReadNullableUInt16Array() => !ReadBool() ? null : ReadUInt16Array();

    /// <summary>
    /// Reads a <see cref="float"/> array and its length (as a UInt16).
    /// </summary>
    public float[] ReadFloatArray()
    {
        int len = ReadUInt16();
        int size = len * sizeof(float);
        if (len == 0) return Array.Empty<float>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(float[]), size);
            return Array.Empty<float>();
        }

        float[] rtn = new float[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<float>(_buffer!, _index + i * sizeof(float));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="float"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public float[]? ReadNullableFloatArray() => !ReadBool() ? null : ReadFloatArray();

    /// <summary>
    /// Reads a <see cref="decimal"/> array and its length (as a UInt16).
    /// </summary>
    public unsafe decimal[] ReadDecimalArray()
    {
        ushort len = ReadUInt16();
        int size = len * 16;
        if (len == 0) return Array.Empty<decimal>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(decimal[]), size);
            return Array.Empty<decimal>();
        }

        decimal[] rtn = new decimal[len];
#if NET5_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
#else
        int[] bits = new int[4];
#endif
        fixed (byte* ptr = &_buffer![_index])
        {
            for (int i = 0; i < len; ++i)
            {
                for (int j = 0; j < 4; ++j)
                    bits[j] = ReadFromBuffer<int>(ptr + i * 16 + j * 4);

                rtn[i] = new decimal(bits);
            }
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="decimal"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public decimal[]? ReadNullableDecimalArray() => !ReadBool() ? null : ReadDecimalArray();

    /// <summary>
    /// Reads a <see cref="double"/> array and its length (as a UInt16).
    /// </summary>
    public double[] ReadDoubleArray()
    {
        int len = ReadUInt16();
        int size = len * sizeof(double);
        if (len == 0) return Array.Empty<double>();
        if (!EnsureMoreLength(size))
        {
            Overflow(typeof(double[]), size);
            return Array.Empty<double>();
        }

        double[] rtn = new double[len];
        for (int i = 0; i < len; i++)
        {
            rtn[i] = ReadFromBuffer<double>(_buffer!, _index + i * sizeof(double));
        }

        _index += size;
        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="double"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public double[]? ReadNullableDoubleArray() => !ReadBool() ? null : ReadDoubleArray();

    /// <summary>
    /// Reads a <see cref="char"/> array and its length (as a UInt16).
    /// </summary>
    public char[] ReadCharArray()
    {
        ushort length = ReadUInt16();

        if (length == 0)
            return Array.Empty<char>();

        if (!EnsureMoreLength(length))
        {
            Overflow(typeof(string), length);
            return Array.Empty<char>();
        }

        char[] str = Encoding.UTF8.GetChars(_buffer!, _index, length);
        _index += length;
        return str;
    }

    /// <summary>
    /// Reads a <see cref="char"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public char[]? ReadNullableCharArray() => !ReadBool() ? null : ReadCharArray();

    /// <summary>
    /// Reads a <see cref="string"/> array and its length (as a UInt16).
    /// </summary>
    public string[] ReadStringArray()
    {
        string[] rtn = new string[ReadUInt16()];
        int ttlSize = sizeof(ushort);
        for (int i = 0; i < rtn.Length; i++)
        {
            ushort length = ReadUInt16();
            ttlSize += sizeof(ushort) + length;

            if (length == 0)
            {
                rtn[i] = string.Empty;
                continue;
            }

            if (!EnsureMoreLength(length))
            {
                Overflow(typeof(string[]), ttlSize);
                for (int j = i; j < rtn.Length; ++j)
                    rtn[j] = string.Empty;
                break;
            }

            string str = Encoding.UTF8.GetString(_buffer!, _index, length);
            _index += length;
            rtn[i] = str;
        }

        return rtn;
    }

    /// <summary>
    /// Reads a <see cref="string"/> array that can be null and its length (as a UInt16).
    /// </summary>
    public string[] ReadNullableStringArray() => !ReadBool() ? null! : ReadStringArray();

    /// <summary>
    /// Creates or gets a cached delegate to read <typeparamref name="T"/> to a <see cref="ByteReader"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Reader{T}"/>.</returns>
    /// <remarks>If <paramref name="isNullable"/> is true, nullable value type structs should be indicated with <typeparamref name="T"/> being the nullable type.</remarks>
    /// <param name="isNullable">Should the type be considered nullable?</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> (marked nullable by <paramref name="isNullable"/>) is not auto-readable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
    public static Reader<T> GetReadMethodDelegate<T>(bool isNullable = false)
    {
        try
        {
            return isNullable ? NullableReaderHelper<T>.Reader : ReaderHelper<T>.Reader;
        }
        catch (TypeInitializationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Creates a delegate to read <paramref name="type"/> from a <see cref="ByteReader"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Reader{T}"/>.</returns>
    /// <remarks>If <paramref name="isNullable"/> is true, nullable value type structs should be indicated with <paramref name="type"/> being the nullable type.</remarks>
    /// <param name="isNullable">Should the type be considered nullable?</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="AutoEncodeTypeNotFoundException"><paramref name="type"/> (marked nullable by <paramref name="isNullable"/>) is not auto-readable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
    public static Delegate CreateReadMethodDelegate(Type type, bool isNullable = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        MethodInfo? method = GetReadMethod(type, isNullable);
        if (method == null)
            throw new AutoEncodeTypeNotFoundException(type);

        try
        {
            return method.CreateDelegate(typeof(Reader<>).MakeGenericType(type));
        }
        catch (ArgumentException ex)
        {
            throw new ByteEncoderException(Properties.Localization.CouldNotCreateEncoderMethod, ex);
        }
    }

    internal static Reader<T1> CreateReadMethodDelegate<T1>(bool isNullable = false) => (Reader<T1>)CreateReadMethodDelegate(typeof(T1), isNullable);

    /// <summary>
    /// Get the <see cref="MethodInfo"/> of a method to read <paramref name="type"/> from a <see cref="ByteReader"/>, or <see langword="null"/> if it's not found.
    /// </summary>
    /// <remarks>If <paramref name="isNullable"/> is true, nullable value type structs should be indicated with <paramref name="type"/> being the nullable type.</remarks>
    /// <param name="isNullable">Should the type be considered nullable?</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
    public static MethodInfo? GetReadMethod(Type type, bool isNullable = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        MethodInfo? method;
        if (type.IsEnum)
        {
            method = (isNullable ? ReadNullableEnumMethod : ReadEnumMethod)?.MakeGenericMethod(type)
                     ?? throw new MemberAccessException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, isNullable ? "ReadNullableEnum" : "ReadEnum"));
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GenericTypeArguments[0].IsEnum)
        {
            method = (isNullable ? ReadNullableEnumMethod : ReadEnumMethod)?.MakeGenericMethod(type.GenericTypeArguments[0])
                     ?? throw new MemberAccessException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, isNullable ? "ReadNullableEnum" : "ReadEnum"));
        }
        else
        {
            lock (ByteEncoders.AutoReadSync)
            {
                if (_nullableReaders == null || _nonNullableReaders == null)
                    PrepareMethods();
                if (isNullable)
                {
                    _nullableReaders!.TryGetValue(type, out method);
                }
                else if (!_nonNullableReaders!.TryGetValue(type, out method) && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    _nullableReaders!.TryGetValue(type, out method);
            }
        }

        return method;
    }

    /// <summary>
    /// Caches a delegate to read <typeparamref name="T"/> to a <see cref="ByteReader"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Reader{T}"/>.</returns>
    /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-readable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
    private static class ReaderHelper<T>
    {
        /// <summary>
        /// Caches a delegate to read <typeparamref name="T"/> to a <see cref="ByteReader"/>.
        /// </summary>
        /// <returns>A delegate of type <see cref="Reader{T}"/>.</returns>
        /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-readable.</exception>
        /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
        internal static Reader<T> Reader { get; }
        static ReaderHelper()
        {
            ByteEncoders.ThrowIfNotAutoType<T>();
            Reader = CreateReadMethodDelegate<T>(isNullable: false);
        }
    }

    /// <summary>
    /// Caches a delegate to read the nullable version of <typeparamref name="T"/> to a <see cref="ByteReader"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Reader{T}"/>.</returns>
    /// <remarks>For nullable structs, you must use the nullable type as the type parameter.</remarks>
    /// <exception cref="AutoEncodeTypeNotFoundException">The nullable version of <typeparamref name="T"/> is not auto-readable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
    private static class NullableReaderHelper<T>
    {
        /// <summary>
        /// Caches a delegate to read <typeparamref name="T"/> to a <see cref="ByteReader"/>.
        /// </summary>
        /// <returns>A delegate of type <see cref="Reader{T}"/>.</returns>
        /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-readable.</exception>
        /// <exception cref="ByteEncoderException">There was an error trying to create a auto-read method.</exception>
        internal static Reader<T> Reader { get; }
        static NullableReaderHelper()
        {
            ByteEncoders.ThrowIfNotAutoType<T>();
            Reader = CreateReadMethodDelegate<T>(isNullable: true);
        }
    }
}