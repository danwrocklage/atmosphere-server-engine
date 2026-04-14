using System.Reflection;

namespace AGame.Core.Identity;

/// <summary>
/// Types of user access
/// </summary>
public class GrandTypes
{
    static GrandTypes()
    {
        Items = typeof(GrandTypes)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.PropertyType == typeof(string))
            .Select(x => (string) x.GetValue(null))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();
    }
    
    /// <summary>
    /// All grand types in one collection
    /// </summary>
    public static HashSet<string> Items { get; }
    
    /// <summary>
    /// Main frontend client (game)
    /// </summary>
    public static string Client => "client_auth";

    /// <summary>
    /// Public web portal
    /// </summary>
    public static string Web => "web_auth";

    /// <summary>
    /// Web admin portal
    /// </summary>
    public static string WebAdmin => "admin_web_auth";
}