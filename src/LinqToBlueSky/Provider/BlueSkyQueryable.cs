/***********************************************************
 * Credits:
 * 
 * MSDN Documentation -
 * Walkthrough: Creating an IQueryable LINQ Provider
 * 
 * http://msdn.microsoft.com/en-us/library/bb546158.aspx
 * 
 * Matt Warren's Blog -
 * LINQ: Building an IQueryable Provider:
 * 
 * http://blogs.msdn.com/mattwar/default.aspx
 * 
 * Adopted and Modified By: Joe Mayo, 8/26/08
 * *********************************************************/
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToBlueSky.Provider;

/// <summary>
/// IQueryable of T part of LINQ to BlueSky
/// </summary>
/// <typeparam name="T">Type to operate on</typeparam>
public class BlueSkyQueryable<T> : IOrderedQueryable<T>
{
    /// <summary>
    /// init with BlueSkyContext
    /// </summary>
    /// <param name="context"></param>
    public BlueSkyQueryable(BlueSkyContext context)
    {
        Provider = new BlueSkyQueryProvider();
        Expression = Expression.Constant(this);

        // lets provider reach back to BlueSkyContext, 
        // where execute implementation resides
        ((BlueSkyQueryProvider) Provider).Context = context;
    }

    /// <summary>
    /// modified as internal because LINQ to BlueSky is Unusable 
    /// without BlueSkyContext, but provider still needs access
    /// </summary>
    /// <param name="provider">IQueryProvider</param>
    /// <param name="expression">Expression Tree</param>
    internal BlueSkyQueryable(
        BlueSkyQueryProvider provider,
        Expression expression)
    {
        if (provider == null)
        {
            throw new ArgumentNullException("provider");
        }

        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (!typeof(IQueryable<T>).GetTypeInfo().IsAssignableFrom(expression.Type.GetTypeInfo()))
        {
            throw new ArgumentOutOfRangeException("expression");
        }

        Provider = provider;
        Expression = expression;
    }

    /// <summary>
    /// IQueryProvider part of LINQ to Twitter
    /// </summary>
    public IQueryProvider Provider { get; private set; }
    
    /// <summary>
    /// expression tree
    /// </summary>
    public Expression Expression { get; private set; }

    /// <summary>
    /// type of T in IQueryable of T
    /// </summary>
    public Type ElementType
    {
        get { return typeof(T); }
    }

    /// <summary>
    /// executes when iterating over collection
    /// </summary>
    /// <returns>query results</returns>
    public IEnumerator<T> GetEnumerator()
    {
        var tsk = Task.Run(() => (((BlueSkyQueryProvider)Provider).ExecuteAsync<IEnumerable<T>>(Expression)));
        return ((IEnumerable<T>)tsk.Result).GetEnumerator();
    }

    /// <summary>
    /// non-generic execution when collection is iterated over
    /// </summary>
    /// <returns>query results</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return (Provider.Execute<IEnumerable>(Expression)).GetEnumerator();
    }
}
