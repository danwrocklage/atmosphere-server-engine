using System.Buffers;
using BenchmarkDotNet.Attributes;
using MessagePack;

namespace AUtils.Sil.Benchmark;

[Sil(1000)]
[MessagePackObject]
public class Model
{
    [Key(0)]
    public string? A { get; set; }
    [Key(1)]
    public int F { get; set; }
    [Key(2)]
    public double L { get; set; }
    [Key(3)]
    public Guid J { get; set; }
}

[SimpleJob(launchCount: 3, warmupCount: 20, iterationCount: 20, invocationCount:10000, id: "SerializationBenchmark")]
public class SerializationBenchmark
{
    private readonly byte[] mBuffer;
    private readonly int mSize;
    private readonly Model mModel;

    public SerializationBenchmark()
    {
        mModel = new Model
        {
            A = "SomeTestModel",
            F = 20234593,
            J = Guid.NewGuid(),
            L = 20938234.9845983495d
        };
        mSize = AUtils.Sil.Sil.OutputSize(mModel);
        mBuffer = new byte[mSize];
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public int SilOutputSize()
    {
        return AUtils.Sil.Sil.OutputSize(mModel);
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public void SilSerializationOnly()
    {
        AUtils.Sil.Sil.Serialize(mModel, mBuffer);
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public void SilSerializationWithSize()
    {
        var size = AUtils.Sil.Sil.OutputSize(mModel);
        var buffer = new byte[size];
        AUtils.Sil.Sil.Serialize(mModel, buffer);
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public void SilSerializationWithPool()
    {
        var size = AUtils.Sil.Sil.OutputSize(mModel);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        AUtils.Sil.Sil.Serialize(mModel, buffer);
        ArrayPool<byte>.Shared.Return(buffer);
    }

    [Benchmark(OperationsPerInvoke = 100)]
    public byte[] MessagePackSerialization()
    {
        return MessagePackSerializer.Serialize(mModel);
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public object SilFullWithSize()
    {
        var size = AUtils.Sil.Sil.OutputSize(mModel);
        var buffer = new byte[size];
        AUtils.Sil.Sil.Serialize(mModel, buffer);
        return Sil.Deserialize(buffer).Result;
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public object SilFullWithPool()
    {
        var size = AUtils.Sil.Sil.OutputSize(mModel);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        AUtils.Sil.Sil.Serialize(mModel, buffer);
        var result = Sil.Deserialize(buffer);
        ArrayPool<byte>.Shared.Return(buffer);
        return result.Result;
    }
    
    [Benchmark(OperationsPerInvoke = 100)]
    public Model MessagePackFull()
    {
        var bytes = MessagePackSerializer.Serialize(mModel);
        return MessagePackSerializer.Deserialize<Model>(new ReadOnlyMemory<byte>(bytes));
    }
}