using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AUtils.IoC.Tests;

public class ContainerTests
{
    private IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.Register(x => x.For<AChild1>().As<AbstractBaseClass>().AsSelf());
        builder.Register(x => x.For<AChild2>().As<AbstractBaseClass>().AsSelf());
        builder.Register(x => x.For<AChild3>().As<AbstractBaseClass>().AsSelf());
        builder.Register(x => x.For<ClassWithEnumerable>().AsSelf());
        builder.Register(x => x.For<InterfacedClass1>().As<ISimpleInterface>());
        builder.Register(x => x.For<InterfacedClass2>().As<ISimpleInterface>());
        builder.Register(x => x.For<GenericInterfacedClass1>().As<IGenericInterface<AChild1>>());
        builder.Register(x => x.For<GenericInterfacedClass2>().As<IGenericInterface<AChild2>>());
        builder.Register(x => x.For<ClassWithInterface>().AsSelf());
        builder.Register(x => x.For<ClassWithOpenGeneric>().AsSelf());
        builder.Register(x => x.For(typeof(OpenGenericClass<>)).As(typeof(IGenericInterface2<>)));
        builder.Register(x => x.For((c, _) => new CustomResolveClass(new NonContainerStruct { AChild1 = c.Resolve<AChild1>()})).AsSelf());
        return builder.Build();
    }
    
    [Fact]
    public void ContainerBuildTest()
    {
        var container = BuildContainer();
        Assert.NotNull(container);
    }
    
    [Fact]
    public void IsRegisteredTest()
    {
        var builder = new ContainerBuilder();
        builder.Register(x => x.For<InterfacedClass1>().As<ISimpleInterface>());
        Assert.True(builder.IsRegistered<ISimpleInterface>());
        
        Assert.False(builder.IsRegistered(typeof(IGenericInterface2<>)));
        builder.Register(x => x.For(typeof(OpenGenericClass<>)).As(typeof(IGenericInterface2<>)));
        Assert.True(builder.IsRegistered(typeof(IGenericInterface2<>)));
        
        Assert.False(builder.IsRegistered<NonContainedClass>());
    }
    
    [Fact]
    public void ContainerFailBuildTest()
    {
        var builder = new ContainerBuilder();
        builder.Register(x => x.For<AChild1>().AsSelf());
        builder.Register(x => x.For<InvalidClass>().AsSelf());

        var exceptionThrown = false;
        try
        {
            builder.Build();
        }
        catch (Exception e)
        {
            Assert.IsType<AggregateException>(e);
            Assert.Single(((AggregateException)e).InnerExceptions);
            Assert.IsType<ResolveException>(((AggregateException)e).InnerExceptions.First());
            exceptionThrown = true;
        }
        
        Assert.True(exceptionThrown);
    }
    
    [Fact]
    public void SimpleGenericResolveTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<AChild1>();
        Assert.NotNull(item);
        Assert.Equal(10, item.Get());
    }
    
    [Fact]
    public void OpenGenericResolveTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<IGenericInterface2<AChild1>>();
        Assert.NotNull(item);
        Assert.IsType<AChild1>(item.GetTyped());
    }
    
    [Fact]
    public void ContainerOpenGenericResolveTest()
    {
        var container = BuildContainer();
        var runtimeContainer = container.Resolve<IContainer>();
        var item = runtimeContainer.Resolve<IGenericInterface2<AChild1>>();
        Assert.NotNull(item);
        Assert.IsType<AChild1>(item.GetTyped());
    }
    
    [Fact]
    public void ClassWithOpenGenericResolveTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<ClassWithOpenGeneric>();
        Assert.NotNull(item);
        Assert.Equal(10, item.GetT().Get());
    }
    
    [Fact]
    public void SimpleTypedResolveTest()
    {
        var container = BuildContainer();
        var item = container.Resolve(typeof(AChild1));
        Assert.NotNull(item);
        Assert.IsType<AChild1>(item);
        Assert.Equal(10, ((AChild1) item).Get());
    }
    
    [Fact]
    public void ResolveAllTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<AbstractBaseClass[]>();
        Assert.NotNull(item);
        Assert.Equal(3, item.Length);
        Assert.IsType<AChild1>(item[0]);
        Assert.IsType<AChild2>(item[1]);
        Assert.IsType<AChild3>(item[2]);
    }
    
    [Fact]
    public void ResolveEnumerableDependencyTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<ClassWithEnumerable>();
        Assert.NotNull(item);
        Assert.Equal(3, item.Count());
        Assert.Equal(60, item.Sum());
    }

    [Fact]
    public void FailResolveTest()
    {
        var container = BuildContainer();
        Assert.Throws<ArgumentException>(() => container.Resolve<NonContainedClass>());
    }
    
    [Fact]
    public void SingletonTest()
    {
        var builder = new ContainerBuilder();
        builder.Register(x => x.For<AChild1>().AsSelf());
        builder.Register(x => x.For<SingletonClass>().AsSelf().Singleton());
        var container = builder.Build();

        var singleton = container.Resolve<SingletonClass>();
        singleton.Increment();
        
        container.Resolve<SingletonClass>().Increment();
        
        Assert.Equal(20, container.Resolve<SingletonClass>().GetResult());
    }
    
    [Fact]
    public void TransientTest()
    {
        var builder = new ContainerBuilder();
        builder.Register(x => x.For<AChild1>().AsSelf());
        builder.Register(x => x.For<SingletonClass>().AsSelf());
        var container = builder.Build();
        
        container.Resolve<SingletonClass>().Increment();
        container.Resolve<SingletonClass>().Increment();
        
        Assert.Equal(0, container.Resolve<SingletonClass>().GetResult());
    }

    [Fact]
    public void CustomResolverTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<CustomResolveClass>();
        
        Assert.NotNull(item);
        Assert.False(item.IsChildNull());
        Assert.Equal(10, item.Get());
    }
    
    [Fact]
    public void SimpleInterfaceTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<ISimpleInterface>();
        
        Assert.NotNull(item);
        Assert.Equal("Child:20 Name:InterfacedClass1", item.Get());
        
        var items = container.Resolve<IEnumerable<ISimpleInterface>>().ToArray();
        
        Assert.NotNull(items);
        Assert.Equal(2, items.Length);
        Assert.Equal("Child:30 Name:InterfacedClass2", items[1].Get());
        
        var item2 = container.Resolve<ClassWithInterface>();
        
        Assert.NotNull(item2);
        Assert.Equal("Child:20 Name:InterfacedClass1", item2.GetResult());
    }
    
    [Fact]
    public void GenericInterfaceTest()
    {
        var container = BuildContainer();
        var item = container.Resolve<IGenericInterface<AChild1>>();
        
        Assert.NotNull(item);
        Assert.Equal(10, item.GetTyped().Get());
    }
}