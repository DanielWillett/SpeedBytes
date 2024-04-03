using System.Runtime.Serialization;

namespace DanielWillett.SpeedBytes;

/// <summary>
/// Thrown when a type is given to a <see cref="ByteReader"/> or <see cref="ByteWriter"/> that can't be read automatically.
/// </summary>
[Serializable]
public class AutoEncodeTypeNotFoundException : Exception
{
    /// <summary>
    /// Type that was attempted to auto-encode.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Assembly qualified name of the type that was attempted to auto-encode.
    /// </summary>
    public string TypeName { get; }

    /// <inheritdoc />
    protected AutoEncodeTypeNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        string typeName = info.GetString("Type") ?? string.Empty;
        Type = Type.GetType(typeName, throwOnError: false);
        TypeName = typeName;
    }

    /// <summary>
    /// Create a <see cref="AutoEncodeTypeNotFoundException"/> with a template message based on a type and 
    /// </summary>
    /// <param name="type"></param>
    public AutoEncodeTypeNotFoundException(Type type) :
        base(string.Format(Properties.Localization.AutoEncodeTypeNotFoundExceptionWithTypeName, type.FullName))
    {
        Type = type;
        TypeName = type.AssemblyQualifiedName!;
    }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("Type", TypeName);
    }
}