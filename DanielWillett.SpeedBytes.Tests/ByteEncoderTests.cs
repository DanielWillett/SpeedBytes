#define PRINT_BYTES
using DanielWillett.SpeedBytes.Compression;
using DanielWillett.SpeedBytes.Formatting;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable LocalizableElement

namespace DanielWillett.SpeedBytes.Tests;

/// <summary>Unit tests for <see cref="ByteWriter"/> and <see cref="ByteReader"/>.</summary>
[TestClass]
public class ByteEncoderTests
{
    private static ByteWriter GetWriter(bool stream, out Stream? memory)
    {
        ByteWriter writer = new ByteWriter(stream ? 2 : 512);
        if (stream)
        {
            writer.Stream = memory = new MemoryStream();
        }
        else memory = null;
        return writer;
    }
    private static void TryPrint(ByteWriter writer)
    {
#if !PRINT_BYTES
        return;
#endif

        const ByteStringFormat fmt =
            ByteStringFormat.NewLineAtBeginning
            | ByteStringFormat.ColumnLabels
            | ByteStringFormat.RowLabels
            | ByteStringFormat.Columns8;

        try
        {
            if (writer.Stream is MemoryStream mem)
            {
                mem.Seek(0, SeekOrigin.Begin);
                byte[] output = new byte[mem.Length];
                int ln = mem.Read(output, 0, output.Length);
                Console.WriteLine(ByteFormatter.FormatBinary(output.AsSpan(0, ln), fmt));
            }
            else if(writer.Stream != null)
                Console.WriteLine("Writer is in stream mode.");
            else
            {
                byte[] bytes = writer.ToArray();
                Console.WriteLine(ByteFormatter.FormatBinary(bytes, fmt));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private static void TestOne<T>(T value, bool stream, bool testNullable = true)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);
        try
        {
            writer.Write(10L);
            ByteWriter.GetWriteMethodDelegate<T>(false).Invoke(writer, value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }

            Assert.AreEqual(10L, reader.ReadInt64());
            Assert.AreEqual(value, ByteReader.GetReadMethodDelegate<T>(false).Invoke(reader));
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }

        if (!testNullable)
            return;

        Type type = typeof(T);
        if (type.IsValueType)
            type = typeof(Nullable<>).MakeGenericType(type);

        writer = GetWriter(stream, out mem);
        try
        {
            writer.Write(10L);
            Delegate d = ByteWriter.CreateWriteMethodDelegate(type, true);
            MethodInfo invMethod = d.GetType().GetMethod("Invoke")!;
            invMethod.Invoke(d, [ writer, value ]);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            Assert.AreEqual(10L, reader.ReadInt64());
            Delegate d2 = ByteReader.CreateReadMethodDelegate(type, true);
            invMethod = d2.GetType().GetMethod("Invoke")!;
            object? rtn = invMethod.Invoke(d2, [ reader ]);
            Assert.IsNotNull(rtn);
            if (rtn is T t)
            {
                Assert.AreEqual(value, t);
            }
            else if (rtn.GetType() == type)
            {
                object nullable = Activator.CreateInstance(type, [ value ])!;
                Assert.AreEqual(nullable, rtn);
            }
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    private static void TestShortMany<T>(T[] value, bool stream, bool testNullable = true)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);
        try
        {
            writer.Write(10L);
            ByteWriter.GetWriteMethodDelegate<T[]>(false).Invoke(writer, value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            Assert.AreEqual(10L, reader.ReadInt64());
            T[] data = ByteReader.GetReadMethodDelegate<T[]>(false).Invoke(reader);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }

        if (!testNullable)
            return;

        writer = GetWriter(stream, out mem);
        try
        {
            writer.Write(10L);
            ByteWriter.GetWriteMethodDelegate<T[]>(false).Invoke(writer, value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            Assert.AreEqual(10L, reader.ReadInt64());
            T[] data = ByteReader.GetReadMethodDelegate<T[]>(false).Invoke(reader);

            Assert.IsNotNull(data);
            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(false, false)]
    public void TestWriteBoolean(bool value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, true)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, true)]
    [DataRow(new bool[0], true)]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, false)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, false)]
    [DataRow(new bool[0], false)]
    public void TestWriteBooleanArray(bool[] value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.Write(value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            bool[] data = reader.ReadBoolArray();

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < value.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, true)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, true)]
    [DataRow(new bool[0], true)]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, false)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, false)]
    [DataRow(new bool[0], false)]
    public void TestWriteBitArray(bool[] value, bool stream)
    {
        BitArray bitArray = new BitArray(value);
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.Write(bitArray);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            BitArray data = reader.ReadBitArray();

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < value.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, true)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, true)]
    [DataRow(new bool[0], true)]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, false)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, false)]
    [DataRow(new bool[0], false)]
    public void TestWriteLongBitArray(bool[] value, bool stream)
    {
        BitArray bitArray = new BitArray(value);
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteLong(bitArray);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            BitArray data = reader.ReadLongBitArray();

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < value.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, true)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, true)]
    [DataRow(new bool[0], true)]
    [DataRow(new bool[] { true, false, false, true, false, false, false, true, false, true, true, false }, false)]
    [DataRow(new bool[] { false, false, true, true, false, true, true, true }, false)]
    [DataRow(new bool[0], false)]
    public void TestWriteLongBooleanArray(bool[] value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteLong(value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            bool[] data = reader.ReadLongBoolArray();

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < value.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }
    
    [TestMethod]
    [DataRow((byte)0, true)]
    [DataRow((byte)24, true)]
    [DataRow((byte)255, true)]
    [DataRow((byte)0, false)]
    [DataRow((byte)24, false)]
    [DataRow((byte)255, false)]
    public void TestWriteUInt8(byte value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new byte[] { 4, 0, 32, 8, 12 }, true)]
    [DataRow(new byte[] { 7, 243, 3, 246, 46 }, true)]
    [DataRow(new byte[0], true)]
    [DataRow(new byte[] { 4, 0, 32, 8, 12 }, false)]
    [DataRow(new byte[] { 7, 243, 3, 246, 46 }, false)]
    [DataRow(new byte[0], false)]
    public void TestWriteUInt8Array(byte[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(new byte[] { 4, 0, 32, 8, 12 }, false)]
    [DataRow(new byte[] { 7, 243, 3, 246, 46 }, false)]
    [DataRow(new byte[] { 7, 243, 3, 246, 46 }, true)]
    [DataRow(new byte[0], false)]
    public void TestWriteLongUInt8Array(byte[] value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteLong(value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            byte[] data = reader.ReadLongUInt8Array();

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow('a', true)]
    [DataRow('粞', true)]
    [DataRow('a', false)]
    [DataRow('粞', false)]
    public void TestWriteChar(char value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new char[] { 'h', 'e', 'l', 'l', 'o' }, true)]
    [DataRow(new char[] { '\\', ' ', 'w', '粞', 'r', 'l', 'd' }, true)]
    [DataRow(new char[] { 'h', 'e', 'l', 'l', 'o' }, false)]
    [DataRow(new char[] { '\\', ' ', 'w', '粞', 'r', 'l', 'd' }, false)]
    public void TestWriteCharArray(char[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow("hello", true)]
    [DataRow("\\ w粞rld", true)]
    [DataRow("hello", false)]
    [DataRow("\\ w粞rld", false)]
    public void TestWriteString(string value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new string[] { "hello", "\\ w粞rld" }, true)]
    [DataRow(new string[] { "hello", "\\ w粞rld" }, false)]
    public void TestWriteStringArray(string[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(12d, true)]
    [DataRow(0d, true)]
    [DataRow(-4d, true)]
    [DataRow(12d, false)]
    [DataRow(0d, false)]
    [DataRow(-4d, false)]
    public void TestWriteDecimal(double value, bool stream)
    {
        TestOne(new decimal(value), stream);
    }

    [TestMethod]
    [DataRow(new double[] { 12d, 6d, 0.012842042048d }, true)]
    [DataRow(new double[] { 4d, 1d, -53d }, true)]
    [DataRow(new double[] { 12d, 6d, 0.012842042048d }, false)]
    [DataRow(new double[] { 4d, 1d, -53d }, false)]
    public void TestWriteDecimalArray(double[] value, bool stream)
    {
        decimal[] val = new decimal[value.Length];
        for (int i = 0; i < val.Length; ++i)
            val[i] = new decimal(value[i]);

        TestShortMany(val, stream);
    }

    [TestMethod]
    [DataRow(12d, true)]
    [DataRow(0d, true)]
    [DataRow(-4d, true)]
    [DataRow(12d, false)]
    [DataRow(0d, false)]
    [DataRow(-4d, false)]
    public void TestWriteDouble(double value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new double[] { 12d, 6d, 0.012842042048d }, true)]
    [DataRow(new double[] { 4d, 1d, -53d }, true)]
    [DataRow(new double[] { 12d, 6d, 0.012842042048d }, false)]
    [DataRow(new double[] { 4d, 1d, -53d }, false)]
    public void TestWriteDoubleArray(double[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(12f, true)]
    [DataRow(0f, true)]
    [DataRow(-4f, true)]
    [DataRow(12f, false)]
    [DataRow(0f, false)]
    [DataRow(-4f, false)]
    public void TestWriteFloat(float value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new float[] { 12f, 6f, 0.012842042048f }, true)]
    [DataRow(new float[] { 4f, 1f, -53f }, true)]
    [DataRow(new float[] { 12f, 6f, 0.012842042048f }, false)]
    [DataRow(new float[] { 4f, 1f, -53f }, false)]
    public void TestWriteFloatArray(float[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(12, true)]
    [DataRow(0, true)]
    [DataRow(int.MaxValue, true)]
    [DataRow(12, false)]
    [DataRow(0, false)]
    [DataRow(int.MaxValue, false)]
    public void TestWriteInt32(int value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(46, false)]
    [DataRow(0, false)]
    [DataRow(-3578539, false)]
    [DataRow(-ByteEncoders.Int24MaxValue, false)]
    [DataRow(ByteEncoders.Int24MaxValue, false)]
    [DataRow(7, true)]
    public void TestWriteInt24(int value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteInt24(value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            Assert.AreEqual(value, reader.ReadInt24());
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(46u, false)]
    [DataRow(0u, false)]
    [DataRow(3578539u, false)]
    [DataRow((uint)(ByteEncoders.Int24MaxValue * 2), false)]
    [DataRow(3u, true)]
    public void TestWriteUInt24(uint value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteUInt24(value);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem != null)
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            else reader.LoadNew(writer.ToArray());

            Assert.AreEqual(value, reader.ReadUInt24());
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new int[] { -1, 3, 6 }, true)]
    [DataRow(new int[] { -1, 3, 6 }, false)]
    public void TestWriteInt32Array(int[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(12L, true)]
    [DataRow(0L, true)]
    [DataRow(long.MaxValue, true)]
    [DataRow(12L, false)]
    [DataRow(0L, false)]
    [DataRow(long.MaxValue, false)]
    public void TestWriteInt64(long value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new long[] { -1L, 3L, long.MaxValue }, true)]
    [DataRow(new long[] { -1L, 3L, long.MaxValue }, false)]
    public void TestWriteInt64Array(long[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow((sbyte)3, true)]
    [DataRow((sbyte)0, true)]
    [DataRow(sbyte.MinValue, true)]
    [DataRow((sbyte)3, false)]
    [DataRow((sbyte)0, false)]
    [DataRow(sbyte.MinValue, false)]
    public void TestWriteInt8(sbyte value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new sbyte[] { -1, 3, sbyte.MinValue }, true)]
    [DataRow(new sbyte[] { -1, 3, sbyte.MinValue }, false)]
    public void TestWriteInt8Array(sbyte[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow((short)-3, true)]
    [DataRow((short)0, true)]
    [DataRow(short.MaxValue, true)]
    [DataRow((short)-3, false)]
    [DataRow((short)0, false)]
    [DataRow(short.MaxValue, false)]
    public void TestWriteInt16(short value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new short[] { -1, 3, short.MinValue }, true)]
    [DataRow(new short[] { -1, 3, short.MinValue }, false)]
    public void TestWriteInt16Array(short[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(3u, true)]
    [DataRow(0u, true)]
    [DataRow(uint.MaxValue, true)]
    [DataRow(3u, false)]
    [DataRow(0u, false)]
    [DataRow(uint.MaxValue, false)]
    public void TestWriteUInt32(uint value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new uint[] { uint.MinValue, 3, uint.MaxValue }, true)]
    [DataRow(new uint[] { uint.MinValue, 3, uint.MaxValue }, false)]
    public void TestWriteUInt32Array(uint[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow(3ul, true)]
    [DataRow(0ul, true)]
    [DataRow(ulong.MaxValue, true)]
    [DataRow(3ul, false)]
    [DataRow(0ul, false)]
    [DataRow(ulong.MaxValue, false)]
    public void TestWriteUInt32(ulong value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new ulong[] { ulong.MinValue, 3ul, ulong.MaxValue }, true)]
    [DataRow(new ulong[] { ulong.MinValue, 3ul, ulong.MaxValue }, false)]
    public void TestWriteUInt32Array(ulong[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow((ushort)3, true)]
    [DataRow((ushort)0, true)]
    [DataRow(ushort.MaxValue, true)]
    [DataRow((ushort)3, false)]
    [DataRow((ushort)0, false)]
    [DataRow(ushort.MaxValue, false)]
    public void TestWriteUInt32(ushort value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new ushort[] { ushort.MinValue, 3, ushort.MaxValue }, true)]
    [DataRow(new ushort[] { ushort.MinValue, 3, ushort.MaxValue }, false)]
    public void TestWriteUInt32Array(ushort[] value, bool stream)
    {
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow("2023-06-10T07:58:02.0065000", true)]
    [DataRow("2022-11-04T05:02:04.0000010", true)]
    [DataRow("2023-06-10T07:58:02.0065000", false)]
    [DataRow("2022-11-04T05:02:04.0000010", false)]
    public void TestWriteDateTime(string dt, bool stream)
    {
        DateTime value = DateTime.ParseExact(dt, "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new string[] { "2023-06-10T07:58:02.0220000", "2021-05-15T02:09:07.0005002", "2004-06-30T01:14:22.0001010" }, true)]
    [DataRow(new string[] { "2023-06-10T07:58:02.0220000", "2021-05-15T02:09:07.0005002", "2004-06-30T01:14:22.0001010" }, false)]
    public void TestWriteDateTimeArray(string[] dts, bool stream)
    {
        DateTime[] value = new DateTime[dts.Length];
        for (int i = 0; i < dts.Length; ++i)
            value[i] = DateTime.ParseExact(dts[i], "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow("2023-06-10T07:58:02.0065000+03:00", true)]
    [DataRow("2023-06-10T07:58:02.0065000+03:00", true)]
    [DataRow("2022-11-04T05:02:04.0000010-11:00", false)]
    [DataRow("2022-11-04T05:02:04.0000010-11:00", false)]
    public void TestWriteDateTimeOffset(string dt, bool stream)
    {
        DateTimeOffset value = DateTimeOffset.ParseExact(dt, "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new string[] { "2023-06-10T07:58:02.0220000+04:00", "2021-05-15T02:09:07.0005002+00:00", "2004-06-30T01:14:22.0001010-10:00" }, true)]
    [DataRow(new string[] { "2023-06-10T07:58:02.0220000+04:00", "2021-05-15T02:09:07.0005002+00:00", "2004-06-30T01:14:22.0001010-10:00" }, false)]
    public void TestWriteDateTimeOffsetArray(string[] dts, bool stream)
    {
        DateTimeOffset[] value = new DateTimeOffset[dts.Length];
        for (int i = 0; i < dts.Length; ++i)
            value[i] = DateTimeOffset.ParseExact(dts[i], "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow("a421adb44a664fa3a61cf098d7d1aca2", true)]
    [DataRow("77aa06bf324a4b18b1de0b3b2b5d082f", true)]
    [DataRow("a421adb44a664fa3a61cf098d7d1aca2", false)]
    [DataRow("77aa06bf324a4b18b1de0b3b2b5d082f", false)]
    public void TestWriteGuid(string dt, bool stream)
    {
        Guid value = Guid.ParseExact(dt, "N");
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(new string[] { "cdca249acc1e490aaab7dcacc6891d4e", "3c1e0236cf2648caa98665ad01b5eff1", "e47e5b5a272340dbb3918d9221d3e17b" }, true)]
    [DataRow(new string[] { "cdca249acc1e490aaab7dcacc6891d4e", "3c1e0236cf2648caa98665ad01b5eff1", "e47e5b5a272340dbb3918d9221d3e17b" }, false)]
    public void TestWriteGuidArray(string[] dts, bool stream)
    {
        Guid[] value = new Guid[dts.Length];
        for (int i = 0; i < dts.Length; ++i)
            value[i] = Guid.ParseExact(dts[i], "N");
        TestShortMany(value, stream);
    }

    [TestMethod]
    [DataRow("35:12:45:18.4210000", true)]
    [DataRow("-11:00:51:44.7470000", true)]
    [DataRow("35:12:45:18.4210000", false)]
    [DataRow("-11:00:51:44.7470000", false)]
    public void TestWriteTimeSpan(string ts, bool stream)
    {
        TimeSpan value = TimeSpan.ParseExact(ts, "G", CultureInfo.InvariantCulture, TimeSpanStyles.None);
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(typeof(ByteReader), false)]
    [DataRow(typeof(string), false)]
    [DataRow(typeof(object), false)]
    [DataRow(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger), false)]
    [DataRow(typeof(ByteReader), true)]
    [DataRow(typeof(string), true)]
    [DataRow(typeof(object), true)]
    [DataRow(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger), true)]
    public void TestWriteType(Type value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? memory);
        ByteReader reader = new ByteReader();
        try
        {
            writer.Write(value);
            TryPrint(writer);
            if (memory == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                memory.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(memory);
            }
            Assert.AreEqual(value, reader.ReadType());
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (memory != null)
            {
                memory.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new Type[]
    {
        typeof(ByteReader), typeof(string), typeof(object), typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger)
    }, true)]
    [DataRow(new Type[]
    {
        typeof(ByteReader), typeof(string), typeof(object), typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger)
    }, false)]
    public void TestWriteTypeArray(Type[] value, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? memory);
        ByteReader reader = new ByteReader();

        try
        {
            writer.Write(value);
            TryPrint(writer);
            if (memory == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                memory.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(memory);
            }
            Type?[] info = reader.ReadTypeArray();

            Assert.AreEqual(value.Length, info.Length);
            for (int i = 0; i < value.Length; ++i)
                Assert.AreEqual(value[i], info[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (memory != null)
            {
                memory.Dispose();
                writer.Stream = null;
            }
        }
    }

    public enum DirectionByte : byte
    {
        North,
        South,
        East,
        West
    }
    public enum DirectionShort : short
    {
        North,
        South,
        East,
        West
    }
    public enum Direction
    {
        North,
        South,
        East,
        West
    }
    public enum DirectionLong : long
    {
        North,
        South,
        East,
        West
    }

    [TestMethod]
    [DataRow(DirectionByte.North, true)]
    [DataRow(DirectionByte.South, true)]
    [DataRow(DirectionByte.North, false)]
    [DataRow(DirectionByte.South, false)]
    public void TestWriteEnumByte(DirectionByte value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(DirectionShort.North, true)]
    [DataRow(DirectionShort.South, true)]
    [DataRow(DirectionShort.North, false)]
    [DataRow(DirectionShort.South, false)]
    public void TestWriteEnumShort(DirectionShort value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(Direction.North, true)]
    [DataRow(Direction.South, true)]
    [DataRow(Direction.North, false)]
    [DataRow(Direction.South, false)]
    public void TestWriteEnumInt(Direction value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    [DataRow(DirectionLong.North, true)]
    [DataRow(DirectionLong.South, true)]
    [DataRow(DirectionLong.North, false)]
    [DataRow(DirectionLong.South, false)]
    public void TestWriteEnumLong(DirectionLong value, bool stream)
    {
        TestOne(value, stream);
    }

    [TestMethod]
    public void TestInvalidType()
    {
        Assert.ThrowsException<AutoEncodeTypeNotFoundException>(() => ByteWriter.GetWriteMethodDelegate<ByteEncoderTests>());
    }

    [TestMethod]
    public void TestValidType()
    {
        Delegate writerMethod = ByteWriter.GetWriteMethodDelegate<Guid>();

        Assert.IsNotNull(writerMethod);
    }

    [TestMethod]
    [DataRow(new byte[] { 0, 0, 0, 30, 16, 255, 224, 0, 0, 0, 0, 0, 0, 0, 0, 21, 0, 12, 52, 64 }, true, true)]
    [DataRow(new byte[] { 0, 0, 0, 30, 16, 255, 224, 0, 0, 0, 0, 0, 0, 0, 0, 21, 0, 12, 52, 64 }, true, false)]
    [DataRow(new byte[] { 0, 0, 0, 30, 16, 255, 224, 0, 0, 0, 0, 0, 0, 0, 0, 21, 0, 12, 52, 64 }, false, false)]
    public void TestWriteZeroCompressedUInt8ArrayType(byte[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            byte[] data = reader.ReadZeroCompressedUInt8Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new ushort[] { 1, 0, 0, 30, 16, 255, 2224, 0, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52, 64 }, true, true)]
    [DataRow(new ushort[] { 1, 0, 0, 30, 16, 255, 2224, 0, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52, 64 }, true, false)]
    [DataRow(new ushort[] { 0, 0, 0, 30, 16, 255, 65535, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 12, 15, 64 }, false, false)]
    public void TestWriteZeroCompressedUInt16ArrayType(ushort[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            ushort[] data = reader.ReadZeroCompressedUInt16Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new uint[] { 1, 0, 0, 30, 16, 255, 2224, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, true)]
    [DataRow(new uint[] { 1, 0, 0, 30, 16, 255, 2224, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, false)]
    [DataRow(new uint[] { 0, 0, 0, 30, 16, 255, 65535, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 12, 15, 64 }, false, false)]
    public void TestWriteZeroCompressedUInt32ArrayType(uint[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            uint[] data = reader.ReadZeroCompressedUInt32Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new ulong[] { 1, 0, 0, 30, 16, 255, 2224, ulong.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, true)]
    [DataRow(new ulong[] { 1, 0, 0, 30, 16, 255, 2224, ulong.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, false)]
    [DataRow(new ulong[] { 0, 0, 0, 30, 16, 255, 65535, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 12, 15, 64 }, false, false)]
    public void TestWriteZeroCompressedUInt64ArrayType(ulong[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            ulong[] data = reader.ReadZeroCompressedUInt64Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new sbyte[] { 0, 0, 0, 30, 16, -127, sbyte.MinValue, sbyte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 12, 52, 64 }, true, true)]
    [DataRow(new sbyte[] { 0, 0, 0, 30, 16, -127, sbyte.MinValue, sbyte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 12, 52, 64 }, true, false)]
    [DataRow(new sbyte[] { 0, 0, 0, 30, 16, -127, sbyte.MinValue, sbyte.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 12, 52, 64 }, false, false)]
    public void TestWriteZeroCompressedInt8ArrayType(sbyte[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            sbyte[] data = reader.ReadZeroCompressedInt8Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new short[] { 1, 0, 0, 30, 16, 255, 2224, 0, 0, 0, short.MinValue, 0, 0, 0, 0, 21, 0, 4, 52, 64 }, true, true)]
    [DataRow(new short[] { 1, 0, 0, 30, 16, 255, 2224, 0, 0, 0, short.MinValue, 0, 0, 0, 0, 21, 0, 4, 52, 64 }, true, false)]
    [DataRow(new short[] { 0, 0, 0, 30, 16, 255, short.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 12, 15, 64 }, false, false)]
    public void TestWriteZeroCompressedInt16ArrayType(short[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            short[] data = reader.ReadZeroCompressedInt16Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new int[] { 1, 0, 0, 30, 16, 255, 2224, 99248240, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, false, false)]
    [DataRow(new int[] { 1, 0, 0, 30, 16, 255, 2224, int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, true)]
    [DataRow(new int[] { 1, 0, 0, 30, 16, 255, 2224, int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, false)]
    [DataRow(new int[] { 0, 0, 0, 30, 16, 255, 65535, 0, 0, 0, 0, 0, 0, 0, 0, int.MinValue, 0, 12, 15, 64 }, false, false)]
    public void TestWriteZeroCompressedInt32ArrayType(int[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            int[] data = reader.ReadZeroCompressedInt32Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    [DataRow(new long[] { 1, 0, 0, 30, 16, 255, 2224, long.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, true)]
    [DataRow(new long[] { 1, 0, 0, 30, 16, 255, 2224, long.MaxValue, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 }, true, false)]
    [DataRow(new long[] { 0, 0, 0, 30, 16, 255, 65535, 0, 0, 0, 0, 0, 0, 0, long.MinValue, 16, 0, 12, 15, 64 }, false, false)]
    public void TestWriteZeroCompressedInt64ArrayType(long[] value, bool @long, bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        try
        {
            writer.WriteZeroCompressed(value, @long);
            TryPrint(writer);
            ByteReader reader = new ByteReader();
            if (mem == null)
                reader.LoadNew(writer.ToArray());
            else
            {
                mem.Seek(0, SeekOrigin.Begin);
                reader.LoadNew(mem);
            }
            long[] data = reader.ReadZeroCompressedInt64Array(@long);

            Assert.AreEqual(value.Length, data.Length);
            for (int i = 0; i < data.Length; ++i)
                Assert.AreEqual(value[i], data[i]);
            Assert.IsFalse(reader.HasFailed);
        }
        finally
        {
            if (mem != null)
            {
                mem.Dispose();
                writer.Stream = null;
            }
        }
    }

    [TestMethod]
    public void TestSkip()
    {
        ByteWriter writer = new ByteWriter();
        const string testString = "test";
        
        writer.Write(0);
        writer.Write(testString);
        TryPrint(writer);
        byte[] result = writer.ToArray();

        ByteReader reader = new ByteReader();
        reader.LoadNew(result);
        reader.Skip(sizeof(int));

        string output = reader.ReadString();

        Assert.AreEqual(testString, output);
        Assert.IsFalse(reader.HasFailed);
    }

    [TestMethod]
    public void TestStreamBufferOverflow()
    {
        ByteWriter writer = new ByteWriter(8);

        using MemoryStream ms = new MemoryStream(64);
        writer.Stream = ms;

        writer.Write(3);
        writer.Write((ushort)5);
        writer.WriteBlock(new byte[] { 43, 26, 224, 46, 2 });
        TryPrint(writer);

        Assert.AreEqual(8, writer.Buffer.Length);
        writer.Flush();
        Assert.AreEqual(11L, ms.Position);
    }

    [TestMethod]
    public void TestBufferOverflow()
    {
        ByteWriter writer = new ByteWriter(8);

        writer.Write(3);
        writer.Write((ushort)5);
        writer.WriteBlock(new byte[] { 43, 26, 224, 46, 2 });
        
        Assert.IsTrue(11 <= writer.Buffer.Length);
    }
    [TestMethod]
    public void TestNavMethods()
    {
        ByteWriter writer = new ByteWriter();
        DateTime dt = DateTime.Now;
        writer.Write(dt);
        writer.Write(10L);
        writer.Write(20L);

        writer.BackTrack(sizeof(long));
        writer.Write(30L);
        writer.Return();

        byte[] data = writer.ToArray();

        ByteReader reader = new ByteReader();
        reader.LoadNew(data, 0);

        DateTime dt2 = reader.ReadDateTime();
        long l2 = reader.ReadInt64();
        long l3 = reader.ReadInt64();
        Assert.AreEqual(dt, dt2);
        Assert.AreEqual(30L, l2);
        Assert.AreEqual(20L, l3);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestComplex(bool stream)
    {
        ByteWriter writer = GetWriter(stream, out Stream? mem);

        const long val1 = 30L;
        decimal[] val2 = [ 3m, 10m, 561.253966m ];
        DateTime dt = new DateTime(2022, 10, 9);

        const int blockLen = 9951;
        const byte blockVal = 12;

        writer.Write(val1);
        writer.Write(val2);
        writer.Write(dt);
        writer.WriteBlock(blockVal, blockLen);

        byte[] data;
        if (!stream)
            data = writer.ToArray();
        else
            data = ((MemoryStream)mem!).ToArray();

        ByteReader reader = new ByteReader();
        reader.LoadNew(data, 0);

        Assert.AreEqual(val1, reader.ReadInt64());
        Assert.That.SequenceIsEqual(val2, reader.ReadDecimalArray());
        Assert.AreEqual(dt, reader.ReadDateTime());
        
        byte[] block = new byte[blockLen];
        Unsafe.InitBlock(ref block[0], blockVal, blockLen);
        Assert.That.SequenceIsEqual(block, reader.ReadBlock(blockLen));
    }

    [TestMethod]
    [DataRow(100_445L, 2)]
    [DataRow(100_445L, 0)]
    [DataRow(0L, 0)]
    [DataRow(-10L, 0)]
    [DataRow(100_445L, 2)]
    [DataRow(0L, 2)]
    [DataRow(-10L, 2)]
    public void TestCapacity(long capacity, int decimals)
    {
        int ct = ByteFormatter.GetCapacityLength(capacity, decimals: decimals);

        Span<char> capacityString = stackalloc char[ct];
        int ct2 = ByteFormatter.FormatCapacity(capacity, capacityString, decimals: decimals);
        Assert.AreEqual(ct2, ct);
        Console.WriteLine(capacityString.ToString());
    }
}