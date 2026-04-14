using System;

namespace AUtils.Sil;

/// <summary>
/// Ignore property for serialization
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SilIgnoreAttribute : Attribute { }