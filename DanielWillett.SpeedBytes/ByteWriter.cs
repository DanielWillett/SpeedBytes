using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DanielWillett.SpeedBytes;

/// <summary>
/// Fast encoding to a byte array of data. Also works with <see cref="System.IO.Stream"/>s.
/// </summary>
public class ByteWriter
{
    private const int GuidSize = 16;
    private static readonly bool IsBigEndian = !BitConverter.IsLittleEndian;
    private static Dictionary<Type, MethodInfo>? _nonNullableWriters;
    private static Dictionary<Type, MethodInfo>? _nullableWriters;
    private static readonly MethodInfo? WriteEnumMethod = typeof(ByteWriter).GetMethod(nameof(WriteEnum), BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo? WriteNullableEnumMethod = typeof(ByteWriter).GetMethod(nameof(WriteNullableEnum), BindingFlags.Instance | BindingFlags.NonPublic);
    private int _size;
    private int _maxSize;
    private byte[] _buffer;
    private bool _streamMode;
    private Stream? _stream;

    /// <summary>
    /// Event called for logging messages. Defaults to <see cref="Console.WriteLine(string)"/>.
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// Defines the behavior of a <see cref="ByteWriter"/> when it encounters an array, string, or other enumerable too long to write.
    /// </summary>
    public EnumerableOverflowMode EnumerableOverflowMode { get; set; } = EnumerableOverflowMode.Throw;

    /// <summary>
    /// Buffer representing all data being written. In stream mode this acts as a temporary buffer and can be ignored.
    /// </summary>
    public byte[] Buffer { get => _buffer; set => _buffer = value; }

    /// <summary>
    /// Optional stream replacement for the buffer.
    /// </summary>
    public Stream? Stream
    {
        get => _stream;
        set
        {
            if (value is not null)
            {
                if (!value.CanWrite)
                    throw new ArgumentException(Properties.Localization.GivenStreamCanNotWrite, nameof(value));
                _stream = value;
                _streamMode = true;
                _size = 0;
            }
            else
            {
                _stream = null;
                _streamMode = false;
                _size = 0;
            }
        }
    }

    /// <summary>
    /// Number of bytes written to the stream or buffer.
    /// </summary>
    public int Count { get => _size; set => _size = value; }

    /// <summary>
    /// Starting capacity of the buffer.
    /// </summary>
    public int BaseCapacity { get; }

    /// <summary>
    /// Create a <see cref="ByteWriter"/> with a starting capacity of <paramref name="capacity"/> bytes.
    /// </summary>
    public ByteWriter(int capacity = 0)
    {
        BaseCapacity = capacity;
        _buffer = BaseCapacity < 1 ? Array.Empty<byte>() : new byte[BaseCapacity];
    }
    private static void PrepareMethods()
    {
        _nonNullableWriters ??= new Dictionary<Type, MethodInfo>(44)
        {
            { typeof(int), GetMethod(typeof(int)) },
            { typeof(uint), GetMethod(typeof(uint)) },
            { typeof(byte), GetMethod(typeof(byte)) },
            { typeof(sbyte), GetMethod(typeof(sbyte)) },
            { typeof(bool), GetMethod(typeof(bool)) },
            { typeof(long), GetMethod(typeof(long)) },
            { typeof(ulong), GetMethod(typeof(ulong)) },
            { typeof(short), GetMethod(typeof(short)) },
            { typeof(ushort), GetMethod(typeof(ushort)) },
            { typeof(float), GetMethod(typeof(float)) },
            { typeof(decimal), GetMethod(typeof(decimal)) },
            { typeof(double), GetMethod(typeof(double)) },
            { typeof(char), GetMethod(typeof(char)) },
            { typeof(string), GetMethod(typeof(string)) },
#if NET5_0_OR_GREATER
            { typeof(Half), GetMethod(typeof(Half)) },
#endif
            { typeof(Type), GetMethod(typeof(Type)) },
            { typeof(Type[]), GetMethod(typeof(Type[])) },
            { typeof(DateTime), GetMethod(typeof(DateTime)) },
            { typeof(DateTimeOffset), GetMethod(typeof(DateTimeOffset)) },
            { typeof(TimeSpan), GetMethod(typeof(TimeSpan)) },
            { typeof(Guid), GetMethod(typeof(Guid)) },
            { typeof(Guid[]), GetMethod(typeof(Guid[])) },
            { typeof(DateTime[]), GetMethod(typeof(DateTime[])) },
            { typeof(DateTimeOffset[]), GetMethod(typeof(DateTimeOffset[])) },
            { typeof(byte[]), GetMethod(typeof(byte[])) },
            { typeof(sbyte[]), GetMethod(typeof(sbyte[])) },
            { typeof(int[]), GetMethod(typeof(int[])) },
            { typeof(uint[]), GetMethod(typeof(uint[])) },
            { typeof(bool[]), GetMethod(typeof(bool[])) },
            { typeof(BitArray), GetMethod(typeof(BitArray)) },
            { typeof(long[]), GetMethod(typeof(long[])) },
            { typeof(ulong[]), GetMethod(typeof(ulong[])) },
            { typeof(short[]), GetMethod(typeof(short[])) },
            { typeof(ushort[]), GetMethod(typeof(ushort[])) },
            { typeof(float[]), GetMethod(typeof(float[])) },
            { typeof(double[]), GetMethod(typeof(double[])) },
            { typeof(decimal[]), GetMethod(typeof(decimal[])) },
            { typeof(char[]), GetMethod(typeof(char[])) },
            { typeof(string[]), GetMethod(typeof(string[])) }
        };

        _nullableWriters ??= new Dictionary<Type, MethodInfo>(44)
        {
            { typeof(int?), GetNullableMethod(typeof(int?)) },
            { typeof(uint?), GetNullableMethod(typeof(uint?)) },
            { typeof(byte?), GetNullableMethod(typeof(byte?)) },
            { typeof(sbyte?), GetNullableMethod(typeof(sbyte?)) },
            { typeof(bool?), GetNullableMethod(typeof(bool?)) },
            { typeof(long?), GetNullableMethod(typeof(long?)) },
            { typeof(ulong?), GetNullableMethod(typeof(ulong?)) },
            { typeof(short?), GetNullableMethod(typeof(short?)) },
            { typeof(ushort?), GetNullableMethod(typeof(ushort?)) },
            { typeof(float?), GetNullableMethod(typeof(float?)) },
            { typeof(decimal?), GetNullableMethod(typeof(decimal?)) },
            { typeof(double?), GetNullableMethod(typeof(double?)) },
            { typeof(char?), GetNullableMethod(typeof(char?)) },
            { typeof(string), GetNullableMethod(typeof(string)) },
            { typeof(Type), GetMethod(typeof(Type)) },
            { typeof(Type[]), GetMethod(typeof(Type[])) },
            { typeof(DateTime?), GetNullableMethod(typeof(DateTime?)) },
            { typeof(DateTimeOffset?), GetNullableMethod(typeof(DateTimeOffset?)) },
            { typeof(TimeSpan?), GetNullableMethod(typeof(TimeSpan?)) },
            { typeof(Guid?), GetNullableMethod(typeof(Guid?)) },
#if NET5_0_OR_GREATER
            { typeof(Half?), GetNullableMethod(typeof(Half?)) },
#endif
            { typeof(Guid[]), GetNullableMethod(typeof(Guid[])) },
            { typeof(DateTime[]), GetNullableMethod(typeof(DateTime[])) },
            { typeof(DateTimeOffset[]), GetNullableMethod(typeof(DateTimeOffset[])) },
            { typeof(byte[]), GetNullableMethod(typeof(byte[])) },
            { typeof(sbyte[]), GetNullableMethod(typeof(sbyte[])) },
            { typeof(int[]), GetNullableMethod(typeof(int[])) },
            { typeof(uint[]), GetNullableMethod(typeof(uint[])) },
            { typeof(BitArray), GetNullableMethod(typeof(BitArray)) },
            { typeof(bool[]), GetNullableMethod(typeof(bool[])) },
            { typeof(long[]), GetNullableMethod(typeof(long[])) },
            { typeof(ulong[]), GetNullableMethod(typeof(ulong[])) },
            { typeof(short[]), GetNullableMethod(typeof(short[])) },
            { typeof(ushort[]), GetNullableMethod(typeof(ushort[])) },
            { typeof(float[]), GetNullableMethod(typeof(float[])) },
            { typeof(double[]), GetNullableMethod(typeof(double[])) },
            { typeof(decimal[]), GetNullableMethod(typeof(decimal[])) },
            { typeof(char[]), GetNullableMethod(typeof(char[])) },
            { typeof(string[]), GetNullableMethod(typeof(string[])) }
        };

        return;

        MethodInfo GetMethod(Type writeType) => typeof(ByteWriter).GetMethod("Write", BindingFlags.Instance | BindingFlags.Public, null, [ writeType ], null)
                                                ?? throw new MemberAccessException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, "Write(" + writeType.Name + " n)"));

