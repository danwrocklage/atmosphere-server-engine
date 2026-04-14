using System;
using System.IO;
using System.Threading.Tasks;
using ACore.Tests.Shared;
using ACore.Tests.Shared.Database;
using AUtils.IoC;
using Xunit;

namespace ACore.Patching.Tests;

internal class CounterSingleton
{
    public void Increment(int value = 1) => Value += value;
    
    public int Value { get; private set; }
}

public class PatchingTests
{
    internal const string TestCategory = "TestCategory";
    internal const string TestCategory2 = "TestCategory2";
    internal const string TestCategory3 = "TestCategory3";
    
    private static ContainerBuilder PrepareContainer()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        builder.Singleton<CounterSingleton>();
        var module = new PatchingModule();
        module.ConfigureServices(builder);
        return builder;
    }
    
    [Fact]
    public async Task SimplePatchApplyingTest()
    {
        var builder = PrepareContainer();
        var container = builder.Build();
        container.Resolve<FakeDatabase>().GetFakeRepository<PatchEntity>().RawData.Add(new PatchEntity
        {
            Category = TestCategory,
            Id = Guid.NewGuid(),
            Name = "20200101_01",
            Order = "20200101_01",
            AppliedAt = DateTime.UtcNow.AddMinutes(-10),
            ClrType = typeof(Patch1).FullName
        });
        var patchService = container.Resolve<IPatchService>();

        await patchService.Migrate(TestCategory);

        var counter = container.Resolve<CounterSingleton>();
        Assert.Equal(7, counter.Value);
    }
    
    [Fact]
    public async Task ApplyDbStoredPatchTest()
    {
        var builder = PrepareContainer();
        var container = builder.Build();
        container.Resolve<FakeDatabase>().GetFakeRepository<PatchEntity>().RawData.Add(new PatchEntity
        {
            Category = TestCategory,
            Id = Guid.NewGuid(),
            Name = "20200101_01",
            Order = "20200101_01",
            AppliedAt = null,
            ClrType = typeof(Patch1).FullName
        });
        var patchService = container.Resolve<IPatchService>();

        await patchService.Migrate(TestCategory);

        var counter = container.Resolve<CounterSingleton>();
        Assert.Equal(10, counter.Value);
    }
    
    [Fact]
    public async Task InvalidPatchTest()
    {
        var builder = PrepareContainer();
        var container = builder.Build();
        var patchService = container.Resolve<IPatchService>();

       await Assert.ThrowsAsync<InvalidDataException>(() => patchService.Migrate(TestCategory3));
    }
    
    [Fact]
    public async Task GetPatchesInfo()
    {
        var builder = PrepareContainer();
        var container = builder.Build();
            
        var patches = container.Resolve<FakeDatabase>().GetFakeRepository<PatchEntity>().RawData;
        patches
            .Add(new PatchEntity
            {
                Category = TestCategory,
                Id = Guid.NewGuid(),
                Name = "Some test",
                Order = "20210101_01",
                AppliedAt = null,
                ClrType = "SomeNamespace.SomePatch1"
            });
        patches
            .Add(new PatchEntity
            {
                Category = "New test category",
                Id = Guid.NewGuid(),
                Name = "Some test",
                Order = "20210101_01",
                AppliedAt = null,
                ClrType = "SomeNamespace.SomePatch2"
            });

        var patchService = container.Resolve<IPatchService>();

        var patchInfos = await patchService.GetPatches(TestCategory);
        Assert.Equal(3, patchInfos.Length);
        
        patchInfos = await patchService.GetPatches();
        Assert.Equal(6, patchInfos.Length);
    }
}