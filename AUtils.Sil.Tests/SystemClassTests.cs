using System;
using Xunit;

namespace AUtils.Sil.Tests;

public class SystemClassTests
{
    private void Serialize<T>(T source)
    {
        var bytes = new byte[Sil.OutputSize(source)];
        Sil.Serialize(source, new Memory<byte>(bytes));
        var destination = Sil.Deserialize(bytes);
        
        Assert.NotNull(destination.Result);
        Assert.IsType<T>(destination.Result);
        Assert.Equal(destination.Result.GetType(), destination.ResultType);
        Assert.Equal(source, (T) destination.Result);
    }

    [Fact] public void StringSerializationTest() => Serialize("MyString");
    [Fact] public void ByteSerializationTest() => Serialize<byte>(235);
    [Fact] public void SByteSerializationTest() => Serialize<sbyte>(117);
    [Fact] public void IntSerializationTest() => Serialize<int>(235);
    [Fact] public void UIntSerializationTest() => Serialize<uint>(235);
    [Fact] public void ShortSerializationTest() => Serialize<short>(235);
    [Fact] public void UShortSerializationTest() => Serialize<ushort>(235);
    [Fact] public void LongSerializationTest() => Serialize<long>(235L);
    [Fact] public void ULongSerializationTest() => Serialize<ulong>(235);
    [Fact] public void FloatSerializationTest() => Serialize<float>(235.234f);
    [Fact] public void DoubleSerializationTest() => Serialize<double>(235234234.234234234d);
    [Fact] public void DecimalSerializationTest() => Serialize<decimal>(235.293593456345m);
}