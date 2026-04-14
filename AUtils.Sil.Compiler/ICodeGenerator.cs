namespace AUtils.Sil.Compiler;

public interface ICodeGenerator
{
    Task Generate(string output, List<(ushort, Type)> types, CancellationToken token = default);
}