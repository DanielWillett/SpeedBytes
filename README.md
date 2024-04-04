# SpeedBytes

Library for quick and easy manual binary serialization.

Get the latest release on NuGet: https://www.nuget.org/packages/DanielWillett.SpeedBytes

Includes targets for `netcoreapp3.1, net5.0, net8.0, and net461`.
Unit tested on `.NET Core 3.1, .NET 5, .NET 8, and .NET Framework 4.8.1`.

Main classes:
* `ByteReader` Contains methods for reading data from a byte array or stream.
* `ByteWriter` Contains methods for writing data to an expandable byte array or stream.
* `ByteFormatter` Contains methods for converting byte sequences and capacities into text.

Features are available for reading and writing binary data from generic types.

Also includes experimental (untested) support for big endian machines. 
Please create issues for errors if you encounter an error regarding endianness as I have no way to test it.

# Standard I/O
## Binary Buffer
### Writing
```cs
// creates a writer with a starting capacity of 1024 B.
ByteWriter writer = new ByteWriter(1024);

// write an int32 (4 bytes)
writer.Write(32);

// write a UTF-8 string (2 byte length header + 11 UTF-8 bytes)
writer.Write("test string");

// write an int32 array (2 byte length header + 16 bytes of int32)
writer.write(new int[] { 1, 76, 14, 9 });

byte[] data = writer.ToArray();
```

### Reading
```cs
byte[] data = /* data source */;

ByteReader reader = new ByteReader();

reader.LoadNew(data);

int count = reader.ReadInt32();
string words = reader.ReadString();
int[] array = reader.ReadInt32Array();
```

## Stream Buffer
Also contains support for reading from and writing to Streams.
### Writing
```cs
using FileStream stream = /* etc */;

ByteWriter writer = new ByteWriter
{
	Stream = stream
};

// write 100 int32's (400 bytes)
for (int i = 0; i < 100; i++)
	writer.Write(i);

writer.Flush();
```

### Reading
```cs
using FileStream stream = /* etc */;

ByteReader reader = new ByteReader();
reader.LoadNew(stream);

// read 400 bytes to the stack
Span<byte> data = stackalloc byte[400];
reader.ReadBlockTo(data);
```

# Generic I/O

Support for generic type reading and writing (or just by passing a `Type`).

```cs
// generic delegates are cached using a static generic class
Reader<string> readFunc  = ByteReader.GetReadMethodDelegate<string>(isNullable: false);
Writer<string> writeFunc = ByteWriter.GetWriteMethodDelegate<string>(isNullable: false);

ByteWriter writer = /* etc */;

writeFunc(writer, "test string");

ByteReader reader = /* etc */;

string testString = readFunc(reader);
```

## Register Custom Types
```cs
// entrypoint
public static void Main(string[] args)
{
    // adds the type 'Version' to ByteReader.GetReadMethodDelegate and ByteWriter.GetWriteMethodDelegate.
    ByteEncoders.TryAddAutoSerializableClassType(
        WriteVersion,
        WriteNullableVersion,
        ReadVersion,
        ReadNullableVersion
    );
}

private static void WriteVersion(ByteWriter writer, Version version)
{
    writer.Write(version.Major);
    writer.Write(version.Minor);
    writer.Write(version.Build);
    writer.Write(version.Revision);
}
private static void WriteNullableVersion(ByteWriter writer, Version? version)
{
    if (version != null)
    {
        writer.Write(true);
        WriteVersion(writer, version);
    }
    else writer.Write(false);
}
private static Version ReadVersion(ByteReader reader)
{
    return new Version(
        major: reader.ReadInt32(),
        minor: reader.ReadInt32(),
        build: reader.ReadInt32(),
        revision: reader.ReadInt32()
    );
}
private static Version? ReadNullableVersion(ByteReader reader)
{
    if (!reader.ReadBool())
        return null;
    return ReadVersion(reader);
}
```

# Byte Formatter

Extra features for formatting binary data into a string.

## Formatting Byte Sequences
```cs
const ByteStringFormat fmt =
	ByteStringFormat.NewLineAtBeginning
	| ByteStringFormat.ColumnLabels
	| ByteStringFormat.RowLabels
	| ByteStringFormat.Columns8;

byte[] data = [ 16, 32, 64, /* etc */ ];
int ct = ByteFormatter.GetMaxBinarySize(data.Length, fmt);

Span<char> binaryString = stackalloc char[ct];
ByteFormatter.FormatBinary(data, binaryString, fmt);

// or

string binaryString = ByteFormatter.FormatBinary(data, fmt);

/* sample output with a 64 byte array

         01 02 03 04 05 06 07 08
    0x00 10 20 30 44 61 6E 69 65
    0x08 6C 57 69 6C 6C 65 74 74
    0x10 2E 53 70 65 65 64 42 79
    0x18 74 65 73 2E 42 79 74 65
    0x20 52 65 61 64 65 72 2C 20
    0x28 44 61 6E 69 65 6C 57 69
    0x30 6C 6C 65 74 74 2E 53 70
    0x38 65 65 64 42 79 74 65 73
*/
```

## Formatting Capacity
```cs
const long capacity = 100_445; // ~ 98.091 KiB

int ct = ByteFormatter.GetCapacityLength(capacity, decimals: 2 /* 0.00 */);

Span<char> capacityString = stackalloc char[ct];
ByteFormatter.FormatCapacity(capacity, capacityString, decimals: 2);

// or

string capacityString = ByteFormatter.FormatCapacity(capacity, decimals: 2);

// capacityString = "98.09 KiB"
```

# Zero-Compressed Format

Also includes extension methods for compressing integer spans (of any integral type) into a 'zero-compressed' format, in which long spans of zeros within a span are compressed significantly.
```cs
using DanielWillett.SpeedBytes.Compression;

/* writing */
int[] array = [ 1, 0, 0, 30, 16, 255, 2224, 99248240, 0, 0, 0, 0, 0, 0, 0, 21, 0, 4, 52 ];

ByteWriter writer = new ByteWriter();
writer.WriteZeroCompressed(array);

/* resulting data (44 bytes for 19 int32s instead of 78):

         01 02 03 04 05 06 07 08
    0x00 14 00 02 FF 03 1E 00 00
    0x08 00 10 00 00 00 FF 00 00
    0x10 00 FF FF 00 00 07 FF 04
    0x18 00 00 00 80 00 00 00 00
    0x20 0C 00 00 00 0F 00 00 00
    0x28 40 00 00 00
*/

/* reading */
ByteReader reader = /* etc */;
int[] array = reader.ReadZeroCompressedInt32Array();
```