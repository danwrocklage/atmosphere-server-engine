using System;

namespace AUtils.Sil;

/// <summary>
/// Sil serialization attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class SilAttribute : Attribute
{
    public SilAttribute(ushort index)
    {
        Index = index;
    }

    public ushort? Index { get; }
}