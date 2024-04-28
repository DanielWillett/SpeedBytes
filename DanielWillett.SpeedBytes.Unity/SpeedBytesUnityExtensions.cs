using UnityEngine;

namespace DanielWillett.SpeedBytes.Unity;

/// <summary>
/// Adds extensions supporting common types from UnityEngine.
/// </summary>
public static class SpeedBytesUnityExtensions
{
    static SpeedBytesUnityExtensions()
    {
        // register auto-serializable types
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadVector2, ReadNullableVector2);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadVector3, ReadNullableVector3);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadVector4, ReadNullableVector4);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadRect, ReadNullableRect);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadQuaternion, ReadNullableQuaternion);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadBounds, ReadNullableBounds);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadRay, ReadNullableRay);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadRay2D, ReadNullableRay2D);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadPlane, ReadNullablePlane);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadColor32, ReadNullableColor32);
        ByteEncoders.TryAddAutoSerializableStructType(Write, WriteNullable, ReadColor, ReadNullableColor);
    }

    /// <summary>
    /// Must be called to register types for auto-serialization.
    /// </summary>
    /// <remarks>This registration actually happens in the type initializer but this method will invoke that, as will any other method in this class.</remarks>
    public static void Register()
    {

    }

    /// <summary>
    /// Write a <see cref="Vector2"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Vector2 n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
    }

    /// <summary>
    /// Write a <see cref="Vector3"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Vector3 n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.z);
    }

    /// <summary>
    /// Write a <see cref="Vector4"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Vector4 n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.z);
        writer.Write(n.w);
    }

    /// <summary>
    /// Write a <see cref="Rect"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Rect n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.width);
        writer.Write(n.height);
    }

    /// <summary>
    /// Write a <see cref="Quaternion"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Quaternion n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.z);
        writer.Write(n.w);
    }

    /// <summary>
    /// Write a <see cref="Bounds"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Bounds n)
    {
        Vector3 c = n.center;
        Vector3 e = n.extents;
        writer.Write(c.x);
        writer.Write(c.y);
        writer.Write(c.z);
        writer.Write(e.x);
        writer.Write(e.y);
        writer.Write(e.z);
    }

    /// <summary>
    /// Write a <see cref="Ray"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Ray n)
    {
        Vector3 o = n.origin;
        Vector3 d = n.direction;
        writer.Write(o.x);
        writer.Write(o.y);
        writer.Write(o.z);
        writer.Write(d.x);
        writer.Write(d.y);
        writer.Write(d.z);
    }

    /// <summary>
    /// Write a <see cref="Ray2D"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Ray2D n)
    {
        Vector2 o = n.origin;
        Vector2 d = n.direction;
        writer.Write(o.x);
        writer.Write(o.y);
        writer.Write(d.x);
        writer.Write(d.y);
    }

    /// <summary>
    /// Write a <see cref="Plane"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Plane n)
    {
        Vector3 norm = n.normal;
        writer.Write(norm.x);
        writer.Write(norm.y);
        writer.Write(norm.z);
        writer.Write(n.distance);
    }

    /// <summary>
    /// Write a <see cref="Color32"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Color32 n)
    {
        Span<byte> rgba = stackalloc byte[4]
        {
            n.r,
            n.g,
            n.b,
            n.a
        };

        writer.WriteBlock(rgba);
    }

    /// <summary>
    /// Write a <see cref="Color32"/> to the buffer without the alpha channel (assumed to be 255).
    /// </summary>
    public static void WriteNoAlpha(this ByteWriter writer, Color32 n)
    {
        Span<byte> rgb = stackalloc byte[3]
        {
            n.r,
            n.g,
            n.b
        };

        writer.WriteBlock(rgb);
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer.
    /// </summary>
    public static void Write(this ByteWriter writer, Color n)
    {
        writer.Write(n.r);
        writer.Write(n.g);
        writer.Write(n.b);
        writer.Write(n.a);
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer without the alpha channel (assumed to be 1).
    /// </summary>
    public static void WriteNoAlpha(this ByteWriter writer, Color n)
    {
        writer.Write(n.r);
        writer.Write(n.g);
        writer.Write(n.b);
    }

    /// <summary>
    /// Write a <see cref="Vector2"/> to the buffer by reference.
    /// </summary>
    public static void Write(this ByteWriter writer, in Vector2 n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
    }

    /// <summary>
    /// Write a <see cref="Vector3"/> to the buffer by reference.
    /// </summary>
    public static void Write(this ByteWriter writer, in Vector3 n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.z);
    }

    /// <summary>
    /// Write a <see cref="Vector4"/> to the buffer by reference.
    /// </summary>
    public static void Write(this ByteWriter writer, in Vector4 n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.z);
        writer.Write(n.w);
    }

    /// <summary>
    /// Write a <see cref="Rect"/> to the buffer by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void Write(this ByteWriter writer, ref Rect n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.width);
        writer.Write(n.height);
    }

    /// <summary>
    /// Write a <see cref="Quaternion"/> to the buffer by reference.
    /// </summary>
    public static void Write(this ByteWriter writer, in Quaternion n)
    {
        writer.Write(n.x);
        writer.Write(n.y);
        writer.Write(n.z);
        writer.Write(n.w);
    }

    /// <summary>
    /// Write a <see cref="Bounds"/> to the buffer by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void Write(this ByteWriter writer, ref Bounds n)
    {
        Vector3 c = n.center;
        Vector3 e = n.extents;
        writer.Write(c.x);
        writer.Write(c.y);
        writer.Write(c.z);
        writer.Write(e.x);
        writer.Write(e.y);
        writer.Write(e.z);
    }

    /// <summary>
    /// Write a <see cref="Ray"/> to the buffer by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void Write(this ByteWriter writer, ref Ray n)
    {
        Vector3 o = n.origin;
        Vector3 d = n.direction;
        writer.Write(o.x);
        writer.Write(o.y);
        writer.Write(o.z);
        writer.Write(d.x);
        writer.Write(d.y);
        writer.Write(d.z);
    }

    /// <summary>
    /// Write a <see cref="Ray2D"/> to the buffer by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void Write(this ByteWriter writer, ref Ray2D n)
    {
        Vector2 o = n.origin;
        Vector2 d = n.direction;
        writer.Write(o.x);
        writer.Write(o.y);
        writer.Write(d.x);
        writer.Write(d.y);
    }

    /// <summary>
    /// Write a <see cref="Plane"/> to the buffer by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void Write(this ByteWriter writer, ref Plane n)
    {
        Vector3 norm = n.normal;
        writer.Write(norm.x);
        writer.Write(norm.y);
        writer.Write(norm.z);
        writer.Write(n.distance);
    }

    /// <summary>
    /// Write a <see cref="Color32"/> to the buffer by reference.
    /// </summary>
    public static void Write(this ByteWriter writer, in Color32 n)
    {
        Span<byte> rgba = stackalloc byte[4]
        {
            n.r,
            n.g,
            n.b,
            n.a
        };
        
        writer.WriteBlock(rgba);
    }

    /// <summary>
    /// Write a <see cref="Color32"/> to the buffer by reference without the alpha channel (assumed to be 255).
    /// </summary>
    public static void WriteNoAlpha(this ByteWriter writer, in Color32 n)
    {
        Span<byte> rgb = stackalloc byte[3]
        {
            n.r,
            n.g,
            n.b
        };
        
        writer.WriteBlock(rgb);
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer by reference.
    /// </summary>
    public static void Write(this ByteWriter writer, in Color n)
    {
        writer.Write(n.r);
        writer.Write(n.g);
        writer.Write(n.b);
        writer.Write(n.a);
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer by reference without the alpha channel (assumed to be 1).
    /// </summary>
    public static void WriteNoAlpha(this ByteWriter writer, in Color n)
    {
        writer.Write(n.r);
        writer.Write(n.g);
        writer.Write(n.b);
    }

    /// <summary>
    /// Write a nullable <see cref="Vector2"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Vector2? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Vector3"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Vector3? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Vector4"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Vector4? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Rect"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Rect? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Quaternion"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Quaternion? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Bounds"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Bounds? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Ray"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Ray? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Ray2D"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Ray2D? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Plane"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Plane? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Color32"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Color32? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Color32"/> to the buffer without the alpha channel (assumed to be 255).
    /// </summary>
    public static void WriteNullableNoAlpha(this ByteWriter writer, Color32? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteNoAlpha(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Color"/> to the buffer.
    /// </summary>
    public static void WriteNullable(this ByteWriter writer, Color? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.Write(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Color"/> to the buffer without the alpha channel (assumed to be 1).
    /// </summary>
    public static void WriteNullableNoAlpha(this ByteWriter writer, Color? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteNoAlpha(n.Value);
    }

    /// <summary>
    /// Reads a <see cref="Vector2"/> from the buffer.
    /// </summary>
    public static Vector2 ReadVector2(this ByteReader reader)
    {
        Vector2 v = default;

        v.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        v.y = reader.ReadFloat();

        return reader.HasFailed ? default : v;
    }

    /// <summary>
    /// Reads a <see cref="Vector3"/> from the buffer.
    /// </summary>
    public static Vector3 ReadVector3(this ByteReader reader)
    {
        Vector3 v = default;

        v.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        v.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        v.z = reader.ReadFloat();

        return reader.HasFailed ? default : v;
    }

    /// <summary>
    /// Reads a <see cref="Vector4"/> from the buffer.
    /// </summary>
    public static Vector4 ReadVector4(this ByteReader reader)
    {
        Vector4 v = default;

        v.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        v.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        v.z = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        v.w = reader.ReadFloat();

        return reader.HasFailed ? default : v;
    }

    /// <summary>
    /// Reads a <see cref="Rect"/> from the buffer.
    /// </summary>
    public static Rect ReadRect(this ByteReader reader)
    {
        float x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        float y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        float width = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        float height = reader.ReadFloat();

        return reader.HasFailed ? default : new Rect(x, y, width, height);
    }

    /// <summary>
    /// Reads a <see cref="Quaternion"/> from the buffer.
    /// </summary>
    public static Quaternion ReadQuaternion(this ByteReader reader)
    {
        Quaternion q = default;

        q.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        q.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        q.z = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        q.w = reader.ReadFloat();

        return reader.HasFailed ? default : q;
    }

    /// <summary>
    /// Reads a <see cref="Bounds"/> from the buffer.
    /// </summary>
    public static Bounds ReadBounds(this ByteReader reader)
    {
        Vector3 c = default;

        c.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.z = reader.ReadFloat();
        if (reader.HasFailed)
            return default;

        Vector3 e = default;

        e.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        e.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        e.z = reader.ReadFloat();
        if (reader.HasFailed)
            return default;

        Bounds b = default;
        b.center = c;
        b.extents = e;

        return b;
    }

    /// <summary>
    /// Reads a <see cref="Ray"/> from the buffer.
    /// </summary>
    public static Ray ReadRay(this ByteReader reader)
    {
        Vector3 o = default;

        o.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        o.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        o.z = reader.ReadFloat();
        if (reader.HasFailed)
            return default;

        Vector3 d = default;

        d.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        d.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        d.z = reader.ReadFloat();

        return reader.HasFailed ? default : new Ray(o, d);
    }

    /// <summary>
    /// Reads a <see cref="Ray2D"/> from the buffer.
    /// </summary>
    public static Ray2D ReadRay2D(this ByteReader reader)
    {
        Vector2 o = default;

        o.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        o.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;

        Vector2 d = default;

        d.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        d.y = reader.ReadFloat();

        return reader.HasFailed ? default : new Ray2D(o, d);
    }

    /// <summary>
    /// Reads a <see cref="Plane"/> from the buffer.
    /// </summary>
    public static Plane ReadPlane(this ByteReader reader)
    {
        Vector3 n = default;

        n.x = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        n.y = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        n.z = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        float d = reader.ReadFloat();

        return reader.HasFailed ? default : new Plane(n, d);
    }

    /// <summary>
    /// Reads a <see cref="Color32"/> from the buffer.
    /// </summary>
    public static Color32 ReadColor32(this ByteReader reader)
    {
        Span<byte> rgba = stackalloc byte[4];
        if (!reader.ReadBlockTo(rgba))
            return default;

        Color32 c = default;
        c.r = rgba[0];
        c.g = rgba[1];
        c.b = rgba[2];
        c.a = rgba[3];
        return c;
    }

    /// <summary>
    /// Reads a <see cref="Color32"/> from the buffer without the alpha channel (assumed to be 255).
    /// </summary>
    public static Color32 ReadColor32NoAlpha(this ByteReader reader)
    {
        Span<byte> rgb = stackalloc byte[3];
        if (!reader.ReadBlockTo(rgb))
            return default;

        Color32 c = default;
        c.r = rgb[0];
        c.g = rgb[1];
        c.b = rgb[2];
        c.a = 255;
        return c;
    }

    /// <summary>
    /// Reads a <see cref="Color"/> from the buffer.
    /// </summary>
    public static Color ReadColor(this ByteReader reader)
    {
        Color c = default;

        c.r = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.g = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.b = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.a = reader.ReadFloat();

        return reader.HasFailed ? default : c;
    }

    /// <summary>
    /// Reads a <see cref="Color"/> from the buffer without the alpha channel (assumed to be 1).
    /// </summary>
    public static Color ReadColorNoAlpha(this ByteReader reader)
    {
        Color c = default;

        c.r = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.g = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.b = reader.ReadFloat();
        if (reader.HasFailed)
            return default;
        c.a = 1f;

        return reader.HasFailed ? default : c;
    }

    /// <summary>
    /// Reads a nullable <see cref="Vector2"/> from the buffer.
    /// </summary>
    public static Vector2? ReadNullableVector2(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadVector2();
    }

    /// <summary>
    /// Reads a nullable <see cref="Vector3"/> from the buffer.
    /// </summary>
    public static Vector3? ReadNullableVector3(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadVector3();
    }

    /// <summary>
    /// Reads a nullable <see cref="Vector4"/> from the buffer.
    /// </summary>
    public static Vector4? ReadNullableVector4(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadVector4();
    }

    /// <summary>
    /// Reads a nullable <see cref="Rect"/> from the buffer.
    /// </summary>
    public static Rect? ReadNullableRect(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadRect();
    }

    /// <summary>
    /// Reads a nullable <see cref="Quaternion"/> from the buffer.
    /// </summary>
    public static Quaternion? ReadNullableQuaternion(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadQuaternion();
    }

    /// <summary>
    /// Reads a nullable <see cref="Bounds"/> from the buffer.
    /// </summary>
    public static Bounds? ReadNullableBounds(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadBounds();
    }

    /// <summary>
    /// Reads a nullable <see cref="Ray"/> from the buffer.
    /// </summary>
    public static Ray? ReadNullableRay(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadRay();
    }

    /// <summary>
    /// Reads a nullable <see cref="Ray2D"/> from the buffer.
    /// </summary>
    public static Ray2D? ReadNullableRay2D(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadRay2D();
    }

    /// <summary>
    /// Reads a nullable <see cref="Plane"/> from the buffer.
    /// </summary>
    public static Plane? ReadNullablePlane(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadPlane();
    }

    /// <summary>
    /// Reads a nullable <see cref="Color32"/> from the buffer.
    /// </summary>
    public static Color32? ReadNullableColor32(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadColor32();
    }

    /// <summary>
    /// Reads a nullable <see cref="Color32"/> from the buffer without the alpha channel (assumed to be 255).
    /// </summary>
    public static Color32? ReadNullableColor32NoAlpha(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadColor32NoAlpha();
    }

    /// <summary>
    /// Reads a nullable <see cref="Color"/> from the buffer.
    /// </summary>
    public static Color? ReadNullableColor(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadColor();
    }

    /// <summary>
    /// Reads a nullable <see cref="Color"/> from the buffer without the alpha channel (assumed to be 1).
    /// </summary>
    public static Color? ReadNullableColorNoAlpha(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadColorNoAlpha();
    }
}