        MethodInfo GetNullableMethod(Type writeType) => typeof(ByteWriter).GetMethod("WriteNullable", BindingFlags.Instance | BindingFlags.Public, null, [ writeType ], null)
                                                        ?? throw new MemberAccessException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, 
                                                           "WriteNullable(" + (writeType.IsGenericType ? writeType.GenericTypeArguments[0] : writeType).Name + "? n)"));
    }
    internal void Log(string msg)
    {
        if (OnLog == null)
            Console.WriteLine(msg);
        else
            OnLog.Invoke(msg);
    }
    internal static void AddWriterMethod<T>(Writer<T> reader)
    {
        if (_nonNullableWriters == null)
            PrepareMethods();
        _nonNullableWriters!.Add(typeof(T), reader.Method);
    }
    internal static void AddNullableWriterStructMethod<T>(Writer<T?> reader) where T : struct
    {
        if (_nullableWriters == null)
            PrepareMethods();
        _nullableWriters!.Add(typeof(T?), reader.Method);
    }
    internal static void AddNullableWriterClassMethod<T>(Writer<T?> reader) where T : class
    {
        if (_nullableWriters == null)
            PrepareMethods();
        _nullableWriters!.Add(typeof(T), reader.Method);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void CheckArrayLength(Type type, int inputLength, int maxLength, out int len)
    {
        len = inputLength;
        if (inputLength <= maxLength)
            return;

        len = maxLength;

        switch (EnumerableOverflowMode)
        {
            case EnumerableOverflowMode.Throw:
                throw new ArgumentOutOfRangeException("n",
                    string.Format(
                        Properties.Localization.ArrayTooLongToWrite,
                        type.Name,
                        inputLength,
                        maxLength
                    )
                );

            case EnumerableOverflowMode.Truncate:
                break;

            case EnumerableOverflowMode.LogAndWriteEmpty:
                len = 0;
                break;
        }

        Log(string.Format(
            Properties.Localization.ArrayTooLongToWrite,
            type.Name,
            inputLength,
            maxLength
        ));
        Log(new StackTrace(1).ToString());
    }

    /// <summary>
    /// Copies the buffer to a byte array.
    /// </summary>
    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public byte[] ToArray()
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterToArrayStreamModeNotSupported);

        byte[] rtn = new byte[_size];
        System.Buffer.BlockCopy(_buffer, 0, rtn, 0, _size);
        return rtn;
    }

    /// <summary>
    /// This can be more resource-intensive when used on a reusable writer as the buffer will have to be reallocated afterwards.
    /// </summary>
    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public ArraySegment<byte> ToArraySegmentAndFlush()
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterToArraySegmentStreamModeNotSupported);

        ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, 0, _size);
        _buffer = Array.Empty<byte>();
        _size = 0;
        return segment;
    }

    /// <summary>
    /// This can be dangerous if the data is used after the next time the writer is used.
    /// </summary>
    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public ArraySegment<byte> ToArraySegmentAndDontFlush()
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterToArraySegmentStreamModeNotSupported);

        return new ArraySegment<byte>(_buffer, 0, _size);
    }

    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public void ExtendBuffer(int newsize)
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterExtendBufferStreamModeNotSupported);

        ExtendBufferIntl(newsize);
    }

    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public void ExtendBufferFor(int byteCount)
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterExtendBufferStreamModeNotSupported);

        ExtendBufferIntl(byteCount + _size);
    }

    private void ExtendBufferIntl(int newSize)
    {
        if (newSize <= _buffer.Length)
            return;
        if (_size == 0)
            _buffer = new byte[newSize];
        else
        {
            byte[] old = _buffer;
            int sz2 = old.Length;
            int sz = sz2 + sz2 / 2;
            if (sz < newSize) sz = newSize;
            _buffer = new byte[sz];
            System.Buffer.BlockCopy(old, 0, _buffer, 0, _size);
        }
    }

    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    public void BackTrack(int position)
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterNavMethodsStreamModeNotSupported);
        if (position < 0)
            position = 0;
        if (position > _size)
            throw new ArgumentOutOfRangeException(nameof(position));
        if (position == _size)
            return;

        _maxSize = _size;
        _size = position;
    }

    /// <exception cref="NotSupportedException">Not supported in stream mode.</exception>
    /// <exception cref="InvalidOperationException">You must call <see cref="BackTrack"/> before calling <see cref="Return"/>.</exception>
    public void Return()
    {
        if (_streamMode)
            throw new NotSupportedException(Properties.Localization.ByteWriterNavMethodsStreamModeNotSupported);
        if (_maxSize == 0)
            throw new InvalidOperationException(Properties.Localization.ByteWriterReturnNotNavigating);

        if (_size < _maxSize)
            _size = _maxSize;
        _maxSize = 0;
    }

    /// <summary>
    /// Writes a struct directly as it's laid out in computer memory.
    /// </summary>
    /// <remarks>Use with caution, may not be consistant with structs that do not have an explicit layout. Does not take endianness into account.</remarks>
    public unsafe void WriteStruct<T>(in T value) where T : unmanaged
    {
        if (_streamMode)
        {
            int size = sizeof(T);
            if (_buffer.Length < size)
                _buffer = new byte[size];
            fixed (byte* ptr = _buffer)
            {
                *(T*)ptr = value;
            }
            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + sizeof(T);
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        fixed (byte* ptr = &_buffer[_size])
        {
            *(T*)ptr = value;
        }
        _size = newsize;
    }

    /// <summary>
    /// Write a block of <paramref name="count"/> bytes all initialized with a <paramref name="value"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
    public unsafe void WriteBlock(byte value, int count)
    {
        if (count == 0)
            return;
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (_streamMode)
        {
            if (_buffer == null || _buffer.Length < 1)
                _buffer = new byte[BaseCapacity <= 0 ? Math.Min(128, count) : 0];

            if (value != 0)
            {
                fixed (byte* ptr = _buffer)
                {
                    int c = Math.Min(count, _buffer.Length);
                    for (int i = 0; i < c; ++i)
                        ptr[i] = value;
                }
            }

            _size += count;
            while (count > 0)
            {
                int c = Math.Min(count, _buffer.Length);
                _stream!.Write(_buffer, 0, c);
                count -= c;
            }
            return;
        }
        int newsize = _size + count;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        if (value != 0)
        {
            fixed (byte* ptr = &_buffer[_size])
            {
                for (int i = 0; i < count; ++i)
                    ptr[i] = value;
            }
        }
        _size = newsize;
    }
    private static unsafe void Reverse(byte* litEndStrt, int size)
    {
        byte* stack = stackalloc byte[size];
        Unsafe.CopyBlock(stack, litEndStrt, (uint)size);
        for (int i = 0; i < size; i++)
            litEndStrt[i] = stack[size - i - 1];
    }
    private static void Reverse(byte[] litEndStrt, int index, int size)
    {
        Span<byte> stack = stackalloc byte[size];
        Unsafe.CopyBlock(ref stack[0], ref litEndStrt[index], (uint)size);
        for (int i = 0; i < size; i++)
            litEndStrt[i] = stack[size - i - 1];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void EndianCheck(byte* litEndStrt, int size)
    {
        if (size > 1 && IsBigEndian) Reverse(litEndStrt, size);
    }
    internal unsafe void WriteInternal<T>(T value) where T : unmanaged
    {
        int size = sizeof(T);
        if (size == 1)
        {
            if (_streamMode)
            {
                _stream!.WriteByte(*(byte*)&value);
                ++_size;
                return;
            }

            int newsize = _size + 1;
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);

            _buffer[_size] = *(byte*)&value;
            _size = newsize;
        }
        else
        {
            if (_streamMode)
            {
                if (_buffer.Length < size)
                    _buffer = new byte[size];
                fixed (byte* ptr = _buffer)
                {
                    *(T*)ptr = value;
                    EndianCheck(ptr, size);
                }
                _stream!.Write(_buffer, 0, size);
                _size += size;
                return;
            }
            int newsize = _size + size;
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);
            fixed (byte* ptr = &_buffer[_size])
            {
                *(T*)ptr = value;
                EndianCheck(ptr, size);
            }
            _size = newsize;
        }
    }
    private void WriteInternal<T>(T[] n) where T : unmanaged
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        WriteInternal<T>(n.AsSpan());
    }
    private unsafe void WriteInternal<T>(ReadOnlySpan<T> n) where T : unmanaged
    {
        int objSize = sizeof(T);
        CheckArrayLength(typeof(T), n.Length, ushort.MaxValue, out int len);
        if (_streamMode)
        {
            int size = sizeof(ushort) + objSize * len;
            if (_buffer.Length < size)
                _buffer = new byte[size];
            fixed (byte* ptr = _buffer)
            {
                *(ushort*)ptr = (ushort)len;
                EndianCheck(ptr, sizeof(ushort));
                for (int i = 0; i < len; ++i)
                {
                    byte* ptr2 = ptr + sizeof(ushort) + i * objSize;
                    *(T*)ptr2 = n[i];
                    EndianCheck(ptr2, objSize);
                }
            }
            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + sizeof(ushort) + objSize * len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        fixed (byte* ptr = &_buffer[_size])
        {
            *(ushort*)ptr = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            for (int i = 0; i < len; ++i)
            {
                byte* ptr2 = ptr + sizeof(ushort) + i * objSize;
                *(T*)ptr2 = n[i];
                EndianCheck(ptr2, objSize);
            }
        }
        _size = newsize;
    }

    /// <summary>
    /// Write a 32-bit integer value to the buffer.
    /// </summary>
    public void Write(int n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 32-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(int? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 32-bit integer value to the buffer.
    /// </summary>
    public void Write(uint n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable unsigned 32-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(uint? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 8-bit integer value to the buffer.
    /// </summary>
    public void Write(byte n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable unsigned 8-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(byte? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an 8-bit integer value to the buffer.
    /// </summary>
    public void Write(sbyte n) => WriteInternal(unchecked((byte)n));

    /// <summary>
    /// Write a nullable 8-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(sbyte? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(unchecked((byte)n.Value));
        }
        else Write(false);
    }

    /// <summary>
    /// Write a boolean value to the buffer.
    /// </summary>
    public void Write(bool n) => WriteInternal(n ? (byte)1 : (byte)0);

    /// <summary>
    /// Write a nullable boolean value to the buffer.
    /// </summary>
    public void WriteNullable(bool? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 64-bit integer value to the buffer.
    /// </summary>
    public void Write(long n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 64-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(long? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 64-bit integer value to the buffer.
    /// </summary>
    public void Write(ulong n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable unsigned 64-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(ulong? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 16-bit integer value to the buffer.
    /// </summary>
    public void Write(short n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 16-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(short? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 16-bit integer value to the buffer.
    /// </summary>
    public void Write(ushort n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable unsigned 16-bit integer value to the buffer.
    /// </summary>
    public void WriteNullable(ushort? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 24-bit integer value to the buffer.
    /// </summary>
    /// <remarks>Values over <see cref="ByteEncoders.Int24MaxValue"/> or under <see cref="ByteEncoders.Int24MinValue"/> will be clamped within the range.</remarks>
    public void WriteInt24(int n)
    {
        if (n > ByteEncoders.Int24MaxValue)
            n = ByteEncoders.Int24MaxValue;
        if (n < -ByteEncoders.Int24MaxValue)
            n = -ByteEncoders.Int24MaxValue;
        n += ByteEncoders.Int24MaxValue;
        // sign bit
        byte b = (byte)((n >> 16) & 0xFF);
        WriteInternal((ushort)(n & 0xFFFF));
        WriteInternal(b);
    }

    /// <summary>
    /// Write an unsigned 24-bit integer value to the buffer.
    /// </summary>
    /// <remarks>Values over double <see cref="ByteEncoders.Int24MaxValue"/> will be clamped within the range.</remarks>
    public void WriteUInt24(uint n)
    {
        if (n > ByteEncoders.Int24MaxValue)
        {
            if (n > ByteEncoders.Int24MaxValue * 2)
                n = ByteEncoders.Int24MaxValue * 2;
            WriteInt24((int)-(n - ByteEncoders.Int24MaxValue));
        }
        else
        {
            WriteInt24((int)n);
        }
    }

    /// <summary>
    /// Write a 32-bit floating point value to the buffer.
    /// </summary>
    public void Write(float n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 32-bit floating point value to the buffer.
    /// </summary>
    public void WriteNullable(float? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

#if NET5_0_OR_GREATER

    /// <summary>
    /// Write a 16-bit floating point value to the buffer.
    /// </summary>
    public void Write(Half n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 16-bit floating point value to the buffer.
    /// </summary>
    public void WriteNullable(Half? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }
#endif

    /// <summary>
    /// Write a 128-bit floating point value to the buffer.
    /// </summary>
    public void Write(decimal n)
    {
#if NET5_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(n, bits);
#else
        int[] bits = decimal.GetBits(n);
#endif
        const int size = 16;
        if (_streamMode)
        {
            if (_buffer.Length < size)
                _buffer = new byte[size];

            for (int i = 0; i < 4; ++i)
            {
                Unsafe.WriteUnaligned(ref _buffer[i * 4], bits[i]);
                if (IsBigEndian)
                    Reverse(_buffer, i * 4, sizeof(int));
            }

            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + size;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        for (int i = 0; i < 4; ++i)
        {
            Unsafe.WriteUnaligned(ref _buffer[_size + i * 4], bits[i]);
            if (IsBigEndian)
                Reverse(_buffer, _size + i * 4, sizeof(int));
        }
        _size = newsize;
    }

    /// <summary>
    /// Write a nullable 128-bit floating point value to the buffer.
    /// </summary>
    public void WriteNullable(decimal? n)
    {
        if (n.HasValue)
        {
            Write(true);
            Write(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 64-bit floating point value to the buffer.
    /// </summary>
    public void Write(double n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 64-bit floating point value to the buffer.
    /// </summary>
    public void WriteNullable(double? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 16-bit (UTF-16) character to the buffer.
    /// </summary>
    public void Write(char n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 16-bit (UTF-16) character to the buffer.
    /// </summary>
    public void WriteNullable(char? n)
    {
        if (n.HasValue)
        {
            Write(true);
            WriteInternal(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a UTF-8 string to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 UTF-8 bytes.</exception>
    public void Write(string n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a nullable UTF-8 string to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 UTF-8 bytes.</exception>
    public void WriteNullable(string? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a UTF-8 character array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 UTF-8 bytes.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(char[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a nullable UTF-8 character array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 UTF-8 bytes.</exception>
    public void WriteNullable(char[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a UTF-8 string to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 UTF-8 bytes.</exception>
    public unsafe void Write(ReadOnlySpan<char> n)
    {
        CheckArrayLength(typeof(char), n.Length, ushort.MaxValue, out int charCt);
        if (charCt == 0)
        {
            Write((byte)0);
            return;
        }
#if NETFRAMEWORK
        int ct;
        byte[] binary;
        fixed (char* ptr = n)
        {
            ct = Encoding.UTF8.GetByteCount(ptr, n.Length);
            CheckArrayLength(typeof(char), ct, ushort.MaxValue, out ct);
            if (ct == 0)
            {
                Write((byte)0);
                return;
            }
            binary = new byte[ct];
            fixed (byte* ptr2 = binary)
            {
                ct = Encoding.UTF8.GetBytes(ptr, n.Length, ptr2, ct);
            }
        }
#else
        int ct = Encoding.UTF8.GetByteCount(n);
        CheckArrayLength(typeof(char), ct, ushort.MaxValue, out ct);
        if (ct == 0)
        {
            Write((byte)0);
            return;
        }

#pragma warning disable CS9081
        Span<byte> binary;
        if (ct > 512)
            binary = new byte[ct];
        else
            binary = stackalloc byte[ct];
#pragma warning restore CS9081

        ct = Encoding.UTF8.GetBytes(n, binary);
        if (binary.Length != ct)
            binary = binary[..ct];
#endif

        WriteInternal((ushort)ct);
        if (_streamMode)
        {
#if NETFRAMEWORK
            _stream!.Write(binary, 0, ct);
#else
            _stream!.Write(binary);
#endif
            _size += ct;
            return;
        }
        int newsize = _size + ct;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
#if NETFRAMEWORK
        System.Buffer.BlockCopy(binary, 0, _buffer, _size, ct);
#else
        fixed (byte* bufPtr = &_buffer[_size])
        fixed (byte* charData = binary)
            System.Buffer.MemoryCopy(charData, bufPtr, ct, ct);
#endif
        _size = newsize;
    }

    /// <summary>
    /// Write a UTF-8 string span to the buffer with an unsigned 8-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 UTF-8 bytes.</exception>
    public void WriteShort(string n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        WriteShort(n.AsSpan());
    }

    /// <summary>
    /// Write a nullable UTF-8 string to the buffer with an unsigned 8-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 UTF-8 bytes.</exception>
    public void WriteNullableShort(string? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteShort(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a UTF-8 string to the buffer with an unsigned 8-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 UTF-8 bytes.</exception>
    public unsafe void WriteShort(ReadOnlySpan<char> n)
    {
        CheckArrayLength(typeof(char), n.Length, byte.MaxValue, out int charCt);
        if (charCt == 0)
        {
            Write((byte)0);
            return;
        }
#if NETFRAMEWORK
        int ct;
        byte[] binary;
        fixed (char* ptr = n)
        {
            ct = Encoding.UTF8.GetByteCount(ptr, n.Length);
            CheckArrayLength(typeof(char), ct, byte.MaxValue, out ct);
            if (ct == 0)
            {
                Write((byte)0);
                return;
            }
            binary = new byte[ct];
            fixed (byte* ptr2 = binary)
            {
                ct = Encoding.UTF8.GetBytes(ptr, n.Length, ptr2, ct);
            }
        }
#else
        int ct = Encoding.UTF8.GetByteCount(n);
        CheckArrayLength(typeof(char), ct, byte.MaxValue, out ct);
        if (ct == 0)
        {
            Write((byte)0);
            return;
        }

        Span<byte> binary = stackalloc byte[ct];

        ct = Encoding.UTF8.GetBytes(n, binary);
        if (binary.Length != ct)
            binary = binary[..ct];
#endif

        WriteInternal((byte)ct);
        if (_streamMode)
        {
#if NETFRAMEWORK
            _stream!.Write(binary, 0, ct);
#else
            _stream!.Write(binary);
#endif
            _size += ct;
            return;
        }
        int newsize = _size + ct;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
#if NETFRAMEWORK
        System.Buffer.BlockCopy(binary, 0, _buffer, _size, ct);
#else
        fixed (byte* bufPtr = &_buffer[_size])
        fixed (byte* charData = binary)
            System.Buffer.MemoryCopy(charData, bufPtr, ct, ct);
#endif
        _size = newsize;
    }

    /// <summary>
    /// Write an ASCII string span to the buffer with an unsigned 8-bit length header.
    /// </summary>
    /// <remarks>Max length (in ASCII bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 ASCII bytes.</exception>
    public void WriteShortAscii(string n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        WriteShort(n.AsSpan());
    }

    /// <summary>
    /// Write a nullable ASCII string to the buffer with an unsigned 8-bit length header.
    /// </summary>
    /// <remarks>Max length (in ASCII bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 ASCII bytes.</exception>
    public void WriteNullableShortAscii(string? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteShort(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write an ASCII string to the buffer with an unsigned 8-bit length header.
    /// </summary>
    /// <remarks>Max length (in ASCII bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 ASCII bytes.</exception>
    public unsafe void WriteShortAscii(ReadOnlySpan<char> n)
    {
        CheckArrayLength(typeof(char), n.Length, byte.MaxValue, out int charCt);
        if (charCt == 0)
        {
            Write((byte)0);
            return;
        }
#if NETFRAMEWORK
        int ct;
        byte[] binary;
        fixed (char* ptr = n)
        {
            ct = Encoding.ASCII.GetByteCount(ptr, n.Length);
            CheckArrayLength(typeof(char), ct, byte.MaxValue, out ct);
            if (ct == 0)
            {
                Write((byte)0);
                return;
            }
            binary = new byte[ct];
            fixed (byte* ptr2 = binary)
            {
                ct = Encoding.ASCII.GetBytes(ptr, n.Length, ptr2, ct);
            }
        }
#else
        int ct = Encoding.ASCII.GetByteCount(n);
        CheckArrayLength(typeof(char), ct, byte.MaxValue, out ct);
        if (ct == 0)
        {
            Write((byte)0);
            return;
        }

        Span<byte> binary = stackalloc byte[ct];

        ct = Encoding.ASCII.GetBytes(n, binary);
        if (binary.Length != ct)
            binary = binary[..ct];
#endif

        WriteInternal((byte)ct);
        if (_streamMode)
        {
#if NETFRAMEWORK
            _stream!.Write(binary, 0, ct);
#else
            _stream!.Write(binary);
#endif
            _size += ct;
            return;
        }
        int newsize = _size + ct;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
#if NETFRAMEWORK
        System.Buffer.BlockCopy(binary, 0, _buffer, _size, ct);
#else
        fixed (byte* bufPtr = &_buffer[_size])
        fixed (byte* charData = binary)
            System.Buffer.MemoryCopy(charData, bufPtr, ct, ct);
#endif
        _size = newsize;
    }

    /// <summary>
    /// Write an array of nullable CLR types to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(Type?[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a nullable array of nullable CLR types to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length (in UTF-8 bytes): 255.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 255 UTF-8 bytes.</exception>
    public void WriteNullableShort(Type?[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a span of nullable CLR types to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<Type?> n)
    {
        CheckArrayLength(typeof(Type), n.Length, ushort.MaxValue, out int len);
        WriteInternal((ushort)len);
        for (int i = 0; i < len; ++i)
            Write(n[i]);
    }

    /// <summary>
    /// Write a nullable CLR type to the buffer.
    /// </summary>
    public void Write(Type? type)
    {
        const string nsSystem = "System";
        Assembly? typeAssembly = type?.Assembly;
        if (type == null)
        {
            Write((byte)128);
            return;
        }

        byte flag = 0;

        string ns = type.Namespace ?? string.Empty;
        if (typeAssembly == ByteEncoders.MSCoreLib && ns.StartsWith(nsSystem, StringComparison.Ordinal))
        {
            flag |= 64;
            ns = ns.Length > nsSystem.Length ? ns.Substring(nsSystem.Length + 1) : string.Empty;
        }

        string str = type.FullName!.Substring(type.Namespace!.Length + 1);

        Write(flag);
        if (flag == 0)
            str += ", " + type.Assembly.GetName().Name;

        Write(ns.Length == 0 ? str : (ns + "." + str));
    }

    /// <summary>
    /// Write a <see cref="DateTime"/> to the buffer.
    /// </summary>
    public void Write(DateTime n) => WriteInternal(n.ToBinary());

    /// <summary>
    /// Write a nullable <see cref="DateTime"/> to the buffer.
    /// </summary>
    public void WriteNullable(DateTime? n)
    {
        if (n.HasValue)
        {
            Write(true);
            Write(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a <see cref="DateTimeOffset"/> to the buffer.
    /// </summary>
    public void Write(DateTimeOffset n)
    {
        WriteInternal(n.DateTime.ToBinary());
        Write((short)Math.Round(n.Offset.TotalMinutes));
    }

    /// <summary>
    /// Write a nullable <see cref="DateTimeOffset"/> to the buffer.
    /// </summary>
    public void WriteNullable(DateTimeOffset? n)
    {
        if (n.HasValue)
        {
            Write(true);
            Write(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a <see cref="TimeSpan"/> to the buffer.
    /// </summary>
    public void Write(TimeSpan n) => WriteInternal(n.Ticks);

    /// <summary>
    /// Write a nullable <see cref="TimeSpan"/> to the buffer.
    /// </summary>
    public void WriteNullable(TimeSpan? n)
    {
        if (n.HasValue)
        {
            Write(true);
            Write(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a <see cref="Guid"/> to the buffer.
    /// </summary>
    public void Write(Guid n)
    {
#if NETFRAMEWORK
        byte[] buffer = n.ToByteArray();
#else
        Span<byte> buffer = stackalloc byte[GuidSize];
        n.TryWriteBytes(buffer);
#endif
        if (IsBigEndian)
        {
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
        }

        WriteBlock(buffer);
    }

    /// <summary>
    /// Write a nullable <see cref="Guid"/> to the buffer.
    /// </summary>
    public void WriteNullable(Guid? n)
    {
        if (n.HasValue)
        {
            Write(true);
            Write(n.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a <see cref="Guid"/> array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(Guid[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a <see cref="Guid"/> span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public unsafe void Write(ReadOnlySpan<Guid> n)
    {
        CheckArrayLength(typeof(Guid), n.Length, ushort.MaxValue, out int len);
#if !NETFRAMEWORK
        Span<byte> guidBuffer = stackalloc byte[GuidSize];
#endif
        if (_streamMode)
        {
            int size = sizeof(ushort) + GuidSize * len;
            if (_buffer.Length < size)
                _buffer = new byte[size];
            fixed (byte* ptr = _buffer)
            {
                *(ushort*)ptr = (ushort)len;
                EndianCheck(ptr, sizeof(ushort));
                for (int i = 0; i < len; ++i)
                {
#if NETFRAMEWORK
                    byte[] guidBuffer =
#endif
                    WriteGuid(n, i
#if !NETFRAMEWORK
                        , guidBuffer
#endif
                    );
                    ref byte ptr2 = ref Unsafe.AsRef<byte>(ptr + sizeof(ushort) + i * GuidSize);
                    Unsafe.CopyBlock(ref ptr2, ref guidBuffer[0], GuidSize);
                }
            }
            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + sizeof(ushort) + GuidSize * len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        fixed (byte* ptr = &_buffer[_size])
        {
            *(ushort*)ptr = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            for (int i = 0; i < len; ++i)
            {
#if NETFRAMEWORK
                byte[] guidBuffer =
#endif
                    WriteGuid(n, i
#if !NETFRAMEWORK
                        , guidBuffer
#endif
                    );
                ref byte ptr2 = ref Unsafe.AsRef<byte>(ptr + sizeof(ushort) + i * GuidSize);
                Unsafe.CopyBlock(ref ptr2, ref guidBuffer[0], GuidSize);
            }
        }
        _size = newsize;
    }

    // ReSharper disable once RedundantUnsafeContext
    private unsafe
#if NETFRAMEWORK
        byte[]
#else
        void
#endif
        WriteGuid(ReadOnlySpan<Guid> n, int i
#if !NETFRAMEWORK
        , Span<byte> guidBuffer
#endif
    )
    {
#if NETFRAMEWORK
        byte[] guidBuffer = n[i].ToByteArray();
#elif NET8_0_OR_GREATER
        ref readonly Guid guid = ref n[i];
        guid.TryWriteBytes(guidBuffer);
#else
        Guid guid = n[i];
        guid.TryWriteBytes(guidBuffer);
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
        return guidBuffer;
#endif
    }

    /// <summary>
    /// Write a nullable <see cref="Guid"/> array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(Guid[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a <see cref="DateTime"/> array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(DateTime[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a <see cref="DateTime"/> span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public unsafe void Write(ReadOnlySpan<DateTime> n)
    {
        const int objSize = sizeof(long);
        CheckArrayLength(typeof(DateTime), n.Length, ushort.MaxValue, out int len);
        if (_streamMode)
        {
            int size = sizeof(ushort) + objSize * len;
            if (_buffer.Length < size)
                _buffer = new byte[size];
            fixed (byte* ptr = _buffer)
            {
                *(ushort*)ptr = (ushort)len;
                EndianCheck(ptr, sizeof(ushort));
                for (int i = 0; i < len; ++i)
                {
                    byte* ptr2 = ptr + sizeof(ushort) + i * objSize;
                    *(long*)ptr2 = n[i].ToBinary();
                    EndianCheck(ptr2, objSize);
                }
            }
            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + sizeof(ushort) + objSize * len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        fixed (byte* ptr = &_buffer[_size])
        {
            *(ushort*)ptr = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            for (int i = 0; i < len; ++i)
            {
                byte* ptr2 = ptr + sizeof(ushort) + i * objSize;
                *(long*)ptr2 = n[i].ToBinary();
                EndianCheck(ptr2, objSize);
            }
        }
        _size = newsize;
    }

    /// <summary>
    /// Write a nullable <see cref="DateTime"/> array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(DateTime[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a <see cref="DateTimeOffset"/> array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(DateTimeOffset[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a <see cref="DateTimeOffset"/> span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public unsafe void Write(ReadOnlySpan<DateTimeOffset> n)
    {
        const int objSize = sizeof(long) + sizeof(short);
        CheckArrayLength(typeof(DateTimeOffset), n.Length, ushort.MaxValue, out int len);
        if (_streamMode)
        {
            int size = sizeof(ushort) + objSize * len;
            if (_buffer.Length < size)
                _buffer = new byte[size];
            fixed (byte* ptr = _buffer)
            {
                *(ushort*)ptr = (ushort)len;
                EndianCheck(ptr, sizeof(ushort));
                for (int i = 0; i < len; ++i)
                {
                    byte* ptr2 = ptr + sizeof(ushort) + i * objSize;
                    DateTimeOffset dt = n[i];
                    *(long*)ptr2 = dt.DateTime.ToBinary();
                    *(short*)(ptr2 + sizeof(long)) = (short)Math.Round(dt.Offset.TotalMinutes);
                    EndianCheck(ptr2, sizeof(long));
                    EndianCheck(ptr2 + sizeof(long), sizeof(short));
                }
            }
            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + sizeof(ushort) + objSize * len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        fixed (byte* ptr = &_buffer[_size])
        {
            *(ushort*)ptr = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            for (int i = 0; i < len; ++i)
            {
                byte* ptr2 = ptr + sizeof(ushort) + i * objSize;
                DateTimeOffset dt = n[i];
                *(long*)ptr2 = dt.DateTime.ToBinary();
                *(short*)(ptr2 + sizeof(long)) = (short)Math.Round(dt.Offset.TotalMinutes);
                EndianCheck(ptr2, sizeof(long));
                EndianCheck(ptr2 + sizeof(long), sizeof(short));
            }
        }
        _size = newsize;
    }

    /// <summary>
    /// Write a nullable <see cref="DateTimeOffset"/> array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(DateTimeOffset[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n);
        }
        else Write(false);
    }

#pragma warning disable CS1574
    /// <summary>
    /// Write an enum to the buffer as it's underlying type. Alias of <see cref="Write{TEnum}"/>.
    /// </summary>
    /// <remarks>Changing the underlying type of <typeparamref name="TEnum"/> can corrupt up data saved with this method, consider casting to an integer value type instead.</remarks>
    private void WriteEnum<TEnum>(TEnum o) where TEnum : unmanaged, Enum => WriteInternal(o);
#pragma warning restore CS1574

    /// <summary>
    /// Write a nullable enum to the buffer as it's underlying type. Alias of <see cref="WriteNullable{TEnum}"/>.
    /// </summary>
    /// <remarks>Changing the underlying type of <typeparamref name="TEnum"/> can corrupt up data saved with this method, consider casting to an integer value type instead.</remarks>
    private void WriteNullableEnum<TEnum>(TEnum? o) where TEnum : unmanaged, Enum
    {
        if (o.HasValue)
        {
            Write(true);
            Write(o.Value);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an enum to the buffer as it's underlying type. Alias of <see cref="WriteEnum{TEnum}"/>.
    /// </summary>
    /// <remarks>Changing the underlying type of <typeparamref name="TEnum"/> can corrupt up data saved with this method, consider casting to an integer value type instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<TEnum>(TEnum o) where TEnum : unmanaged, Enum => WriteEnum(o);

    /// <summary>
    /// Write a nullable enum to the buffer as it's underlying type. Alias of <see cref="WriteNullableEnum{TEnum}"/>.
    /// </summary>
    /// <remarks>Changing the underlying type of <typeparamref name="TEnum"/> can corrupt up data saved with this method, consider casting to an integer value type instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNullable<TEnum>(TEnum? n) where TEnum : unmanaged, Enum => WriteNullableEnum(n);

    /// <summary>
    /// Write a byte array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(byte[] n)
#if !NETFRAMEWORK
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }
#else
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));
        CheckArrayLength(typeof(byte), n.Length, ushort.MaxValue, out int len);
        if (_streamMode)
        {
            WriteInternal((ushort)len);
            _stream!.Write(n, 0, len);
            _size += len;
            return;
        }
        int newsize = _size + sizeof(ushort) + len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        unsafe
        {
            fixed (byte* ptr = &_buffer[_size])
            {
                *(ushort*)ptr = (ushort)len;
                EndianCheck(ptr, sizeof(ushort));
            }
        }
        System.Buffer.BlockCopy(n, 0, _buffer, _size + sizeof(ushort), len);
        _size = newsize;
    }
#endif

    /// <summary>
    /// Write a byte span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.<br/>Not recommended to use this method with a stream when using the <see langword="net461"/> target for this library, as the span has to be copied.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public unsafe void Write(ReadOnlySpan<byte> n)
    {
        CheckArrayLength(typeof(byte), n.Length, ushort.MaxValue, out int len);
        n = n.Slice(0, len);
        if (_streamMode)
        {
            WriteInternal((ushort)len);
#if NETFRAMEWORK
            byte[] arr = n.ToArray();
            _stream!.Write(arr, 0, len);
#else
            _stream!.Write(n);
#endif
            _size += len;
            return;
        }
        int newsize = _size + sizeof(ushort) + len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        
        fixed (byte* ptr = &_buffer[_size])
        {
            *(ushort*)ptr = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            fixed (byte* srcPtr = n)
            {
                System.Buffer.MemoryCopy(srcPtr, ptr + sizeof(ushort), len, len);
            }
        }

        _size = newsize;
    }

    /// <summary>
    /// Write a nullable byte array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(byte[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a byte array to the buffer with a 32-bit length header.
    /// </summary>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void WriteLong(byte[] n)
#if !NETFRAMEWORK
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        WriteLong(n.AsSpan());
    }
#else
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));
        int len = n.Length;
        if (_streamMode)
        {
            WriteInternal(len);
            _stream!.Write(n, 0, len);
            _size += len;
            return;
        }
        int newsize = _size + sizeof(int) + len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        unsafe
        {
            fixed (byte* ptr = &_buffer[_size])
            {
                *(int*)ptr = len;
                EndianCheck(ptr, sizeof(int));
            }
        }
        System.Buffer.BlockCopy(n, 0, _buffer, _size + sizeof(int), len);
        _size = newsize;
    }
#endif

    /// <summary>
    /// Write a byte span to the buffer with a 32-bit length header.
    /// </summary>
    /// <remarks>Not recommended to use this method with a stream when using the <see langword="net461"/> target for this library, as the span has to be copied.</remarks>
    public unsafe void WriteLong(ReadOnlySpan<byte> n)
    {
        int len = n.Length;
        if (_streamMode)
        {
            WriteInternal(len);
#if NETFRAMEWORK
            byte[] arr = n.ToArray();
            _stream!.Write(arr, 0, len);
#else
            _stream!.Write(n);
#endif
            _size += len;
            return;
        }
        int newsize = _size + sizeof(int) + len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);

        fixed (byte* ptr = &_buffer[_size])
        {
            *(int*)ptr = len;
            EndianCheck(ptr, sizeof(int));
            fixed (byte* srcPtr = n)
            {
                System.Buffer.MemoryCopy(srcPtr, ptr + sizeof(int), len, len);
            }
        }

        _size = newsize;
    }

    /// <summary>
    /// Write a nullable byte array to the buffer with a 32-bit length header.
    /// </summary>
    public void WriteNullableLong(byte[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n);
        }
        else Write(false);
    }

    /// <summary>
    /// Write an array of bytes to the buffer.
    /// </summary>
    /// <remarks>This method does not write length, the calling method should handle that itself.</remarks>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void WriteBlock(byte[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));
        if (n.Length == 0)
            return;
        if (_streamMode)
        {
            _stream!.Write(n, 0, n.Length);
            _size += n.Length;
            return;
        }
        int newsize = _size + n.Length;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        System.Buffer.BlockCopy(n, 0, _buffer, _size, n.Length);
        _size = newsize;
    }

    /// <summary>
    /// Write an array of bytes to the buffer.
    /// </summary>
    /// <remarks>This method does not write length, the calling method should handle that itself.</remarks>
    /// <exception cref="ArgumentNullException">The array segment does not have a valid array.</exception>
    public void WriteBlock(ArraySegment<byte> n)
        => WriteBlock(n.Array ?? throw new ArgumentNullException(nameof(n)), n.Offset, n.Count);

    /// <summary>
    /// Write an array of bytes to the buffer.
    /// </summary>
    /// <remarks>This method does not write length, the calling method should handle that itself.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The given index and count do not fit in the array.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void WriteBlock(byte[] n, int index, int count = -1)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));
        if (index < 0)
            index = 0;
        if (count == -1)
            count = n.Length - index;
        if (index + count > n.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (count == 0)
            return;
        if (_streamMode)
        {
            _stream!.Write(n, index, count);
            _size += n.Length;
            return;
        }
        int newsize = _size + count;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        System.Buffer.BlockCopy(n, index, _buffer, _size, count);
        _size = newsize;
    }

    /// <summary>
    /// Write a span of bytes to the buffer.
    /// </summary>
    /// <remarks>
    /// This method does not write length, the calling method should handle that itself.<br/>
    /// Not recommended to use this method with a stream when using the <see langword="net461"/> target for this library, as the span has to be copied.
    /// </remarks>
    public void WriteBlock(ReadOnlySpan<byte> n)
    {
        if (n.Length == 0)
            return;
        if (_streamMode)
        {
#if NETFRAMEWORK
            byte[] arr = n.ToArray();
            _stream!.Write(arr, 0, n.Length);
#else
            _stream!.Write(n);
#endif
            _size += n.Length;
            return;
        }
        int newsize = _size + n.Length;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);

        n.TryCopyTo(_buffer.AsSpan(_size));
        _size = newsize;
    }

    /// <summary>
    /// If in stream mode, flushes the underlying stream, otherwise clears the buffer and resets the position to zero.
    /// </summary>
    public void Flush()
    {
        if (_streamMode)
        {
            _stream!.Flush();
        }
        else if (_buffer.Length != 0)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _size = 0;
        }
    }

    /// <summary>
    /// Write a 32-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(int[] n) => WriteInternal(n);

    /// <summary>
    /// Write a 32-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<int> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 32-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(int[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<int>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 32-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(uint[] n) => WriteInternal(n);

    /// <summary>
    /// Write an unsigned 32-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<uint> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable unsigned 32-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(uint[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<uint>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write an 8-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(sbyte[] n) => WriteInternal(n);

    /// <summary>
    /// Write an 8-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<sbyte> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 8-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(sbyte[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<sbyte>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a packed boolean array to the buffer with an unsigned 16-bit length header. Each element is represented by one bit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(bool[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a packed boolean span to the buffer with an unsigned 16-bit length header. Each element is represented by one bit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public unsafe void Write(ReadOnlySpan<bool> n)
    {
        CheckArrayLength(typeof(bool), n.Length, ushort.MaxValue, out int len);

        int size;
        if (!_streamMode)
        {
            int newsize = _size + (len - 1) / 8 + 1 + sizeof(ushort);
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);
            size = newsize;
        }
        else
        {
            size = (len - 1) / 8 + 1 + sizeof(ushort);
            if (_buffer.Length < size) _buffer = new byte[size];
        }

        fixed (byte* ptr = _buffer)
        {
            byte* ptr2 = _streamMode ? ptr : ptr + _size;
            *(ushort*)ptr2 = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            ptr2 += sizeof(ushort);
            byte current = 0;
            int cutoff = len - 1;
            for (int i = 0; i < len; i++)
            {
                bool c = n[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *ptr2 = current;
                    ptr2++;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
                if (i == cutoff)
                    *ptr2 = current;
            }
        }

        if (!_streamMode)
            _size = size;
        else
        {
            _stream!.Write(_buffer, 0, size);
            _size += size;
        }
    }

    /// <summary>
    /// Write a packed bit array to the buffer with an unsigned 16-bit length header. Each element is represented by one bit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given <see cref="BitArray"/> is null.</exception>
    public unsafe void Write(BitArray n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        CheckArrayLength(typeof(bool), n.Length, ushort.MaxValue, out int len);

        int size;
        if (!_streamMode)
        {
            int newsize = _size + (len - 1) / 8 + 1 + sizeof(ushort);
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);
            size = newsize;
        }
        else
        {
            size = (len - 1) / 8 + 1 + sizeof(ushort);
            if (_buffer.Length < size) _buffer = new byte[size];
        }

        fixed (byte* ptr = _buffer)
        {
            byte* ptr2 = _streamMode ? ptr : ptr + _size;
            *(ushort*)ptr2 = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
        }

        n.CopyTo(_buffer, !_streamMode ? sizeof(ushort) + _size : sizeof(ushort));

        if (!_streamMode)
            _size = size;
        else
        {
            _stream!.Write(_buffer, 0, size);
            _size += size;
        }
    }

    /// <summary>
    /// Write a packed nullable bit array to the buffer with an unsigned 16-bit length header. Each element is represented by one bit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>=
    public void WriteNullable(BitArray? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n);
        }
        else Write(false);
    }

    /// <summary>
    /// Write a packed boolean span to the buffer with a 32-bit length header. Each element is represented by one bit.
    /// </summary>
    public void WriteLong(bool[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        WriteLong(n.AsSpan());
    }

    /// <summary>
    /// Write a packed boolean span to the buffer with a 32-bit length header. Each element is represented by one bit.
    /// </summary>
    public unsafe void WriteLong(ReadOnlySpan<bool> n)
    {
        int len = n.Length;
        int size;
        if (!_streamMode)
        {
            int newsize = _size + (len - 1) / 8 + 1 + sizeof(int);
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);
            size = newsize;
        }
        else
        {
            size = (len - 1) / 8 + 1 + sizeof(int);
            if (_buffer.Length < size) _buffer = new byte[size];
        }

        fixed (byte* ptr = _buffer)
        {
            byte* ptr2 = _streamMode ? ptr : ptr + _size;
            *(int*)ptr2 = len;
            EndianCheck(ptr, sizeof(int));
            ptr2 += sizeof(int);
            byte current = 0;
            int cutoff = len - 1;
            for (int i = 0; i < len; i++)
            {
                bool c = n[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *ptr2 = current;
                    ptr2++;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
                if (i == cutoff)
                    *ptr2 = current;
            }
        }

        if (!_streamMode)
            _size = size;
        else
        {
            _stream!.Write(_buffer, 0, size);
            _size += size;
        }
    }

    /// <summary>
    /// Write a nullable packed boolean array to the buffer with an unsigned 16-bit length header. Each element is represented by one bit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(bool[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a packed bit array to the buffer with a 32-bit length header. Each element is represented by one bit.
    /// </summary>
    /// <exception cref="ArgumentNullException">The given <see cref="BitArray"/> is null.</exception>
    public unsafe void WriteLong(BitArray n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        int len = n.Length;
        int size;
        if (!_streamMode)
        {
            int newsize = _size + (len - 1) / 8 + 1 + sizeof(int);
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);
            size = newsize;
        }
        else
        {
            size = (len - 1) / 8 + 1 + sizeof(int);
            if (_buffer.Length < size) _buffer = new byte[size];
        }

        fixed (byte* ptr = _buffer)
        {
            byte* ptr2 = _streamMode ? ptr : ptr + _size;
            *(int*)ptr2 = len;
            EndianCheck(ptr, sizeof(int));
        }

        n.CopyTo(_buffer, !_streamMode ? sizeof(int) + _size : sizeof(int));

        if (!_streamMode)
            _size = size;
        else
        {
            _stream!.Write(_buffer, 0, size);
            _size += size;
        }
    }

    /// <summary>
    /// Write a 64-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(long[] n) => WriteInternal(n);

    /// <summary>
    /// Write a 64-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<long> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 64-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(long[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<long>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 64-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(ulong[] n) => WriteInternal(n);

    /// <summary>
    /// Write an unsigned 64-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<ulong> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable unsigned 64-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(ulong[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<ulong>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 16-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(short[] n) => WriteInternal(n);

    /// <summary>
    /// Write a 16-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<short> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 16-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(short[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<short>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write an unsigned 16-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(ushort[] n) => WriteInternal(n);

    /// <summary>
    /// Write an unsigned 16-bit integer span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<ushort> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 16-bit integer array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(ushort[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<ushort>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 32-bit floating point array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(float[] n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 32-bit floating point array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(float[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<float>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 128-bit floating point array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(decimal[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }

    /// <summary>
    /// Write a 128-bit floating point span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public unsafe void Write(ReadOnlySpan<decimal> n)
    {
        const int objSize = 16;
        CheckArrayLength(typeof(DateTimeOffset), n.Length, ushort.MaxValue, out int len);
#if NET5_0_OR_GREATER
        Span<int> bitBuffer = stackalloc int[4];
#endif
        if (_streamMode)
        {
            int size = sizeof(ushort) + objSize * len;
            if (_buffer.Length < size)
                _buffer = new byte[size];
            fixed (byte* ptr = _buffer)
            {
                *(ushort*)ptr = (ushort)len;
                EndianCheck(ptr, sizeof(ushort));
                for (int i = 0; i < len; ++i)
                {
                    decimal d = n[i];
#if !NET5_0_OR_GREATER
                    int[] bitBuffer = decimal.GetBits(d);
#else
                    decimal.GetBits(d, bitBuffer);
#endif
                    for (int j = 0; j < 4; ++j)
                    {
                        int index = sizeof(ushort) + i * objSize + j * 4;
                        Unsafe.WriteUnaligned(ptr + index, bitBuffer[j]);
                        if (IsBigEndian)
                            Reverse(_buffer, index, sizeof(int));
                    }
                }
            }
            _stream!.Write(_buffer, 0, size);
            _size += size;
            return;
        }
        int newsize = _size + sizeof(ushort) + objSize * len;
        if (newsize > _buffer.Length)
            ExtendBufferIntl(newsize);
        fixed (byte* ptr = &_buffer[_size])
        {
            *(ushort*)ptr = (ushort)len;
            EndianCheck(ptr, sizeof(ushort));
            for (int i = 0; i < len; ++i)
            {
                decimal d = n[i];
#if !NET5_0_OR_GREATER
                int[] bitBuffer = decimal.GetBits(d);
#else
                decimal.GetBits(d, bitBuffer);
#endif
                for (int j = 0; j < 4; ++j)
                {
                    int index = sizeof(ushort) + i * objSize + j * 4;
                    Unsafe.WriteUnaligned(ptr + index, bitBuffer[j]);
                    if (IsBigEndian)
                        Reverse(_buffer, index, sizeof(int));
                }
            }
        }
        _size = newsize;
    }

    /// <summary>
    /// Write a nullable 128-bit floating point array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(decimal[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<decimal>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a 64-bit floating point array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    public void Write(double[] n) => WriteInternal(n);

    /// <summary>
    /// Write a 64-bit floating point span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void Write(ReadOnlySpan<double> n) => WriteInternal(n);

    /// <summary>
    /// Write a nullable 64-bit floating point array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    public void WriteNullable(double[]? n)
    {
        if (n is not null)
        {
            Write(true);
            WriteInternal<double>(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Write a string array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentNullException">The given array is null.</exception>
    /// <exception cref="ArgumentException">One or more of the strings in the array is null.</exception>
    public void Write(string[] n)
    {
        if (n == null)
            throw new ArgumentNullException(nameof(n));

        Write(n.AsSpan());
    }
#pragma warning disable CS9081

    /// <summary>
    /// Write a string span to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentException">One or more of the strings in the span is null.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public unsafe void Write(ReadOnlySpan<string> n)
    {
        CheckArrayLength(typeof(string), n.Length, ushort.MaxValue, out int len);
        Span<ushort> byteCounts;

        if (len > 128)
            byteCounts = new ushort[len];
        else
            byteCounts = stackalloc ushort[len];

        int maxLen = 0;
        for (int i = 0; i < len; ++i)
        {
            string? str = n[i];
            if (str == null)
                throw new ArgumentException(Properties.Localization.ElementInArrayIsNull, nameof(n));
            int ct = Encoding.UTF8.GetByteCount(str);
            if (ct > ushort.MaxValue)
            {
                string type = nameof(String) + "[" + i + "]";
                int newCt = EnumerableOverflowMode switch
                {
                    EnumerableOverflowMode.Truncate => ushort.MaxValue,
                    EnumerableOverflowMode.LogAndWriteEmpty => 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(n),
                            string.Format(
                                Properties.Localization.ArrayTooLongToWrite,
                                type,
                                ct,
                                ushort.MaxValue)
                        )
                };

                Log(string.Format(
                    Properties.Localization.ArrayTooLongToWrite,
                    type,
                    ct,
                    ushort.MaxValue
                ));
                Log(new StackTrace().ToString());

                ct = newCt;
            }

            byteCounts[i] = (ushort)ct;
            if (ct > maxLen)
                maxLen = ct;
        }

        WriteInternal((ushort)len);
#if NETFRAMEWORK
        byte[] charBuffer = new byte[maxLen];
#else
        Span<byte> charBuffer;
        if (maxLen > 512)
            charBuffer = new byte[maxLen];
        else
            charBuffer = stackalloc byte[maxLen];
#endif
        for (int i = 0; i < len; i++)
        {
            string str = n[i];
            int byteCt = byteCounts[i];
#if NETFRAMEWORK
            fixed (byte* ptr = charBuffer)
            fixed (char* strPtr = str)
                byteCt = Encoding.UTF8.GetBytes(strPtr, str.Length, ptr, byteCt);
#else
            byteCt = Encoding.UTF8.GetBytes(str, charBuffer[..byteCt]);
#endif
            WriteInternal((ushort)byteCt);
            if (_streamMode)
            {
#if NETFRAMEWORK
                _stream!.Write(charBuffer, 0, byteCt);
#else
                _stream!.Write(charBuffer[..byteCt]);
#endif
                _size += byteCt;
                continue;
            }
            int newsize = _size + byteCt;
            if (newsize > _buffer.Length)
                ExtendBufferIntl(newsize);

#if NETFRAMEWORK
            System.Buffer.BlockCopy(charBuffer, 0, _buffer, _size, byteCt);
#else
            fixed (byte* bufPtr = &_buffer[_size])
            fixed (byte* charData = charBuffer)
                System.Buffer.MemoryCopy(charData, bufPtr, byteCt, byteCt);
#endif
            _size = newsize;
        }
    }
#pragma warning restore CS9081

    /// <summary>
    /// Write a nullable string array to the buffer with an unsigned 16-bit length header.
    /// </summary>
    /// <remarks>Max length: 65535.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is longer than 65535 elements.</exception>
    /// <exception cref="ArgumentException">One or more of the strings in the span is null.</exception>
    public void WriteNullable(string[]? n)
    {
        if (n is not null)
        {
            Write(true);
            Write(n.AsSpan());
        }
        else Write(false);
    }

    /// <summary>
    /// Creates or gets a cached delegate to write <typeparamref name="T"/> to a <see cref="ByteWriter"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Writer{T}"/>.</returns>
    /// <remarks>If <paramref name="isNullable"/> is true, nullable value type structs should be indicated with <typeparamref name="T"/> being the nullable type.</remarks>
    /// <param name="isNullable">Should the type be considered nullable?</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> (marked nullable by <paramref name="isNullable"/>) is not auto-writable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
    public static Writer<T> GetWriteMethodDelegate<T>(bool isNullable = false)
    {
        try
        {
            return isNullable ? NullableWriterHelper<T>.Writer : WriterHelper<T>.Writer;
        }
        catch (TypeInitializationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Creates a delegate to write <paramref name="type"/> to a <see cref="ByteWriter"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Writer{T}"/>.</returns>
    /// <remarks>If <paramref name="isNullable"/> is true, nullable value type structs should be indicated with <paramref name="type"/> being the nullable type.</remarks>
    /// <param name="isNullable">Should the type be considered nullable?</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="AutoEncodeTypeNotFoundException"><paramref name="type"/> (marked nullable by <paramref name="isNullable"/>) is not auto-writable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
    public static Delegate CreateWriteMethodDelegate(Type type, bool isNullable = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        MethodInfo? method = GetWriteMethod(type, isNullable);
        if (method == null)
            throw new AutoEncodeTypeNotFoundException(type);

        try
        {
            return method.CreateDelegate(typeof(Writer<>).MakeGenericType(type));
        }
        catch (ArgumentException ex)
        {
            throw new ByteEncoderException(Properties.Localization.CouldNotCreateEncoderMethod, ex);
        }
    }

    internal static Writer<T1> CreateWriteMethodDelegate<T1>(bool isNullable = false) => (Writer<T1>)CreateWriteMethodDelegate(typeof(T1), isNullable);

    /// <summary>
    /// Get the <see cref="MethodInfo"/> of a method to write <paramref name="type"/> to a <see cref="ByteWriter"/>, or <see langword="null"/> if it's not found.
    /// </summary>
    /// <remarks>If <paramref name="isNullable"/> is true, nullable value type structs should be indicated with <paramref name="type"/> being the nullable type.</remarks>
    /// <param name="isNullable">Should the type be considered nullable?</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
    public static MethodInfo? GetWriteMethod(Type type, bool isNullable = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        MethodInfo? method;
        if (type.IsEnum)
        {
            method = (isNullable ? WriteNullableEnumMethod : WriteEnumMethod)?.MakeGenericMethod(type)
                ?? throw new MemberAccessException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, isNullable ? "WriteNullableEnum" : "WriteEnum"));
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GenericTypeArguments[0].IsEnum)
        {
            method = (isNullable ? WriteNullableEnumMethod : WriteEnumMethod)?.MakeGenericMethod(type.GenericTypeArguments[0])
                     ?? throw new MemberAccessException(string.Format(Properties.Localization.AutoEncodeMethodNotFoundCheckReflection, isNullable ? "WriteNullableEnum" : "WriteEnum"));
        }
        else
        {
            lock (ByteEncoders.AutoReadSync)
            {
                if (_nullableWriters == null || _nonNullableWriters == null)
                {
                    PrepareMethods();
                }
                if (isNullable)
                {
                    _nullableWriters!.TryGetValue(type, out method);
                }
                else if (!_nonNullableWriters!.TryGetValue(type, out method) && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    _nullableWriters!.TryGetValue(type, out method);
            }
        }

        return method;
    }

    /// <summary>
    /// Caches a delegate to write <typeparamref name="T"/> to a <see cref="ByteWriter"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Writer{T}"/>.</returns>
    /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-writable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
    private static class WriterHelper<T>
    {
        /// <summary>
        /// Caches a delegate to write <typeparamref name="T"/> to a <see cref="ByteWriter"/>.
        /// </summary>
        /// <returns>A delegate of type <see cref="Writer{T}"/>.</returns>
        /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-writable.</exception>
        /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
        internal static Writer<T> Writer { get; }
        static WriterHelper()
        {
            ByteEncoders.ThrowIfNotAutoType<T>();
            Writer = CreateWriteMethodDelegate<T>(isNullable: false);
        }
    }

    /// <summary>
    /// Caches a delegate to write the nullable version of <typeparamref name="T"/> to a <see cref="ByteWriter"/>.
    /// </summary>
    /// <returns>A delegate of type <see cref="Writer{T}"/>.</returns>
    /// <remarks>For nullable structs, you must use the nullable type as the type parameter.</remarks>
    /// <exception cref="AutoEncodeTypeNotFoundException">The nullable version of <typeparamref name="T"/> is not auto-writable.</exception>
    /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
    private static class NullableWriterHelper<T>
    {
        /// <summary>
        /// Caches a delegate to write <typeparamref name="T"/> to a <see cref="ByteWriter"/>.
        /// </summary>
        /// <returns>A delegate of type <see cref="Writer{T}"/>.</returns>
        /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-writable.</exception>
        /// <exception cref="ByteEncoderException">There was an error trying to create a auto-write method.</exception>
        internal static Writer<T> Writer { get; }
        static NullableWriterHelper()
        {
            ByteEncoders.ThrowIfNotAutoType<T>();
            Writer = CreateWriteMethodDelegate<T>(isNullable: true);
        }
    }
}