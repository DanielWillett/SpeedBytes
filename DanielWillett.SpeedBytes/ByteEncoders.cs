using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.SpeedBytes;

/// <summary>
/// Utility methods relating to <see cref="ByteReader"/> and <see cref="ByteWriter"/>.
/// </summary>
public static class ByteEncoders
{
    internal static readonly Assembly MSCoreLib = typeof(object).Assembly;
    internal static readonly object AutoReadSync = new object();
    internal static readonly List<Type> ValidTypes =
    [
        typeof(ulong), typeof(float), typeof(long), typeof(ushort), typeof(short), typeof(byte), typeof(int), typeof(uint), typeof(bool), typeof(char), typeof(sbyte), typeof(double),
        typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(Type),
        typeof(ulong?), typeof(float?), typeof(long?), typeof(ushort?), typeof(short?), typeof(byte?), typeof(int?), typeof(uint?), typeof(bool?), typeof(char?), typeof(sbyte?),
        typeof(double?), typeof(decimal?), typeof(DateTime?), typeof(DateTimeOffset?), typeof(TimeSpan?), typeof(Guid?),
#if NET5_0_OR_GREATER
        typeof(Half), typeof(Half?)
#endif
    ];
    internal static readonly List<Type> ValidArrayTypes =
    [
        typeof(ulong), typeof(float), typeof(long), typeof(ushort), typeof(short), typeof(byte), typeof(int), typeof(uint), typeof(bool), typeof(sbyte), typeof(decimal), typeof(char),
        typeof(double), typeof(string), typeof(DateTime), typeof(DateTimeOffset), typeof(Guid), typeof(Type),
#if NET5_0_OR_GREATER
        typeof(Half), typeof(Half?)
#endif

    ];

    /// <summary>
    /// Max value of an int24 (used with <see cref="ByteWriter.WriteInt24"/> and <see cref="ByteReader.ReadInt24"/>).
    /// </summary>
    /// <remarks>Minimum is just this but negative.</remarks>
    public const int Int24MaxValue = 8388607;

    /// <summary>
    /// Min value of an int24 (used with <see cref="ByteWriter.WriteInt24"/> and <see cref="ByteReader.ReadInt24"/>).
    /// </summary>
    /// <remarks>Maximum is just this but positive.</remarks>
    public const int Int24MinValue = -Int24MaxValue;

    /// <summary>
    /// A list of all (non-array) types that can be automatically read and written by <see cref="ByteReader"/> and <see cref="ByteWriter"/>. 
    /// </summary>
    /// <remarks>Includes nullable types as separate entries. Enums are not included but are all also valid.</remarks>
    public static IReadOnlyList<Type> ValidReadWriteTypes { get; } = new ReadOnlyCollection<Type>(ValidTypes);

    /// <summary>
    /// A list of all array types that can be automatically read and written by <see cref="ByteReader"/> and <see cref="ByteWriter"/>. 
    /// </summary>
    /// <remarks>The types in this array represent the element type, not the actual array type. Enums are not included but are all also valid.</remarks>
    public static IReadOnlyList<Type> ValidArrayReadWriteTypes { get; } = new ReadOnlyCollection<Type>(ValidArrayTypes);

    /// <summary>
    /// Checks if a type can be automatically read and written by <see cref="ByteReader"/> and <see cref="ByteWriter"/>.
    /// </summary>
    public static bool IsValidAutoType(Type type)
    {
        if (type.IsEnum) return true;
        
        lock (AutoReadSync)
        {
            if (!type.IsArray)
                return ValidTypes.Contains(type);

            type = type.GetElementType()!;
            return type != null && ValidArrayTypes.Contains(type);

        }
    }

