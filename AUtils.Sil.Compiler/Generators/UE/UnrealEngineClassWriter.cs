using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AUtils.Sil.Compiler.Generators.UE;

internal class UnrealEngineClassWriter
{
    private readonly Type mClrType;
    private readonly ushort mClrTypeIndex;
    private readonly string[] mDependencyHeaders; 

    public UnrealEngineClassWriter(Type clrType, ushort clrTypeIndex, (ushort, Type)[] dependencies)
    {
        mClrType = clrType;
        mClrTypeIndex = clrTypeIndex;
        mDependencyHeaders = dependencies
            .Select(x => GetFileName(x.Item1, x.Item2)).ToArray();
    }
    
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task WriteMessageClass(string path, CancellationToken token = default)
    {
        var fileName = GetFileName(mClrTypeIndex, mClrType);
        var builder = new StringBuilder();

        var silProps = mClrType.GetSilProps();
        
        builder.AppendLine($"// {Program.Configuration.Copyrights}");
        builder.AppendAutogen();
        builder.AppendLine("#pragma once");
        builder.AppendLine();
        
        WriteIncludes(builder, silProps, fileName);

        builder.AppendDescription(mClrType);
        builder.AppendLine("UCLASS(Blueprintable)");
        builder.AppendLine($"class ATMOSPHERECLIENT_API {mClrType.UEClassName(true)} : public U{UnrealEngineCodeGenerator.MESSAGE_FILE}");
        builder.AppendLine("{");
        builder.AppendLine("    GENERATED_BODY()");
        builder.AppendLine("public:");

        foreach (var prop in silProps)
        {
            builder.AppendDescription(prop.PropertyType, 1);
            
            builder.AppendLine("    UPROPERTY(BlueprintReadWrite)");
            builder.AppendLine($"    {prop.PropertyType.UEType()} {prop.Name};");
            builder.AppendLine();
        }
        builder.AppendLine("    FORCEINLINE virtual void Serialize(FArchive& Ar) override");
        builder.AppendLine("    {");
        builder.AppendLine("        const bool IsLoading = !Ar.IsSaving();");
        builder.AppendLine("        ");
        builder.AppendLine("        if(!IsLoading)");
        builder.AppendLine("        {");
        builder.AppendLine("            int8 StartToken = 0;");
        builder.AppendLine("            Ar << StartToken;");
        builder.AppendLine("            int16_t TypeToken = GetTypeId();");
        builder.AppendLine("            Ar << TypeToken;");
        builder.AppendLine("        }");
        builder.AppendLine("");
        
        foreach (var prop in silProps)
            WritePropSerializer(prop, builder);
        
        builder.AppendLine("        int8 EndToken = 1;");
        builder.AppendLine("        Ar << EndToken;");
        builder.AppendLine("    }");

        builder.AppendLine($"    virtual uint16 GetTypeId() const override {{ return {mClrTypeIndex}; }}");
        
        builder.AppendLine("    FORCEINLINE virtual uint16 GetMessageSize() override");
        builder.AppendLine("    {");

        builder.AppendLine("    return");

        foreach (var silProp in silProps)
        {
            builder.Append($"/* {silProp.Name} */");
            builder.Append(GetPropSize(silProp));
            builder.Append(" + ");
        }
        
        builder.AppendLine("        0;");
        
        builder.AppendLine("    }");

        builder.AppendLine("};");
        builder.AppendLine("");

        var filePath = Path.Combine(path, $"{fileName}.h");
        await using var file = File.Open(filePath, FileMode.OpenOrCreate);
        await using var writer = new StreamWriter(file);
        await writer.WriteAsync(builder, token);
    }

    private string GetPropSize(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return $"{property.Name}.Len() * {sizeof(char)}";
        if (property.PropertyType.GetCustomAttribute<SilAttribute>() != null)
            return $"{property.Name}.GetMessageSize();";
        if (property.PropertyType.IsEnum)
            return $"{Marshal.SizeOf(property.PropertyType.GetEnumUnderlyingType())};";
        if (property.PropertyType == typeof(Guid))
            return $"{Marshal.SizeOf(property.PropertyType)};";
        if (property.PropertyType.IsArray)
            return $"GetArraySize({property.Name})";
        if (property.PropertyType is {IsValueType: true, Namespace: "System"})
            return $"{Marshal.SizeOf(property.PropertyType)};";
        return "/* Unsupported type */ 0";
    }

    private string GetFileName(ushort index, Type type) => $"{index}_{type.UEClassName(true)}";

    private void WriteIncludes(StringBuilder builder, PropertyInfo[] silProps, string fileName)
    {
        builder.AppendLine("#include \"CoreMinimal.h\"");
        builder.AppendLine();

        builder.AppendLine($"#include \"../{UnrealEngineCodeGenerator.MESSAGE_FILE}.h\"");

        if (mDependencyHeaders.Length > 0)
        {
            builder.AppendLine("// Dependency includes");
            foreach (var dependency in mDependencyHeaders)
                builder.AppendLine($"#include \"{dependency}.h\"");
            builder.AppendLine();
        }

        if (silProps.Any(x => x.PropertyType.IsEnum))
        {
            builder.AppendLine("// Enum dependencies");
            builder.AppendLine($"#include \"{UnrealEngineCodeGenerator.ENUMS_FILE}.h\"");
            builder.AppendLine();
        }

        builder.AppendLine($"#include \"{fileName}.generated.h\"");
        builder.AppendLine();
    }

    private void WritePropSerializer(PropertyInfo property, StringBuilder builder)
    {
        builder.AppendLine($"        // ------ {property.Name} ------");
        if (property.PropertyType == typeof(string))
        {
            builder.AppendLine($"        SerializeString(Ar, {property.Name});");
            builder.AppendLine();
            return;
        }

        if (property.PropertyType.GetCustomAttribute<SilAttribute>() != null)
        {
            builder.AppendLine($"        uint16 {property.Name}Size = {GetPropSize(property)};");
            builder.AppendLine($"        {property.Name}.Serialize(Ar);");
            builder.AppendLine();
            return;
        }
        
        if (property.PropertyType.IsEnum)
        {
            builder.AppendLine($"        uint16 {property.Name}Size = {GetPropSize(property)};");
            builder.AppendLine($"        Ar << {property.Name}Size;");
            builder.AppendLine($"        Ar << {property.Name};");
            
        }
        else if (property.PropertyType == typeof(Guid))
        {
            builder.AppendLine($"        uint16 {property.Name}Size = {GetPropSize(property)};");
            builder.AppendLine($"        Ar << {property.Name}Size;");
            builder.AppendLine($"        {property.Name}.Serialize(Ar);");
        }
        else if (property.PropertyType.IsArray)
        {
            builder.AppendLine("        if(Ar.IsSaving())");
            builder.AppendLine("        {");
            
            builder.AppendLine("            int16_t size = 0;");
            builder.AppendLine("            int16_t size = 0;");
            builder.AppendLine("            int16_t size = 0;");
            
            builder.AppendLine("        }");
            builder.AppendLine("        else");
            builder.AppendLine("        {");
            
            builder.AppendLine("        }");

        }
        
        else if (property.PropertyType is {IsValueType: true, Namespace: "System"})
        {
            builder.AppendLine($"        uint16 {property.Name}Size = {GetPropSize(property)};");
            builder.AppendLine($"        Ar << {property.Name}Size;");
            builder.AppendLine($"        Ar << {property.Name};");
        }
        builder.AppendLine();
    }
}