using System;
using System.Collections.Generic;

namespace AUtils.Sil.Tests;

internal class UnregisteredClass
{
    public string A { get; set; }
}

[Sil(1000)]
internal class CommonSerialize
{
    public string A { get; set; }
    public byte B { get; set; }
    public sbyte C { get; set; }
    public short D { get; set; }
    public ushort E { get; set; }
    public int F { get; set; }
    public uint G { get; set; }
    public long H { get; set; }
    public ulong I { get; set; }
    public Guid J { get; set; }
    public float K { get; set; }
    public double L { get; set; }
    public bool M { get; set; }

    public static CommonSerialize Create()
    {
        return new CommonSerialize
        {
            A = "TestString",
            B = 123,
            C = -99,
            D = 25403,
            E = 45032,
            F = -234895403,
            G = 23985934,
            H = -293859347684,
            I = 38475982734293,
            J = Guid.NewGuid(),
            K = 9483235.92834234f,
            L = 23895728935728.8273892374982374d,
            M = true
        };
    }

    public static void Assert(CommonSerialize source, object destination)
    {
        Xunit.Assert.IsType<CommonSerialize>(destination);
        Xunit.Assert.Equal(source.A, ((CommonSerialize)destination).A);
        Xunit.Assert.Equal(source.B, ((CommonSerialize)destination).B);
        Xunit.Assert.Equal(source.C, ((CommonSerialize)destination).C);
        Xunit.Assert.Equal(source.D, ((CommonSerialize)destination).D);
        Xunit.Assert.Equal(source.E, ((CommonSerialize)destination).E);
        Xunit.Assert.Equal(source.F, ((CommonSerialize)destination).F);
        Xunit.Assert.Equal(source.G, ((CommonSerialize)destination).G);
        Xunit.Assert.Equal(source.H, ((CommonSerialize)destination).H);
        Xunit.Assert.Equal(source.I, ((CommonSerialize)destination).I);
        Xunit.Assert.Equal(source.J, ((CommonSerialize)destination).J);
        Xunit.Assert.Equal(source.K, ((CommonSerialize)destination).K);
        Xunit.Assert.Equal(source.L, ((CommonSerialize)destination).L);
        Xunit.Assert.Equal(source.M, ((CommonSerialize)destination).M);
    }
}

[Sil(1001)]
internal struct CommonStruct
{
    public Guid A { get; set; }
    public int F { get; set; }
    public float K { get; set; }
}

[Sil(1002)]
internal struct CommonStruct2
{
    public int F { get; set; }
    public float K { get; set; }
    public CommonStruct D { get; set; }
}

[Sil(1003)]
internal class CollectionClass
{
    public IDictionary<string, CommonStruct> Dictionary { get; set; }
    public IEnumerable<CommonSerialize> Enumerable { get; set; }
}

internal enum TestEnum : short
{
    Value1,
    Value2,
    Value3
}

[Sil(1004)]
internal class EnumClass
{
    public TestEnum Value { get; set; }
}

[Sil(1005)]
internal class NullableClass
{
    public Guid? Id { get; set; }
    public byte? Byte { get; set; }
    public TestEnum? Enum { get; set; }
}

[Sil(1006)]
internal class IgnorePropertyClass
{
    public Guid Id { get; set; }
    
    [SilIgnore]
    public Guid Id2 { get; set; }
}

[Sil(1007)]
internal class ClassForSizing
{
    public string A { get; set; }
    public byte B { get; set; }
    public sbyte C { get; set; }
    public short D { get; set; }
    public ushort E { get; set; }
    public int F { get; set; }
    public uint G { get; set; }
    public long H { get; set; }
    public ulong I { get; set; }
    public Guid J { get; set; }
    public float K { get; set; }
    public double L { get; set; }
    public Guid? Id { get; set; }
    public byte? Byte { get; set; }
    public TestEnum? Enum { get; set; }
    public SubclassForSizing Subclass { get; set; }

    public static ClassForSizing Create()
    {
        return new ClassForSizing
        {
            A = "TestString",
            B = 123,
            C = -99,
            D = 25403,
            E = 45032,
            F = -234895403,
            G = 23985934,
            H = -293859347684,
            I = 38475982734293,
            J = Guid.NewGuid(),
            K = 9483235.92834234f,
            L = 23895728935728.8273892374982374d,
            Byte = System.Byte.MaxValue,
            Enum = TestEnum.Value3,
            Id = null,
            Subclass = new SubclassForSizing
            {
                A = "TestSubstring",
                C = new byte[] {22, 55, 44, 33, 223, 32, 76, 87},
                Enum = null,
                Float = 230
            }
        };
    }
}

[Sil(1008)]
internal class SubclassForSizing
{
    public string A { get; set; }
    public byte[] C { get; set; }
    public float? Float { get; set; }
    public TestEnum? Enum { get; set; }
}

[Sil(1016)]
internal class ObjectClass
{
    public object A { get; set; }
}

[Sil(1018)]
internal class ArrayClass
{
    public string[] Names { get; set; }
    public short B { get; set; }
}

[Sil(1019)]
internal class ListClass
{
    public List<string> Names { get; set; }
}

[Sil(1020)]
internal class DictionaryClass
{
    public IDictionary<string, object> Names { get; set; }
}

[Sil(1022)]
internal struct EmptyStruct
{
    public Guid Id { get; set; }
    
    //public Guid Id2 { get; set; }
}

[Sil(1023)]
internal class ArrayObjectClass
{
    public Guid GuidValue { get; set; }
    
    public string String { get; set; }
    
    public object[] Array { get; set; }
    
    public NullSubClass SubClass { get; set; }
}

[Sil(1024)]
internal class NullSubClass
{
    public string Type { get; set; }
    
    public string Name { get; set; }
}
