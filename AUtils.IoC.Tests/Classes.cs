using System.Collections.Generic;
using System.Linq;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedField.Local

namespace AUtils.IoC.Tests;

internal interface ISimpleInterface
{
    string Get();
}

internal interface IGenericInterface<out T>
{
    T GetTyped();
}

internal class NonContainedClass
{
    private readonly AChild1 mChild1;

    public NonContainedClass(AChild1 child1)
    {
        mChild1 = child1;
    }
}

internal interface IGenericInterface2<out T> where T : AbstractBaseClass
{
    AbstractBaseClass GetTyped();
}

internal class OpenGenericClass<T> : IGenericInterface2<T> where T : AbstractBaseClass
{
    private readonly AChild1 mChild1;

    public OpenGenericClass(AChild1 child1)
    {
        mChild1 = child1;
    }

    public AbstractBaseClass GetTyped()
    {
        return mChild1;
    }
}

internal class ClassWithOpenGeneric
{
    private readonly IGenericInterface2<AChild2> mGenericInterface;

    public ClassWithOpenGeneric(IGenericInterface2<AChild2> genericInterface)
    {
        mGenericInterface = genericInterface;
    }

    public AbstractBaseClass GetT() => mGenericInterface.GetTyped();
}

internal class InvalidClass
{
    private readonly NonContainedClass mNonContainedClass;

    public InvalidClass(NonContainedClass nonContainedClass)
    {
        mNonContainedClass = nonContainedClass;
    }
}

internal class SingletonClass
{
    private int mCounter;
    private readonly AChild1 mChild1;

    public SingletonClass(AChild1 child1)
    {
        mChild1 = child1;
        mCounter = 0;
    }

    public void Increment() => mCounter += mChild1.Get();

    public int GetResult() => mCounter;
}

internal class InterfacedClass1 : ISimpleInterface
{
    private readonly AChild2 mChild2;

    public InterfacedClass1(AChild2 child2)
    {
        mChild2 = child2;
    }

    public string Get() => $"Child:{mChild2.Get()} Name:{nameof(InterfacedClass1)}";
}

internal class InterfacedClass2 : ISimpleInterface
{
    private readonly AChild3 mChild3;

    public InterfacedClass2(AChild3 child3)
    {
        mChild3 = child3;
    }

    public string Get() => $"Child:{mChild3.Get()} Name:{nameof(InterfacedClass2)}";
}

internal class GenericInterfacedClass1 : IGenericInterface<AChild1>
{
    private readonly IContainer mContainer;

    public GenericInterfacedClass1(IContainer container)
    {
        mContainer = container;
    }

    public AChild1 GetTyped() => (AChild1) mContainer.Resolve(typeof(AChild1));
}

internal class GenericInterfacedClass2 : IGenericInterface<AChild2>
{
    private readonly IContainer mContainer;

    public GenericInterfacedClass2(IContainer container)
    {
        mContainer = container;
    }

    public AChild2 GetTyped() => (AChild2) mContainer.Resolve(typeof(AChild2));
}

internal class ClassWithInterface
{
    private readonly ISimpleInterface mInterface;

    public ClassWithInterface(ISimpleInterface @interface)
    {
        mInterface = @interface;
    }

    public string GetResult() => mInterface.Get();
}

internal abstract class AbstractBaseClass
{
    public abstract int Get();
}

internal struct NonContainerStruct
{
    public AChild1 AChild1 { get; init; }
}

internal class CustomResolveClass
{
    private readonly AChild1 mAChild1;

    public CustomResolveClass(NonContainerStruct s)
    {
        mAChild1 = s.AChild1;
    }

    public bool IsChildNull() => mAChild1 == null;

    public int Get() => mAChild1.Get();
}

internal class AChild1 : AbstractBaseClass
{
    public override int Get()
    {
        return 10;
    }
}

internal class AChild2 : AbstractBaseClass
{
    public override int Get()
    {
        return 20;
    }
}

internal class AChild3 : AbstractBaseClass
{
    public override int Get()
    {
        return 30;
    }
}

internal class ClassWithEnumerable
{
    private readonly IEnumerable<AbstractBaseClass> mAClasses;

    public ClassWithEnumerable(IEnumerable<AbstractBaseClass> aClasses)
    {
        mAClasses = aClasses;
    }

    public int Count() => mAClasses.Count();

    public int Sum() => mAClasses.Sum(x => x.Get());
}