using System;
using System.Collections.Generic;
using Xunit;

namespace AUtils.Sil.Tests;

public class OutputSizeTests
{
    #region Test classes

    [Sil(1010)]
    private class OutputSizeTestClass
    {
        public string A { get; set; }
        public byte B { get; set; }
        public OutputSizeTestSubClass C { get; set; }
        public short D { get; set; }
        public int F { get; set; }
        public long H { get; set; }
        public Guid J { get; set; }
        public float K { get; set; }
        public double L { get; set; }
    }
    
    [Sil(1011)]
    private class OutputSizeTestSubClass
    {
        public sbyte C { get; set; }
        public byte[] D { get; set; }
        public ushort E { get; set; }
        public uint G { get; set; }
        public ulong I { get; set; }
    }
    
    [Sil(1012)]
    private struct OutputSizeTestStruct
    {
        public Half B { get; set; }
        public OutputSizeTestSubStruct C { get; set; }
        public DateOnly D { get; set; }
        public DateTime F { get; set; }
        public Index H { get; set; }
        public Range J { get; set; }
    }
    
    [Sil(1013)]
    private struct OutputSizeTestSubStruct
    {
        public sbyte C { get; set; }
        public ushort E { get; set; }
    }
    
    [Sil(1014)]
    private class OutputSizeTestRecursionClass
    {
        public string Name { get; set; }
        public OutputSizeTestRecursionClass Node { get; set; }
    }

    private enum TestEnum : short
    {
        Value1,
        Value2,
        Value3
    }
    
    private enum TestEnum2 : long
    {
        Value1,
        Value2,
        Value3
    }
    
    [Sil(1015)]
    private class OutputSizeTestNullableClass
    {
        public sbyte? C { get; set; }
        public ushort? E { get; set; }
        public TestEnum2? F { get; set; }
        public TestEnum G { get; set; }
    }
    
    [Sil(1017)]
    private class OutputSizeTestArrayClass
    {
        public string[] Names { get; set; }
        public List<OutputSizeTestSubStruct> SubStructs { get; set; }
        
        public IDictionary<string, object> Dictionary { get; set; }
    }

    #endregion
    
    [Fact]
    public void SimpleTypesOutputSizeTest()
    {
        var size = Sil.OutputSize<int>(384_573_495);
        Assert.Equal(8, size);
        size = Sil.OutputSize<short>(23_543);
        Assert.Equal(6, size);
        size = Sil.OutputSize<byte>(20);
        Assert.Equal(5, size);
        size = Sil.OutputSize<long>(20);
        Assert.Equal(12, size);
        size = Sil.OutputSize(Guid.NewGuid());
        Assert.Equal(20, size);
        size = Sil.OutputSize<double>(20);
        Assert.Equal(12, size);
        size = Sil.OutputSize<float>(20);
        Assert.Equal(8, size);
        size = Sil.OutputSize((Half)20);
        Assert.Equal(6, size);
        
        size = Sil.OutputSize("SomeString");
        Assert.Equal(26, size);
        size = Sil.OutputSize(new byte[] {23, 45, 65, 12, 43});
        Assert.Equal(11, size);
        
        size = Sil.OutputSize<object>(null);
        Assert.Equal(4, size);
    }
    
    [Fact]
    public void ClassOutputSizeTest()
    {
        var instance = new OutputSizeTestClass
        {
            A = "TestString",
            B = 2,
            C = new OutputSizeTestSubClass
            {
                C = -23,
                D = new byte[] {23, 21, 23, 23, 34},
                E = 540,
                G = 34593045,
                I = 49358934590345
            },
            D = 2354,
            F = 234246546,
            H = 4574525364346345,
            J = Guid.NewGuid(),
            K = 34534.345345f,
            L = 2432.343453453645745d,
        };
        
        var size = Sil.OutputSize(instance);
        Assert.Equal(119, size);
    }

    [Fact]
    public void StructOutputSizeTest()
    {
        var instance = new OutputSizeTestStruct
        {
            B = Half.NaN,
            C = new OutputSizeTestSubStruct
            {
                C = 20,
                E = 540
            },
            D = DateOnly.FromDayNumber(340),
            F = DateTime.UtcNow,
            H = new Index(235, true),
            J = new Range()
        };
        
        var size = Sil.OutputSize(instance);
        Assert.Equal(56, size);
    }

    [Fact] 
    public void NullableEnumOutputSizeTest()
    {
        var instance = new OutputSizeTestNullableClass()
        {
            C = null,
            E = 230,
            F = null,
            G = TestEnum.Value2
        };
        
        var size = Sil.OutputSize(instance);
        Assert.Equal(16, size);
    }

    [Fact]
    public void RecursionFailedTest()
    {
        Assert.Throws<SilException>(() =>
            (object) Sil.OutputSize(new OutputSizeTestRecursionClass
            {
                Name = "SomeTest",
                Node = new OutputSizeTestRecursionClass {Name = "SomeSubTest"}
            }));
    }
    
    [Fact] 
    public void CollectionsOutputSizeTest()
    {
        var instance = new OutputSizeTestArrayClass()
        {
            Names = new []{ "Some1", "Some2" },
            SubStructs = new List<OutputSizeTestSubStruct>
            {
                new() {C = 20, E = 540},
                new() {C = 34, E = 34554},
                new() {C = 12, E = 45345},
            }
        };
        
        var size = Sil.OutputSize(instance);
        Assert.Equal(85, size);
        
        instance = new OutputSizeTestArrayClass
        {
            Names = new []{ "Some1", "Some2" },
            Dictionary = new Dictionary<string, object>
            {
                {"Some1", new OutputSizeTestSubStruct {C = 20, E = 540}},
                {"Some2", new OutputSizeTestSubStruct {C = 34, E = 34554}},
            }
        };
        
        size = Sil.OutputSize(instance);
        Assert.Equal(108, size);
    }
}