    /// <summary>
    /// Try to add a custom type to the 'auto read/write' list of types.
    /// </summary>
    /// <returns><see langword="false"/> if the type is already in the list, otherwise <see langword="true"/>.</returns>
    public static bool TryAddAutoSerializableClassType<T>(
        Writer<T> writerMethod, Writer<T?> nullableWriterMethod,
        Reader<T> readerMethod, Reader<T?> nullableReaderMethod
    ) where T : class
    {
        Type type = typeof(T);
        bool isArray = false;
        if (type.IsArray)
        {
            isArray = true;
            type = type.GetElementType()!;
        }

        lock (AutoReadSync)
        {
            List<Type> list = isArray ? ValidArrayTypes : ValidTypes;

            if (list.Contains(type))
                return false;

            list.Add(type);
            ByteReader.AddReaderMethod(readerMethod);
            ByteWriter.AddWriterMethod(writerMethod);
            ByteReader.AddNullableReaderClassMethod(nullableReaderMethod);
            ByteWriter.AddNullableWriterClassMethod(nullableWriterMethod);
        }

        return true;
    }

    /// <summary>
    /// Try to add a custom type to the 'auto read/write' list of types.
    /// </summary>
    /// <returns><see langword="false"/> if the type is already in the list, otherwise <see langword="true"/>.</returns>
    public static bool TryAddAutoSerializableStructType<T>(
        Writer<T> writerMethod, Writer<T?> nullableWriterMethod,
        Reader<T> readerMethod, Reader<T?> nullableReaderMethod
        ) where T : struct
    {
        Type type = typeof(T);
        Type nullableType = typeof(T?);

        lock (AutoReadSync)
        {
            if (ValidTypes.Contains(type) && ValidTypes.Contains(nullableType))
                return false;

            if (!ValidTypes.Contains(type))
            {
                ValidTypes.Add(type);
                ByteWriter.AddWriterMethod(writerMethod);
                ByteReader.AddReaderMethod(readerMethod);
            }

            if (ValidTypes.Contains(nullableType))
                return true;

            ByteWriter.AddNullableWriterStructMethod(nullableWriterMethod);
            ByteReader.AddNullableReaderStructMethod(nullableReaderMethod);
        }

        return true;
    }

    /// <summary>
    /// Get the minimum amount of bytes <typeparamref name="T"/> can take up during auto-encoding.
    /// </summary>
    public static int GetMinimumSize<T>() => GetMinimumSize(typeof(T));

    /// <summary>
    /// Get the minimum amount of bytes a <paramref name="type"/> can take up during auto-encoding.
    /// </summary>
    public static int GetMinimumSize(Type type)
    {
        if (type.IsPointer) return IntPtr.Size;

        if (type.IsArray || type == typeof(string)) return sizeof(ushort);

        try
        {
            return Marshal.SizeOf(type);
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }


    /// <summary>
    /// Throw an error if <typeparamref name="T"/> is not auto-encodable.
    /// </summary>
    /// <exception cref="AutoEncodeTypeNotFoundException"><typeparamref name="T"/> is not auto-encodable.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotAutoType<T>()
    {
        if (!IsValidAutoType(typeof(T)))
            throw new AutoEncodeTypeNotFoundException(typeof(T));
    }

    /// <summary>
    /// Throw an error if <paramref name="type"/> is not auto-encodable.
    /// </summary>
    /// <exception cref="AutoEncodeTypeNotFoundException"><paramref name="type"/> is not auto-encodable.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotAutoType(Type type)
    {
        if (!IsValidAutoType(type))
            throw new AutoEncodeTypeNotFoundException(type);
    }
}

/// <summary>
/// Represents a static write method for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of value to write.</typeparam>
/// <param name="writer">The writer.</param>
/// <param name="arg">The value to write to <paramref name="writer"/>.</param>
public delegate void Writer<in T>(ByteWriter writer, T arg);

/// <summary>
/// Represents a static read method for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of value to read.</typeparam>
/// <param name="reader">The reader.</param>
/// <returns>The read value.</returns>
public delegate T Reader<out T>(ByteReader reader);