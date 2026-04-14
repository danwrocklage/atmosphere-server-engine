using System.Text;

namespace AUtils.Sil.Compiler.Generators.UE;

public abstract class UnrealEngineTemplateWriter
{
    private readonly string mFile;

    protected UnrealEngineTemplateWriter(string header, string file)
    {
        if (string.IsNullOrEmpty(header))
            throw new ArgumentNullException(nameof(header));
        
        if (string.IsNullOrEmpty(file))
            throw new ArgumentNullException(nameof(file));
        
        mFile = file;
        Header = header.ToUpperInvariant();
    }

    protected string Header { get; }
    
    protected StringBuilder Builder { get; } = new();

    public virtual void StartWriting()
    {
        Builder.AppendLine($"/* --- {Header} --- */");
        Builder.AppendAutogen();
    }

    public abstract void WriteType((ushort, Type) item);

    public virtual void EndWriting()
    {
        Builder.AppendLine($"/* --- END {Header} --- */");
        Builder.AppendLine();
    }
    
    public async Task WriteToFile()
    {
        if(!File.Exists(mFile))
        {
            Console.WriteLine($"[{GetType().Name}] File ({mFile}) were not found");
            return;
        }
        
        var anchor = $"/* --- {Header} --- */";
        var endAnchor = $"/* --- END {Header} --- */";
        
        var header = await File.ReadAllTextAsync(mFile);
        var startIndex = header.IndexOf(anchor, StringComparison.InvariantCultureIgnoreCase);
        var endIndex = header.IndexOf(endAnchor, StringComparison.InvariantCultureIgnoreCase);
        var oldContent = header.Substring(startIndex, endIndex + endAnchor.Length - startIndex);
        header = header.Replace(oldContent, Builder.ToString());
        await File.WriteAllTextAsync(mFile, header);
    }
}

public class NativeEventWriter : UnrealEngineTemplateWriter
{
    public NativeEventWriter(string file) : base("MESSAGE HANDLERS", file) { }

    public override void WriteType((ushort, Type) item)
    {
        Builder.AppendLine($"	UFUNCTION(BlueprintNativeEvent) void On{item.Item2.Name}MessageHandle({item.Item2.UEClassName(true)}* Message);");
    }
}

public class ClassForwardsWriter : UnrealEngineTemplateWriter
{
    public ClassForwardsWriter(string file) : base("MESSAGE CLASS FORWARDS", file) { }

    public override void WriteType((ushort, Type) item)
    {
        Builder.AppendLine($"	class {item.Item2.UEClassName(true)};");
    }
}

public class MessageIncludesWriter : UnrealEngineTemplateWriter
{
    public MessageIncludesWriter(string file) : base("MESSAGE INCLUDES", file) { }

    public override void WriteType((ushort, Type) item)
    {
        Builder.AppendLine($"#include \"Dto/{item.Item1}_{item.Item2.Name}{item.Item2.Postfix()}.h\"");
    }
}

public class HandlerCallsWriter : UnrealEngineTemplateWriter
{
    public HandlerCallsWriter(string file) : base("HANDLER CALLS", file) { }

    public override void WriteType((ushort, Type) item)
    {
        Builder.AppendLine($"   case {item.Item1}: {{ Target->On{item.Item2.Name}MessageHandle(Cast<{item.Item2.UEClassName(true)}>(Message)); break; }}");
    }
}

public class EventImplWriter : UnrealEngineTemplateWriter
{
    public EventImplWriter(string file) : base("MESSAGE IMPLEMENTATIONS", file) { }

    public override void WriteType((ushort, Type) item)
    {
        Builder.AppendLine($"void UAtmDtoConsumer::On{item.Item2.Name}MessageHandle_Implementation({item.Item2.UEClassName(true)}* Message)");
        Builder.AppendLine("{");
        Builder.AppendLine($"	UE_LOG(LogAtmosphereClient, Warning, TEXT(\"Got {item.Item2.UEClassName(true)}. Not implemented\"));");
        Builder.AppendLine("	check(0);");
        Builder.AppendLine("}");
        Builder.AppendLine();
    }
}