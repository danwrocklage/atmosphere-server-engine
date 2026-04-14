using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AUtils.Sil.Tests;

public class SilTests
{
    [Fact]
    public void SystemTypesSerializationTest()
    {
        var source = CommonSerialize.Create();
        var buffer = new byte[109];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        CommonSerialize.Assert(source, destination);
    }
    
    [Fact]
    public void BytesSerializationTest()
    {
        var source = new SubclassForSizing
        {
            A = null,
            C = new byte[] { 203, 40, 30, 210, 222, 111 },
            Enum = null,
            Float = 235f
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(source.A, ((SubclassForSizing) destination).A);
        Assert.Equal(source.C, ((SubclassForSizing) destination).C);
        Assert.Equal(source.Enum, ((SubclassForSizing) destination).Enum);
        Assert.Equal(source.Float, ((SubclassForSizing) destination).Float);
    }
    
    [Fact]
    public void ObjectSerializationTest()
    {
        var source = new ObjectClass
        {
            A = new SubclassForSizing
            {
                A = null,
                C = new byte[] { 203, 40, 30, 210, 222, 111 },
                Enum = null,
                Float = 235f
            }
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(destination.GetType(), source.GetType());
        Assert.Equal(((SubclassForSizing)source.A).A, ((SubclassForSizing) ((ObjectClass) destination).A).A);
        Assert.Equal(((SubclassForSizing)source.A).C, ((SubclassForSizing) ((ObjectClass) destination).A).C);
        Assert.Equal(((SubclassForSizing)source.A).Enum, ((SubclassForSizing) ((ObjectClass) destination).A).Enum);
        Assert.Equal(((SubclassForSizing)source.A).Float, ((SubclassForSizing) ((ObjectClass) destination).A).Float);
    }
    
    [Fact]
    public void ArraySerializationTest()
    {
        var source = new ArrayClass
        {
            Names = new []{"Name1", "Name2", "Name3"},
            B = 25
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(destination.GetType(), source.GetType());
        Assert.Equal(source.Names, ((ArrayClass) destination).Names);
    }
    
    [Fact]
    public void ObjectArraySerializationTest()
    {
        var source = new ArrayObjectClass
        {
            GuidValue = Guid.NewGuid(),
            Array = new object[]{Guid.NewGuid(), "StringValue"},
            SubClass = null,
            String = "SomeString"
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(destination.GetType(), source.GetType());
        Assert.Equal(source.String, ((ArrayObjectClass) destination).String);
        Assert.Equal(source.GuidValue, ((ArrayObjectClass) destination).GuidValue);
        Assert.Equal(source.SubClass, ((ArrayObjectClass) destination).SubClass);
        Assert.Equal((IEnumerable<object>) source.Array, ((ArrayObjectClass) destination).Array);
    }
    
    [Fact]
    public void ListSerializationTest()
    {
        var source = new ListClass
        {
            Names = new() {"Name1", "Name2", null, "Name3"}
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(destination.GetType(), source.GetType());
        Assert.Equal(source.Names, ((ListClass) destination).Names);
    }

    [Fact]
    public void EmptyStructSerializationTest()
    {
        var source = new EmptyStruct
        {
            Id = Guid.NewGuid(),
            //Id2 = Guid.NewGuid()
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(destination.GetType(), source.GetType());
        Assert.Equal(source.Id, ((EmptyStruct)destination).Id);
    }
    
    [Fact]
    public void DictionarySerializationTest()
    {
        var source = new DictionaryClass()
        {
            Names = new Dictionary<string, object> {{"Name1", 23}, {"Name2", 24}, {"Name3", 25}}
        };
        var buffer = new byte[Sil.OutputSize(source)];
        
        Sil.Serialize(source, buffer);
        var (destination, type) = Sil.Deserialize(buffer);
        
        Assert.NotNull(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(destination.GetType(), source.GetType());
        Assert.Equal(source.Names, ((DictionaryClass) destination).Names);
    }
    
    [Fact]
    public void UnregisteredSerializationTest()
    {
        Assert.Throws<SilException>(() => Sil.Serialize(new UnregisteredClass {A = "Some"}, new Memory<byte>()));
    }

    [Fact]
    public void StructSerializationTest()
    {
        var source = new CommonStruct2()
        {
            F = 123432,
            K = 234.435345f,
            D = new CommonStruct
            {
                A = Guid.NewGuid(),
                F = 123432,
                K = 234.435345f
            }
        };

        var bytes = new Memory<byte>(new byte[54]);
        Sil.Serialize(source, bytes);
        var (destination, type) = Sil.Deserialize(bytes.ToArray());
        
        Assert.NotNull(destination);
        Assert.IsType<CommonStruct2>(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(source.F, ((CommonStruct2)destination).F);
        Assert.Equal(source.K, ((CommonStruct2)destination).K);
        Assert.Equal(source.D.F, ((CommonStruct2)destination).D.F);
        Assert.Equal(source.D.K, ((CommonStruct2)destination).D.K);
    }
    
    [Fact]
    public void CollectionSerializationTest()
    {
        var source = new CollectionClass()
        {
            Dictionary = new Dictionary<string, CommonStruct>
            {
                {"Some1", new CommonStruct {F = 234, K = 25435.23445f}},
                {"Some2", new CommonStruct {F = 235, K = 25436.23445f}},
                {"Some3", new CommonStruct {F = 236, K = 25437.23445f}},
            },
            Enumerable = new []
            {
                CommonSerialize.Create(),
                CommonSerialize.Create(),
                CommonSerialize.Create()
            }
        };

        var bytes = new byte[Sil.OutputSize(source)];
        Sil.Serialize(source, new Memory<byte>(bytes));
        var (destination, type) = Sil.Deserialize(bytes);
        
        Assert.NotNull(destination);
        Assert.IsType<CollectionClass>(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(3, ((CollectionClass)destination).Dictionary.Count);
        Assert.Equal(3, ((CollectionClass)destination).Enumerable.Count());
        foreach (var item in source.Dictionary)
        {
            Assert.True(((CollectionClass)destination).Dictionary.ContainsKey(item.Key));
            Assert.Equal(item.Value.F, ((CollectionClass)destination).Dictionary[item.Key].F);
            Assert.Equal(item.Value.K, ((CollectionClass)destination).Dictionary[item.Key].K);
        }

        var count = source.Enumerable.Count();
        for (var i = 0; i < count; i++)
        {
            CommonSerialize.Assert(source.Enumerable.ElementAt(i), ((CollectionClass)destination).Enumerable.ElementAt(i));
        }
    }
    
    [Fact]
    public void EnumSerializationTest()
    {
        var source = new EnumClass()
        {
            Value = TestEnum.Value2
        };
        
        var bytes = new byte[Sil.OutputSize(source)];
        Sil.Serialize(source, new Memory<byte>(bytes));
        var (destination, type) = Sil.Deserialize(bytes);
        
        Assert.NotNull(destination);
        Assert.IsType<EnumClass>(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(source.Value, ((EnumClass)destination).Value);
    }

    [Fact]
    public void NullableSerializationTest()
    {
        var source = new NullableClass()
        {
            Byte = 234,
            Enum = TestEnum.Value3,
            Id = null
        };

        var bytes = new byte[13];
        Sil.Serialize(source, new Memory<byte>(bytes));
        var (destination, type) = Sil.Deserialize(bytes);

        Assert.NotNull(destination);
        Assert.IsType<NullableClass>(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(source.Byte, ((NullableClass) destination).Byte);
        Assert.Equal(source.Enum, ((NullableClass) destination).Enum);
        Assert.Equal(source.Id, ((NullableClass) destination).Id);
    }
    
    [Fact]
    public void IgnorePropertySerializationTest()
    {
        var source = new IgnorePropertyClass()
        {
            Id = Guid.NewGuid(),
            Id2 = Guid.NewGuid()
        };

        var bytes = new byte[22];
        Sil.Serialize(source, new Memory<byte>(bytes));
        var (destination, type) = Sil.Deserialize(bytes);

        Assert.NotNull(destination);
        Assert.IsType<IgnorePropertyClass>(destination);
        Assert.Equal(destination.GetType(), type);
        Assert.Equal(source.Id, ((IgnorePropertyClass) destination).Id);
        Assert.Equal(default, ((IgnorePropertyClass) destination).Id2);
    }
}