using System.Linq.Expressions;

namespace LinqToBlueSky.Provider;

class ExpressionTreeModifier<T> : ExpressionVisitor
{
    readonly IQueryable<T> queryableItems;

    internal ExpressionTreeModifier(IQueryable<T> items)
    {
        queryableItems = items;
    }

    internal Expression? CopyAndModify(Expression expression)
    {
        return Visit(expression);
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        // Replace the constant BlueSkyQueryable arg with the queryable collection.
        if (c.Type.Name == "BlueSkyQueryable`1")
            return Expression.Constant(queryableItems);
        
        return c;
    }
}
