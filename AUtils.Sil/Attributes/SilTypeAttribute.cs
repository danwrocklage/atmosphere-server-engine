using System;

namespace AUtils.Sil;

/// <summary>
/// Prop serialization parameters. Used for C++
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SilTypeAttribute : Attribute 
{
    /// <summary>
    /// Class name in C++
    /// </summary>
    public string Name { get; set; }
}