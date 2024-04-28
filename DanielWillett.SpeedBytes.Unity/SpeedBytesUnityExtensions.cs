using System.Runtime.CompilerServices;
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
    [MethodImpl(MethodImplOptions.NoInlining)]
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
    /// Write a <see cref="Vector2"/> to the buffer at half precision.
    /// </summary>
    /// <remarks>In .NET 5.0 there is a Half struct that can be used on non-Unity platforms.</remarks>
    public static void WriteHalfPrecision(this ByteWriter writer, float n)
    {
        writer.Write(Mathf.FloatToHalf(n));
    }

    /// <summary>
    /// Write a <see cref="Vector2"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Vector2 n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
    }

    /// <summary>
    /// Write a <see cref="Vector3"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Vector3 n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.z));
    }

    /// <summary>
    /// Write a <see cref="Vector4"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Vector4 n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.z));
        writer.Write(Mathf.FloatToHalf(n.w));
    }

    /// <summary>
    /// Write a <see cref="Rect"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Rect n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.width));
        writer.Write(Mathf.FloatToHalf(n.height));
    }

    /// <summary>
    /// Write a <see cref="Quaternion"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Quaternion n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.z));
        writer.Write(Mathf.FloatToHalf(n.w));
    }

    /// <summary>
    /// Write a <see cref="Bounds"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Bounds n)
    {
        Vector3 c = n.center;
        Vector3 e = n.extents;
        writer.Write(Mathf.FloatToHalf(c.x));
        writer.Write(Mathf.FloatToHalf(c.y));
        writer.Write(Mathf.FloatToHalf(c.z));
        writer.Write(Mathf.FloatToHalf(e.x));
        writer.Write(Mathf.FloatToHalf(e.y));
        writer.Write(Mathf.FloatToHalf(e.z));
    }

    /// <summary>
    /// Write a <see cref="Ray"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Ray n)
    {
        Vector3 o = n.origin;
        Vector3 d = n.direction;
        writer.Write(Mathf.FloatToHalf(o.x));
        writer.Write(Mathf.FloatToHalf(o.y));
        writer.Write(Mathf.FloatToHalf(o.z));
        writer.Write(Mathf.FloatToHalf(d.x));
        writer.Write(Mathf.FloatToHalf(d.y));
        writer.Write(Mathf.FloatToHalf(d.z));
    }

    /// <summary>
    /// Write a <see cref="Ray2D"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Ray2D n)
    {
        Vector2 o = n.origin;
        Vector2 d = n.direction;
        writer.Write(Mathf.FloatToHalf(o.x));
        writer.Write(Mathf.FloatToHalf(o.y));
        writer.Write(Mathf.FloatToHalf(d.x));
        writer.Write(Mathf.FloatToHalf(d.y));
    }

    /// <summary>
    /// Write a <see cref="Plane"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Plane n)
    {
        Vector3 norm = n.normal;
        writer.Write(Mathf.FloatToHalf(norm.x));
        writer.Write(Mathf.FloatToHalf(norm.y));
        writer.Write(Mathf.FloatToHalf(norm.z));
        writer.Write(Mathf.FloatToHalf(n.distance));
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, Color n)
    {
        writer.Write(Mathf.FloatToHalf(n.r));
        writer.Write(Mathf.FloatToHalf(n.g));
        writer.Write(Mathf.FloatToHalf(n.b));
        writer.Write(Mathf.FloatToHalf(n.a));
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer without the alpha channel (assumed to be 1) at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNoAlpha(this ByteWriter writer, Color n)
    {
        writer.Write(Mathf.FloatToHalf(n.r));
        writer.Write(Mathf.FloatToHalf(n.g));
        writer.Write(Mathf.FloatToHalf(n.b));
    }

    /// <summary>
    /// Write a <see cref="Vector2"/> to the buffer at half precision by reference.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, in Vector2 n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
    }

    /// <summary>
    /// Write a <see cref="Vector3"/> to the buffer at half precision by reference.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, in Vector3 n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.z));
    }

    /// <summary>
    /// Write a <see cref="Vector4"/> to the buffer at half precision by reference.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, in Vector4 n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.z));
        writer.Write(Mathf.FloatToHalf(n.w));
    }

    /// <summary>
    /// Write a <see cref="Rect"/> to the buffer at half precision by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void WriteHalfPrecision(this ByteWriter writer, ref Rect n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.width));
        writer.Write(Mathf.FloatToHalf(n.height));
    }

    /// <summary>
    /// Write a <see cref="Quaternion"/> to the buffer at half precision by reference.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, in Quaternion n)
    {
        writer.Write(Mathf.FloatToHalf(n.x));
        writer.Write(Mathf.FloatToHalf(n.y));
        writer.Write(Mathf.FloatToHalf(n.z));
        writer.Write(Mathf.FloatToHalf(n.w));
    }

    /// <summary>
    /// Write a <see cref="Bounds"/> to the buffer at half precision by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void WriteHalfPrecision(this ByteWriter writer, ref Bounds n)
    {
        Vector3 c = n.center;
        Vector3 e = n.extents;
        writer.Write(Mathf.FloatToHalf(c.x));
        writer.Write(Mathf.FloatToHalf(c.y));
        writer.Write(Mathf.FloatToHalf(c.z));
        writer.Write(Mathf.FloatToHalf(e.x));
        writer.Write(Mathf.FloatToHalf(e.y));
        writer.Write(Mathf.FloatToHalf(e.z));
    }

    /// <summary>
    /// Write a <see cref="Ray"/> to the buffer at half precision by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void WriteHalfPrecision(this ByteWriter writer, ref Ray n)
    {
        Vector3 o = n.origin;
        Vector3 d = n.direction;
        writer.Write(Mathf.FloatToHalf(o.x));
        writer.Write(Mathf.FloatToHalf(o.y));
        writer.Write(Mathf.FloatToHalf(o.z));
        writer.Write(Mathf.FloatToHalf(d.x));
        writer.Write(Mathf.FloatToHalf(d.y));
        writer.Write(Mathf.FloatToHalf(d.z));
    }

    /// <summary>
    /// Write a <see cref="Ray2D"/> to the buffer at half precision by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void WriteHalfPrecision(this ByteWriter writer, ref Ray2D n)
    {
        Vector2 o = n.origin;
        Vector2 d = n.direction;
        writer.Write(Mathf.FloatToHalf(o.x));
        writer.Write(Mathf.FloatToHalf(o.y));
        writer.Write(Mathf.FloatToHalf(d.x));
        writer.Write(Mathf.FloatToHalf(d.y));
    }

    /// <summary>
    /// Write a <see cref="Plane"/> to the buffer at half precision by reference.
    /// </summary>
    /// <remarks><paramref name="n"/> isn't a readonly reference because UnityEngine is too old to have implicitly readonly properties, which would cause a copy to be made anyway.</remarks>
    public static void WriteHalfPrecision(this ByteWriter writer, ref Plane n)
    {
        Vector3 norm = n.normal;
        writer.Write(Mathf.FloatToHalf(norm.x));
        writer.Write(Mathf.FloatToHalf(norm.y));
        writer.Write(Mathf.FloatToHalf(norm.z));
        writer.Write(Mathf.FloatToHalf(n.distance));
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer at half precision by reference.
    /// </summary>
    public static void WriteHalfPrecision(this ByteWriter writer, in Color n)
    {
        writer.Write(Mathf.FloatToHalf(n.r));
        writer.Write(Mathf.FloatToHalf(n.g));
        writer.Write(Mathf.FloatToHalf(n.b));
        writer.Write(Mathf.FloatToHalf(n.a));
    }

    /// <summary>
    /// Write a <see cref="Color"/> to the buffer at half precision by reference without the alpha channel (assumed to be 1).
    /// </summary>
    public static void WriteHalfPrecisionNoAlpha(this ByteWriter writer, in Color n)
    {
        writer.Write(Mathf.FloatToHalf(n.r));
        writer.Write(Mathf.FloatToHalf(n.g));
        writer.Write(Mathf.FloatToHalf(n.b));
    }

    /// <summary>
    /// Write a nullable <see cref="Vector2"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Vector2? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Vector3"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Vector3? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Vector4"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Vector4? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Rect"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Rect? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Quaternion"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Quaternion? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Bounds"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Bounds? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Ray"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Ray? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Ray2D"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Ray2D? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Plane"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Plane? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Color"/> to the buffer at half precision.
    /// </summary>
    public static void WriteHalfPrecisionNullable(this ByteWriter writer, Color? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecision(n.Value);
    }

    /// <summary>
    /// Write a nullable <see cref="Color"/> to the buffer at half precision without the alpha channel (assumed to be 1).
    /// </summary>
    public static void WriteHalfPrecisionNullableNoAlpha(this ByteWriter writer, Color? n)
    {
        if (!n.HasValue)
        {
            writer.Write(false);
            return;
        }

        writer.Write(true);
        writer.WriteHalfPrecisionNoAlpha(n.Value);
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
        float dist = reader.ReadFloat();
        if (reader.HasFailed)
            return default;

        Plane p = default;
        p.normal = n;
        p.distance = dist;

        return p;
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

    /// <summary>
    /// Reads a <see cref="float"/> from the buffer at half precision.
    /// </summary>
    /// <remarks>In .NET 5.0 there is a Half struct that can be used on non-Unity platforms.</remarks>
    public static float ReadHalfPrecisionFloat(this ByteReader reader)
    {
        ushort v = reader.ReadUInt16();
        return reader.HasFailed ? default : Mathf.HalfToFloat(v);
    }

    /// <summary>
    /// Reads a <see cref="Vector2"/> from the buffer at half precision.
    /// </summary>
    public static Vector2 ReadHalfPrecisionVector2(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector2 v = default;

        v.x = Mathf.HalfToFloat(x);
        v.y = Mathf.HalfToFloat(y);

        return v;
    }

    /// <summary>
    /// Reads a <see cref="Vector3"/> from the buffer at half precision.
    /// </summary>
    public static Vector3 ReadHalfPrecisionVector3(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort z = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector3 v = default;

        v.x = Mathf.HalfToFloat(x);
        v.y = Mathf.HalfToFloat(y);
        v.z = Mathf.HalfToFloat(z);

        return v;
    }

    /// <summary>
    /// Reads a <see cref="Vector4"/> from the buffer at half precision.
    /// </summary>
    public static Vector4 ReadHalfPrecisionVector4(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort z = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort w = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector4 v = default;

        v.x = Mathf.HalfToFloat(x);
        v.y = Mathf.HalfToFloat(y);
        v.z = Mathf.HalfToFloat(z);
        v.w = Mathf.HalfToFloat(w);

        return v;
    }

    /// <summary>
    /// Reads a <see cref="Rect"/> from the buffer at half precision.
    /// </summary>
    public static Rect ReadHalfPrecisionRect(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort width = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort height = reader.ReadUInt16();

        return reader.HasFailed ? default : new Rect(Mathf.HalfToFloat(x), Mathf.HalfToFloat(y), Mathf.HalfToFloat(width), Mathf.HalfToFloat(height));
    }

    /// <summary>
    /// Reads a <see cref="Quaternion"/> from the buffer at half precision.
    /// </summary>
    public static Quaternion ReadHalfPrecisionQuaternion(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort z = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort w = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Quaternion q = default;

        q.x = Mathf.HalfToFloat(x);
        q.y = Mathf.HalfToFloat(y);
        q.z = Mathf.HalfToFloat(z);
        q.w = Mathf.HalfToFloat(w);

        return q;
    }

    /// <summary>
    /// Reads a <see cref="Bounds"/> from the buffer at half precision.
    /// </summary>
    public static Bounds ReadHalfPrecisionBounds(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort z = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort w = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort h = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort d = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector3 c = default, e = default;

        c.x = Mathf.HalfToFloat(x);
        c.y = Mathf.HalfToFloat(y);
        c.z = Mathf.HalfToFloat(z);
        e.x = Mathf.HalfToFloat(w);
        e.y = Mathf.HalfToFloat(h);
        e.z = Mathf.HalfToFloat(d);

        Bounds b = default;
        b.center = c;
        b.extents = e;

        return b;
    }

    /// <summary>
    /// Reads a <see cref="Ray"/> from the buffer at half precision.
    /// </summary>
    public static Ray ReadHalfPrecisionRay(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort z = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort dx = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort dy = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort dz = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector3 o = default, d = default;

        o.x = Mathf.HalfToFloat(x);
        o.y = Mathf.HalfToFloat(y);
        o.z = Mathf.HalfToFloat(z);
        d.x = Mathf.HalfToFloat(dx);
        d.y = Mathf.HalfToFloat(dy);
        d.z = Mathf.HalfToFloat(dz);

        return new Ray(o, d);
    }

    /// <summary>
    /// Reads a <see cref="Ray2D"/> from the buffer at half precision.
    /// </summary>
    public static Ray2D ReadHalfPrecisionRay2D(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort dx = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort dy = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector2 o = default, d = default;

        o.x = Mathf.HalfToFloat(x);
        o.y = Mathf.HalfToFloat(y);
        d.x = Mathf.HalfToFloat(dx);
        d.y = Mathf.HalfToFloat(dy);

        return new Ray2D(o, d);
    }

    /// <summary>
    /// Reads a <see cref="Plane"/> from the buffer at half precision.
    /// </summary>
    public static Plane ReadHalfPrecisionPlane(this ByteReader reader)
    {
        ushort x = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort y = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort z = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort dist = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Vector3 o = default;

        o.x = Mathf.HalfToFloat(x);
        o.y = Mathf.HalfToFloat(y);
        o.z = Mathf.HalfToFloat(z);

        Plane p = default;
        p.normal = o;
        p.distance = Mathf.HalfToFloat(dist);

        return p;
    }

    /// <summary>
    /// Reads a <see cref="Color"/> from the buffer at half precision.
    /// </summary>
    public static Color ReadHalfPrecisionColor(this ByteReader reader)
    {
        ushort r = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort g = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort b = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort a = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Color c = default;

        c.r = Mathf.HalfToFloat(r);
        c.g = Mathf.HalfToFloat(g);
        c.b = Mathf.HalfToFloat(b);
        c.a = Mathf.HalfToFloat(a);

        return c;
    }

    /// <summary>
    /// Reads a <see cref="Color"/> from the buffer without the alpha channel (assumed to be 1) at half precision.
    /// </summary>
    public static Color ReadHalfPrecisionColorNoAlpha(this ByteReader reader)
    {
        ushort r = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort g = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        ushort b = reader.ReadUInt16();
        if (reader.HasFailed)
            return default;

        Color c = default;

        c.r = Mathf.HalfToFloat(r);
        c.g = Mathf.HalfToFloat(g);
        c.b = Mathf.HalfToFloat(b);

        return c;
    }

    /// <summary>
    /// Reads a nullable <see cref="Vector2"/> from the buffer at half precision.
    /// </summary>
    public static Vector2? ReadNullableHalfPrecisionVector2(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionVector2();
    }

    /// <summary>
    /// Reads a nullable <see cref="Vector3"/> from the buffer at half precision.
    /// </summary>
    public static Vector3? ReadNullableHalfPrecisionVector3(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionVector3();
    }

    /// <summary>
    /// Reads a nullable <see cref="Vector4"/> from the buffer at half precision.
    /// </summary>
    public static Vector4? ReadNullableHalfPrecisionVector4(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionVector4();
    }

    /// <summary>
    /// Reads a nullable <see cref="Rect"/> from the buffer at half precision.
    /// </summary>
    public static Rect? ReadNullableHalfPrecisionRect(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionRect();
    }

    /// <summary>
    /// Reads a nullable <see cref="Quaternion"/> from the buffer at half precision.
    /// </summary>
    public static Quaternion? ReadNullableHalfPrecisionQuaternion(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionQuaternion();
    }

    /// <summary>
    /// Reads a nullable <see cref="Bounds"/> from the buffer at half precision.
    /// </summary>
    public static Bounds? ReadNullableHalfPrecisionBounds(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionBounds();
    }

    /// <summary>
    /// Reads a nullable <see cref="Ray"/> from the buffer at half precision.
    /// </summary>
    public static Ray? ReadNullableHalfPrecisionRay(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionRay();
    }

    /// <summary>
    /// Reads a nullable <see cref="Ray2D"/> from the buffer at half precision.
    /// </summary>
    public static Ray2D? ReadNullableHalfPrecisionRay2D(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionRay2D();
    }

    /// <summary>
    /// Reads a nullable <see cref="Plane"/> from the buffer at half precision.
    /// </summary>
    public static Plane? ReadNullableHalfPrecisionPlane(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionPlane();
    }

    /// <summary>
    /// Reads a nullable <see cref="Color"/> from the buffer at half precision.
    /// </summary>
    public static Color? ReadNullableHalfPrecisionColor(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionColor();
    }

    /// <summary>
    /// Reads a nullable <see cref="Color"/> from the buffer without the alpha channel (assumed to be 1) at half precision.
    /// </summary>
    public static Color? ReadNullableHalfPrecisionColorNoAlpha(this ByteReader reader)
    {
        if (!reader.ReadBool())
            return null;

        return reader.ReadHalfPrecisionColorNoAlpha();
    }
}