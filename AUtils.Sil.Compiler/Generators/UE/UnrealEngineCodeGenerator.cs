using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace AUtils.Sil.Compiler.Generators.UE;

[DisplayName("ue")]
[Description("Unreal Engine code generator (C++)")]
internal class UnrealEngineCodeGenerator : ICodeGenerator
{
    internal const string ENUMS_FILE = "AtmDtoEnums";
    internal const string CONSUMER_FILE = "AtmDtoConsumer";
    internal const string MESSAGE_FILE = "AtmDtoBase";
    
    public async Task Generate(string output, List<(ushort, Type)> types, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(output) || !Directory.Exists(output)) 
            throw new ArgumentNullException(nameof(output));

        if (!File.Exists(Path.Combine(output, $"{MESSAGE_FILE}.h")))
            throw new FileNotFoundException($"File '{MESSAGE_FILE}.h' is required");

        var dtosPath = Path.Combine(output, "Dto");
        if (!Directory.Exists(dtosPath))
            Directory.CreateDirectory(dtosPath);

        await WriteEnums(Path.Combine(dtosPath, $"{ENUMS_FILE}.h"), types);
        
        var writers = GetWriters(output);

        foreach (var writer in writers)
            writer.StartWriting();

        foreach (var item in types)
        {
            var dependencies = GetDependencies(item.Item2);
            var classWriter = new UnrealEngineClassWriter(item.Item2, item.Item1,
                types.Where(x => dependencies.Any(d => d.PropertyType == x.Item2)).ToArray());
            await classWriter.WriteMessageClass(dtosPath, token);
            
            foreach (var writer in writers)
                writer.WriteType(item);
        }

        foreach (var writer in writers)
        {
            writer.EndWriting();
            await writer.WriteToFile();
        }
    }

    private static UnrealEngineTemplateWriter[] GetWriters(string output)
    {
        var consumerHeader = Path.Combine(output, $"{CONSUMER_FILE}.h");
        var consumerSource = Path.Combine(output, $"{CONSUMER_FILE}.cpp");

        var writers = new UnrealEngineTemplateWriter[]
        {
            new ClassForwardsWriter(consumerHeader),
            new NativeEventWriter(consumerHeader),

            new EventImplWriter(consumerSource),
            new MessageIncludesWriter(consumerSource),
            new HandlerCallsWriter(consumerSource)
        };
        return writers;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private async Task WriteEnums(string filePath, List<(ushort, Type)> types)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"// Copyright {Program.Configuration.Copyrights}. All Rights Reserved.");
        builder.AppendAutogen();
        builder.AppendLine("#pragma once");
        builder.AppendLine();
        builder.AppendLine("#include \"CoreMinimal.h\"");
        builder.AppendLine();
        
        var enums = types.SelectMany(x => x.Item2.GetProperties())
            .Where(x => x.CanRead && x.CanWrite && 
                        x.GetCustomAttribute<SilIgnoreAttribute>() == null &&
                        x.PropertyType.IsEnum)
            .Select(x => x.PropertyType)
            .Distinct()
            .ToArray();

        foreach (var @enum in enums)
        {
            var enumDescription = @enum.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrEmpty(enumDescription))
            {
                builder.AppendLine("/**");
                builder.AppendLine($" * {enumDescription}");
                builder.AppendLine(" */");
            }

            builder.AppendLine("UENUM(BlueprintType)");
            builder.AppendLine($"enum class E{@enum.Name} : {@enum.GetEnumUnderlyingType().UEType()}");
            builder.AppendLine("{");

            var items = Enum.GetValues(@enum);
            foreach (Enum item in items)
            {
                var name = Enum.GetName(@enum, item) ?? throw new NullReferenceException();
                var display = @enum.GetMember(name)[0]
                    .GetCustomAttribute<DisplayAttribute>();

                var meta = "";
                if (display != null)
                {
                    var metaDescription = string.IsNullOrEmpty(display.Description) ? "" : $"ToolTip = \"{display.Description}\"";
                    var metaDisplayName = string.IsNullOrEmpty(display.Name) ? "" : $"DisplayName = \"{display.Name}\"";
                    
                    var separator =
                        string.IsNullOrEmpty(metaDisplayName) || string.IsNullOrEmpty(metaDescription) ? "" : ", ";
                    
                    meta = $" UMETA({metaDisplayName}{separator}{metaDescription})";
                }

                builder.AppendLine($"    E{name} = {item.ToString("D")}{meta},");
            }

            builder.AppendLine("};");
            builder.AppendLine();
        }

        await using var file = File.Open(filePath, FileMode.Create);
        await using var writer = new StreamWriter(file);
        await writer.WriteAsync(builder);
    }

    private PropertyInfo[] GetDependencies(Type item)
    {
        return item.GetSilProps().Where(x =>
            !x.PropertyType.IsEnum &&
            x.PropertyType.Namespace?.StartsWith("System") == false)
            .ToArray();
    }
}