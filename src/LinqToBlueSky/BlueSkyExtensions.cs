using LinqToBlueSky.Provider;

namespace LinqToBlueSky;

public static class BlueSkyExtensions
{
    // TODO: Need to look at whether and/or how to implement this method for BlueSky
    ///// <summary>
    ///// Callback is invoked by LINQ to BlueSky streaming support,
    ///// allowing you to process each individual response from BlueSky.
    ///// For best results, please review the BlueSky API streaming guidelines.
    ///// </summary>
    ///// <param name="streaming">Query being extended</param>
    ///// <param name="callback">Your code for handling BlueSky content</param>
    ///// <returns>Streaming instance to support further LINQ opertations</returns>
    //public static async Task<List<Streaming>> StartAsync(this IQueryable<Streaming> streaming, Func<StreamContent, Task> callback)
    //{
    //    _ = callback ?? throw new ArgumentNullException(nameof(callback), $"{nameof(callback)} is required!");

    //    ITwitterExecute executor = GetTwitterExecutor(streaming);

    //    executor.StreamingCallbackAsync = callback;

    //    return await streaming.ToListAsync().ConfigureAwait(false);
    //}

    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> query)
    {
        var provider = query.Provider as BlueSkyQueryProvider;

        IEnumerable<T> results = (IEnumerable<T>)await provider!.ExecuteAsync<IEnumerable<T>>(query.Expression).ConfigureAwait(false);

        return results.ToList();
    }

    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> query)
        where T : class
    {
        var provider = query.Provider as BlueSkyQueryProvider;

        IEnumerable<T> results = (IEnumerable<T>)await provider!.ExecuteAsync<T>(query.Expression).ConfigureAwait(false);

        return results.FirstOrDefault();
    }

    public static async Task<T> FirstAsync<T>(this IQueryable<T> query)
        where T : class
    {
        var provider = query.Provider as BlueSkyQueryProvider;

        IEnumerable<T> results = (IEnumerable<T>)await provider!.ExecuteAsync<T>(query.Expression).ConfigureAwait(false);

        return results.First();
    }

    public static async Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> query)
        where T : class
    {
        var provider = query.Provider as BlueSkyQueryProvider;

        IEnumerable<T> results = (IEnumerable<T>)await provider!.ExecuteAsync<T>(query.Expression).ConfigureAwait(false);

        return results.SingleOrDefault();
    }

    public static async Task<T> SingleAsync<T>(this IQueryable<T> query)
        where T : class
    {
        var provider = query.Provider as BlueSkyQueryProvider;

        IEnumerable<T> results = (IEnumerable<T>)await provider!.ExecuteAsync<T>(query.Expression).ConfigureAwait(false);

        return results.Single();
    }

    /// <summary>
    /// Enables use of .NET Cancellation Framework for this query.
    /// </summary>
    /// <param name="streaming">Query being extended</param>
    /// <param name="callback">Your code for handling Twitter content</param>
    /// <returns>Streaming instance to support further LINQ opertations</returns>
    public static IQueryable<T> WithCancellation<T>(this IQueryable<T> query, CancellationToken cancelToken)
        where T : class
    {
        IBlueSkyExecute executor = GetTwitterExecutor(query);

        executor.CancellationToken = cancelToken;

        return query;
    }

    /// <summary>
    /// Safely gets a <see cref="TwitterExecutor"/> from an IQueryable<T>
    /// </summary>
    /// <typeparam name="T">IQueryable type</typeparam>
    /// <param name="query">IQueryable for this request</param>
    /// <returns><see cref="TwitterExecutor"/> for the <see cref="BlueSkyContext"/></returns>
    static IBlueSkyExecute GetTwitterExecutor<T>(IQueryable<T> query)
    {
        var executor = (query.Provider as BlueSkyQueryProvider)?.Context?.BlueSkyExecutor;

        _ = executor ?? throw new InvalidOperationException($"{nameof(BlueSkyContext)} not properly instantiated.");

        return executor;
    }
}
