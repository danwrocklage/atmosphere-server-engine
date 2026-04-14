using System.ComponentModel;
using System.Threading.Tasks;

namespace ACore.Patching.Tests;

[Description("Test patch 1")]
internal class Patch1 : Patch
{
    private readonly CounterSingleton mCounter;

    public Patch1(CounterSingleton counter)
    {
        mCounter = counter;
    }

    public override string Order => "20200101_01";
    public override string Category => PatchingTests.TestCategory;
    public override Task Up()
    {
        mCounter.Increment(3);
        return Task.CompletedTask;
    }

    public override Task Down()
    {
        mCounter.Increment(-3);
        return Task.CompletedTask;
    }
}

[Description("Test patch 2")]
internal class Patch2 : Patch
{
    private readonly CounterSingleton mCounter;

    public Patch2(CounterSingleton counter)
    {
        mCounter = counter;
    }

    public override string Order => "20200101_02";
    public override string Category => PatchingTests.TestCategory;
    public override Task Up()
    {
        mCounter.Increment(7);
        return Task.CompletedTask;
    }

    public override Task Down()
    {
        mCounter.Increment(-7);
        return Task.CompletedTask;
    }
}

[Description("Test patch 3")]
internal class Patch3 : Patch
{
    private readonly CounterSingleton mCounter;

    public Patch3(CounterSingleton counter)
    {
        mCounter = counter;
    }

    public override string Order => "20191201_44235";
    public override string Category => PatchingTests.TestCategory2;
    public override Task Up()
    {
        mCounter.Increment(7);
        return Task.CompletedTask;
    }

    public override Task Down()
    {
        mCounter.Increment(-7);
        return Task.CompletedTask;
    }
}

[Description("Invalid order patch")]
internal class InvalidOrderPatch : Patch
{
    private readonly CounterSingleton mCounter;

    public InvalidOrderPatch(CounterSingleton counter)
    {
        mCounter = counter;
    }

    public override string Order => "SOME_NUM";
    public override string Category => PatchingTests.TestCategory3;
    public override Task Up()
    {
        mCounter.Increment(7);
        return Task.CompletedTask;
    }

    public override Task Down()
    {
        mCounter.Increment(-7);
        return Task.CompletedTask;
    }
}