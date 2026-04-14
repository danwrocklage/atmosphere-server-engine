using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AUtils.Sil;

internal static class Types
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static IEnumerable<Assembly> Assemblies => AppDomain.CurrentDomain.GetAssemblies()
        .Where(x => x.FullName?.StartsWith("System") == false && x.FullName?.StartsWith("Microsoft") == false);

    public static IEnumerable<Type> All => Assemblies
        .SelectMany(x => x.GetTypes())
        .Where(x => x.FullName?.StartsWith("System") == false && x.FullName?.StartsWith("Microsoft") == false);
}