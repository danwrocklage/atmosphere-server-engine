using System.Linq.Expressions;

namespace AUtils.IoC.Dependencies;

internal class EnumerableDependency : IDependency
{
    private readonly IDependency[] mElements;
    private readonly Type mDependencyType;
    private Func<IDependency[], object[]> mResolver;

    public EnumerableDependency(IDependency[] elements, Type dependencyType)
    {
        mElements = elements;
        mDependencyType = dependencyType;
    }

    /// <inheritdoc/>
    public Type GetDependencyType() => typeof(IEnumerable<>).MakeGenericType(mDependencyType);

    /// <inheritdoc/>
    public object Get(Type[] genericArgs = null) => GenerateResolver()(mElements);

    private Func<IDependency[], object[]> GenerateResolver()
    {
        if (mResolver != null)
            return mResolver;

        var arrayDependency = mDependencyType.MakeArrayType();

        var dependencies = Expression.Parameter(typeof(IDependency[]), "dependencies");
        var parameters = Expression.Variable(arrayDependency, "parameters");
        var i = Expression.Variable(typeof(int), "i");
            
        var returnLabel = Expression.Label(typeof(object[]), "return");
        var breakLabel = Expression.Label("break");
            
        var getLength = Expression.Property(dependencies, nameof(Array.Length));
        var body = Expression.Block(new[] {parameters, i},
            Expression.Assign(i, Expression.Constant(0)),
            Expression.Assign(parameters, Expression.NewArrayBounds(mDependencyType, getLength)),
            Expression.Loop(
                Expression.Block(
                    Expression.IfThen(
                        Expression.GreaterThanOrEqual(i, getLength),
                        Expression.Break(breakLabel)),
                    Expression.Assign(
                        Expression.ArrayAccess(parameters, i),
                        Expression.Convert(
                            Expression.Call(Expression.ArrayIndex(dependencies, i), nameof(IDependency.Get),
                                Type.EmptyTypes, Expression.Constant(null, typeof(Type[]))), mDependencyType)),
                    Expression.PostIncrementAssign(i)
                )),
            Expression.Label(breakLabel),
            Expression.Return(returnLabel, parameters),
            Expression.Label(returnLabel, Expression.Default(arrayDependency)));
            
        var resolverExpression = Expression.Lambda<Func<IDependency[], object[]>>(body, dependencies);
        mResolver = resolverExpression.Compile();
            
        return mResolver;
    }
}