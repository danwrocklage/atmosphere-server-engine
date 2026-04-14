namespace ACore.Abstractions.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Replace the first char of string with upper char
    /// </summary>
    public static string Capitalize(this string source) => 
        string.Concat(new ReadOnlySpan<char>(new[] {char.ToUpperInvariant(source[0])}), source.AsSpan(1));
}