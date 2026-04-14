namespace AUtils.Sil.Compiler.Generators.UE;

internal static class UnrealEngineUtils
{
    internal const string POSTFIX = "Dto";
    
    public static string Postfix(this Type type) => 
        type.Name.EndsWith(POSTFIX, StringComparison.InvariantCultureIgnoreCase) ? "" : POSTFIX;

    public static string UEClassName(this Type type, bool includeU) =>
        $"{(includeU ? "U" : string.Empty)}{type.Name}{type.Postfix()}";
    
    public static string UEType(this Type propertyType)
    {
        if (propertyType.IsEnum)
            return $"E{propertyType.Name}";

        if (propertyType == typeof(object))
            return "TSharedPtr<UObject>";
        
        if (propertyType == typeof(Guid))
            return "FGuid";
        
        if (propertyType == typeof(string))
            return "FString";
        
        if (propertyType.IsArray)
            return $"TArray<{propertyType.GetElementType().UEType()}>";

        if (propertyType == typeof(bool)) return "bool";
        if (propertyType == typeof(char)) return "char";
        if (propertyType == typeof(sbyte)) return "int8";
        if (propertyType == typeof(byte)) return "uint8";
        if (propertyType == typeof(double)) return "double";
        if (propertyType == typeof(float)) return "float";
        if (propertyType == typeof(short)) return "int16";
        if (propertyType == typeof(ushort)) return "uint16";
        if (propertyType == typeof(int)) return "int32";
        if (propertyType == typeof(uint)) return "uint32";
        if (propertyType == typeof(long)) return "int64";
        if (propertyType == typeof(ulong)) return "uint64";

        return UEClassName(propertyType, true);
    }
}