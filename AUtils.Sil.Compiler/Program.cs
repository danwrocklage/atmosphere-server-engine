using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

// ReSharper disable StringLiteralTypo

namespace AUtils.Sil.Compiler;

internal static class Program
{
    /// <summary>
    /// List of supported code generators
    /// </summary>
    private static readonly Dictionary<string, ICodeGenerator> sGenerators = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(x => x.GetInterfaces().Any(i => i == typeof(ICodeGenerator)))
        .ToDictionary(x => x.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName, x => (ICodeGenerator) Activator.CreateInstance(x));
    
    internal static ProgramConfig Configuration { get; private set; }

    internal static async Task Main(string[] args)
    {
        Console.WriteLine("Sil compiler");
        await GetConfig();
        
        Console.WriteLine("Version {0}", Configuration.Version);
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var input = args.FirstOrDefault(x => x.StartsWith("in="))?["in=".Length..];
        if (input == null)
        {
            Console.WriteLine("Required parameter in=<input directory>");
            return;
        }

#if DEBUG
        Console.WriteLine($"Input is {input}");
        Console.WriteLine();
#endif

        var inputFiles = input.Split(';')
            .Where(x =>
            {
                var result = File.Exists(x);
                if (!result)
                    Console.WriteLine("File not found: {0}", x);
                result = Path.GetExtension(x) == ".dll";
                if (!result)
                    Console.WriteLine("File isn't dll: {0}", x);
                return result;
            })
            .Select(x => Assembly.LoadFrom(x))
            .ToArray();

        if (inputFiles.Length == 0)
        {
            Console.WriteLine("Input is invalid");
            return;
        }
        
#if DEBUG
        foreach (var file in inputFiles)
            Console.WriteLine($"Assembly: {file.FullName}");
        Console.WriteLine();
#endif

        var generators = ParseGenerators(args);
        if (!generators.Any())
        {
            Console.WriteLine("There is no generates");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        await Run(inputFiles, generators);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"Total run: {stopwatch.ElapsedMilliseconds}ms");
    }

    private static async Task GetConfig()
    {
        const string cFileName = "configuration.json";
        
        if (!File.Exists(cFileName))
            throw new FileNotFoundException("configuration.json wasn't found");

        await using var jsonFile = File.OpenRead(cFileName);
        Configuration = await JsonSerializer.DeserializeAsync<ProgramConfig>(jsonFile, JsonSerializerOptions.Default, CancellationToken.None);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("        silc in=<input dll's directories with ; separator> <generator name>=<output directory>");
        Console.WriteLine();
        Console.WriteLine("Supported generators:");
        foreach (var generator in sGenerators)
        {
            var description = generator.Value.GetType().GetCustomAttribute<DescriptionAttribute>();
            Console.WriteLine($"    {generator.Key} - {description?.Description ?? "<no description>"}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Command line arguments processing
    /// </summary>
    private static (ICodeGenerator, string)[] ParseGenerators(IEnumerable<string> args)
    {
        var generators = new List<(ICodeGenerator, string)>();
        foreach (var arg in args)
        {
            if (arg.StartsWith("in=")) continue;

            var splitIndex = arg.IndexOf("=", StringComparison.Ordinal);
            if (splitIndex <= 0)
            {
                Console.WriteLine($"Wrong argument [{arg}]");
                continue;
            }
            var name = arg.Substring(0, splitIndex);
            if (!sGenerators.ContainsKey(name))
            {
                Console.WriteLine($"Unsupported generator [{name}]");
                continue;
            }

            var value = arg.Substring(splitIndex + 1);
            generators.Add((sGenerators[name], value));
        }

        return generators.ToArray();
    }

    /// <summary>
    /// Run code generation
    /// </summary>
    private static async Task Run(Assembly[] files, (ICodeGenerator, string)[] generators)
    {
        var types = new List<(ushort, Type)>();
        foreach (var file in files)
            types.AddRange(TypeLoader.LoadTypes(file));
        
        if(types.Any() != true)
        {
            Console.WriteLine("There is no any types for serializing in loaded assemblies");
            return;
        }
        Console.WriteLine($"Generate for {types.Count} types");
        Console.WriteLine();

        foreach (var (codeGenerator, output) in generators)
        {
            Console.WriteLine($"Run {codeGenerator.GetType().Name} for [{output}] path");
            await codeGenerator.Generate(output, types);
        }
    }
}

internal class ProgramConfig
{
    public string Version { get; init; }
    
    public string Copyrights { get; init; }
    
    public string DllSearchPattern { get; init; }
}