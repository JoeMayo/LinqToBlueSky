﻿using LinqToBlueSky.Common;
using LinqToBlueSky.Feed;
using LinqToBlueSky.OAuth;
using LinqToBlueSky.Provider;

using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LinqToBlueSky.Tests")]

namespace LinqToBlueSky;

/// <summary>
/// manages access to the BlueSky API
/// </summary>
public partial class BlueSkyContext : IDisposable
{
    //
    // header constants
    //

    internal const string XRateLimitLimitKey = "x-rate-limit-limit";
    internal const string XRateLimitRemainingKey = "x-rate-limit-remaining";
    internal const string XRateLimitResetKey = "x-rate-limit-reset";
    internal const string RetryAfterKey = "Retry-After";
    internal const string XMediaRateLimitLimitKey = "x-mediaratelimit-limit";
    internal const string XMediaRateLimitRemainingKey = "x-mediaratelimit-remaining";
    internal const string XMediaRateLimitResetKey = "x-mediaratelimit-reset";
    internal const string DateKey = "Date";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlueSkyContext"/> class.
    /// </summary>
    /// <param name="authorizer">The authorizer.</param>
    public BlueSkyContext(IAuthorizer authorizer)
        : this(new BlueSkyExecute(authorizer))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlueSkyContext"/> class.
    /// </summary>
    /// <param name="execute">The <see cref="ITwitterExecute"/> object to use.</param>
    public BlueSkyContext(IBlueSkyExecute execute)
    {
        BlueSkyExecutor = execute ?? throw new ArgumentNullException(nameof(execute), $"{nameof(BlueSkyExecutor)} is required.");

        if (string.IsNullOrWhiteSpace(UserAgent))
            UserAgent = L2BSKeys.DefaultUserAgent;

        BaseUrl = "https://bsky.social/";
    }

    /// <summary>
    /// base URL for accessing Twitter API v1.1
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// base URL for accessing Twitter API v2
    /// </summary>
    public string? BaseUrl2 { get; set; }

    /// <summary>
    /// base URL for uploading media
    /// </summary>
    public string? UploadUrl { get; set; }

    /// <summary>
    /// base URL for accessing streaming APIs
    /// </summary>
    public string? StreamingUrl { get; set; }

    /// <summary>
    /// Assign the Log to the context
    /// </summary>
    public TextWriter? Log
    {
        get { return BlueSkyExecute.Log; }
        set { BlueSkyExecute.Log = value; }
    }

    /// <summary>
    /// This contains the JSON string from the Twitter response to the most recent query.
    /// </summary>
    public string? RawResult { get; set; }

    /// <summary>
    /// By default, LINQ to Twitter populates RawResult on TwitterContext and JsonContent on entities. 
    /// Setting this to true turn this off so that RawResult and JsonContent are not populated.
    /// </summary>
    public bool ExcludeRawJson { get; set; }

    //
    // The routines in this region delegate to TwitterExecute
    // which contains the methods for communicating with Twitter.
    // This is necessary so we can make the side-effect methods
    // more testable, using IoC.
    //

    /// <summary>
    /// Gets and sets HTTP UserAgent header
    /// </summary>
    public string? UserAgent
    {
        get
        {
            if (BlueSkyExecutor != null)
                return BlueSkyExecutor.UserAgent;
            else
                return string.Empty;
        }
        set
        {
            if (BlueSkyExecutor != null)
                BlueSkyExecutor.UserAgent = value;
            if (Authorizer != null)
                Authorizer.UserAgent = value;
        }
    }

    /// <summary>
    /// Gets or sets the read write timeout.
    /// </summary>
    /// <value>The read write timeout.</value>
    public int ReadWriteTimeout
    {
        get
        {
            if (BlueSkyExecutor != null)
                return BlueSkyExecutor.ReadWriteTimeout;
            return BlueSkyExecute.DefaultReadWriteTimeout;
        }
        set
        {
            if (BlueSkyExecutor != null)
                BlueSkyExecutor.ReadWriteTimeout = value;
        }
    }

    /// <summary>
    /// Gets and sets HTTP UserAgent header
    /// </summary>
    public int Timeout
    {
        get
        {
            if (BlueSkyExecutor != null)
                return BlueSkyExecutor.Timeout;
            return BlueSkyExecute.DefaultTimeout;
        }
        set
        {
            if (BlueSkyExecutor != null)
                BlueSkyExecutor.Timeout = value;
        }
    }

    /// <summary>
    /// Gets or sets the authorized client on the <see cref="ITwitterExecute"/> object.
    /// </summary>
    public IAuthorizer? Authorizer
    {
        get { return BlueSkyExecutor?.Authorizer; }
        set 
        { 
            if (BlueSkyExecutor != null)
                BlueSkyExecutor.Authorizer = value; 
        }
    }

#if !WINDOWS_UWP
    /// <summary>
    /// Allows setting the IWebProxy for all HTTP requests.
    /// </summary>
    public IWebProxy? Proxy
    {
        get { return Authorizer?.Proxy; }
        set 
        { 
            if (Authorizer != null) 
                Authorizer.Proxy = value; 
        }
    }
#endif

    /// <summary>
    /// Gets the most recent URL executed.
    /// </summary>
    /// <remarks>
    /// Supports debugging.
    /// </remarks>
    public Uri? LastUrl
    {
        get { return BlueSkyExecutor?.LastUrl; }
    }
    
    /// <summary>
    /// Methods for communicating with Twitter.
    /// </summary>
    internal IBlueSkyExecute BlueSkyExecutor { get; set; }

    /// <summary>
    /// retrieves a specified response header, converting it to an int
    /// </summary>
    /// <param name="responseHeader">Response header to retrieve.</param>
    /// <returns>int value from response</returns>
    private int GetResponseHeaderAsInt(string responseHeader)
    {
        int headerVal = -1;
        IDictionary<string, string>? headers = ResponseHeaders;

        if (headers != null &&
            headers.ContainsKey(responseHeader))
        {
            string headerValAsString = headers[responseHeader];

            _ = int.TryParse(headerValAsString, out headerVal);
        }

        return headerVal;
    }

    /// <summary>
    /// retrieves a specified response header, converting it to a DateTime
    /// </summary>
    /// <param name="responseHeader">Response header to retrieve.</param>
    /// <returns>DateTime value from response</returns>
    /// <remarks>Expects a string like: Sat, 26 Feb 2011 01:12:08 GMT</remarks>
    private DateTime? GetResponseHeaderAsDateTime(string responseHeader)
    {
        DateTime? headerVal = null;
        IDictionary<string, string>? headers = ResponseHeaders;

        if (headers != null &&
            headers.ContainsKey(responseHeader))
        {
            string headerValAsString = headers[responseHeader];

            if (DateTime.TryParse(headerValAsString,
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                    out DateTime value))
                headerVal = value;
        }

        return headerVal;
    }
    
    /// <summary>
    /// Response headers from Twitter Queries
    /// </summary>
    public IDictionary<string, string>? ResponseHeaders
    {
        get
        {
            return BlueSkyExecutor?.ResponseHeaders;
        }
    }

    /// <summary>
    /// Max number of requests per minute
    /// returned by the most recent query
    /// </summary>
    /// <remarks>
    /// Returns -1 if information isn't available,
    /// i.e. you haven't performed a query yet
    /// </remarks>
    public int RateLimitCurrent
    {
        get
        {
            return GetResponseHeaderAsInt(XRateLimitLimitKey);
        }
    }

    /// <summary>
    /// Number of requests available until reset
    /// returned by the most recent query
    /// </summary>
    /// <remarks>
    /// Returns -1 if information isn't available,
    /// i.e. you haven't performed a query yet
    /// </remarks>
    public int RateLimitRemaining
    {
        get
        {
            return GetResponseHeaderAsInt(XRateLimitRemainingKey);
        }
    }

    /// <summary>
    /// UTC time in ticks until rate limit resets
    /// returned by the most recent query
    /// </summary>
    /// <remarks>
    /// Returns -1 if information isn't available,
    /// i.e. you haven't performed a query yet
    /// </remarks>
    public int RateLimitReset
    {
        get
        {
            return GetResponseHeaderAsInt(XRateLimitResetKey);
        }
    }

    /// <summary>
    /// UTC time in ticks until rate limit resets
    /// returned by the most recent search query 
    /// that fails with an HTTP 503
    /// </summary>
    /// <remarks>
    /// Returns -1 if information isn't available,
    /// i.e. you haven't exceeded search rate yet
    /// </remarks>
    public int RetryAfter
    {
        get
        {
            return GetResponseHeaderAsInt(RetryAfterKey);
        }
    }

    /// <summary>
    /// Max number of requests per window for
    /// TweetWithMediaAsync and ReplyWithMediaAsync.
    /// </summary>
    public int MediaRateLimitCurrent
    {
        get
        {
            return GetResponseHeaderAsInt(XMediaRateLimitLimitKey);
        }
    }

    /// <summary>
    /// Number of requests available until reset
    /// for TweetWithMediaAsync and ReplyWithMediaAsync.
    /// </summary>
    public int MediaRateLimitRemaining
    {
        get
        {
            return GetResponseHeaderAsInt(XMediaRateLimitRemainingKey);
        }
    }

    /// <summary>
    /// UTC time in ticks until rate limit resets
    /// for TweetWithMediaAsync and ReplyWithMediaAsync.
    /// </summary>
    public int MediaRateLimitReset
    {
        get
        {
            return GetResponseHeaderAsInt(XMediaRateLimitResetKey);
        }
    }

    /// <summary>
    /// Gets the response header Date and converts to a nullable-DateTime
    /// </summary>
    /// <remarks>
    /// Returns null if the headers don't contain a valid Date value
    /// i.e. you haven't performed a query yet or not convertable
    /// </remarks>
    public DateTime? TwitterDate
    {
        get
        {
            return GetResponseHeaderAsDateTime(DateKey);
        }
    }

    /// <summary>
    /// Called by QueryProvider to execute queries
    /// </summary>
    /// <param name="expression">ExpressionTree to parse</param>
    /// <param name="isEnumerable">Indicates whether expression is enumerable</param>
    /// <returns>list of objects with query results</returns>
    public virtual async Task<object> ExecuteAsync<T>(Expression expression, bool isEnumerable)
        where T: class
    {
        // request processor is specific to request type (i.e. Status, User, etc.)
        IRequestProcessor<T> reqProc = CreateRequestProcessor<T>(expression);

        // get input parameters that go on the REST query URL
        Dictionary<string, string> parameters = GetRequestParameters(expression, reqProc);

        // construct REST endpoint, based on input parameters
        Request request = reqProc.BuildUrl(parameters);

        string results;

         //process request through Twitter
        if (request.IsStreaming)
            results = await BlueSkyExecutor.QueryBlueSkyStreamAsync(request).ConfigureAwait(false);
        else
            results = await BlueSkyExecutor.QueryBlueSkyAsync(request, reqProc).ConfigureAwait(false);

        if (!ExcludeRawJson)
            RawResult = results;

        // Transform results into objects
        List<T> queryableList = reqProc.ProcessResults(results);

        // Copy the IEnumerable entities to an IQueryable.
        IQueryable<T> queryableItems = queryableList.AsQueryable();

        // Copy the expression tree that was passed in, changing only the first
        // argument of the innermost MethodCallExpression.
        // -- Transforms IQueryable<T> into List<T>, which is (IEnumerable<T>)
        var treeCopier = new ExpressionTreeModifier<T>(queryableItems);
        Expression newExpressionTree = treeCopier.CopyAndModify(expression);

        // This step creates an IQueryable that executes by replacing Queryable methods with Enumerable methods.
        if (isEnumerable)
            return queryableItems.Provider.CreateQuery(newExpressionTree);

        return queryableItems.Provider.Execute<object>(newExpressionTree);
    }

    /// <summary>
    /// Search the where clause for query parameters
    /// </summary>
    /// <param name="expression">Input query expression tree</param>
    /// <param name="reqProc">Processor specific to this request type</param>
    /// <returns>Name/value pairs of query parameters</returns>
    static Dictionary<string, string> GetRequestParameters<T>(Expression expression, IRequestProcessor<T> reqProc)
    {
        var parameters = new Dictionary<string, string>();

        // GHK FIX: Handle all wheres
        MethodCallExpression[] whereExpressions = new WhereClauseFinder().GetAllWheres(expression);
        foreach (var whereExpression in whereExpressions)
        {
            var lambdaExpression = (LambdaExpression)((UnaryExpression)(whereExpression.Arguments[1])).Operand;

            // translate variable references in expression into constants
            lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

            Dictionary<string, string> newParameters = reqProc.GetParameters(lambdaExpression);
            foreach (var newParameter in newParameters)
            {
                if (!parameters.ContainsKey(newParameter.Key))
                    parameters.Add(newParameter.Key, newParameter.Value);
            }
        }

        return parameters;
    }

    protected internal virtual IRequestProcessor<T> CreateRequestProcessor<T>()
        where T : class
    {
        string requestType = typeof(T).Name;

        return CreateRequestProcessor<T>(requestType);
    }

    /// <summary>
    /// TestMethodory method for returning a request processor
    /// </summary>
    /// <typeparam name="T">type of request</typeparam>
    /// <returns>request processor matching type parameter</returns>
    internal IRequestProcessor<T> CreateRequestProcessor<T>(Expression expression)
        where T: class
    {
        _ = expression ?? throw new ArgumentNullException(nameof(expression), "Expression passed to CreateRequestProcessor must not be null.");

        Type? genericType = new MethodCallExpressionTypeFinder().GetGenericType(expression);

        _ = genericType ?? throw new ArgumentNullException(nameof(expression), "Generic type of Expression passed to CreateRequestProcessor must not be null.");

        return CreateRequestProcessor<T>(genericType.Name);
    }

    protected internal IRequestProcessor<T> CreateRequestProcessor<T>(string requestType)
        where T : class
    {
        string? baseUrl = BaseUrl;

        IRequestProcessor<T> req = requestType switch
        {
            nameof(FeedQuery) => new FeedRequestProcessor<T>(),
            _ => throw new ArgumentException($"Type, {requestType} isn't a supported BlueSky entity.", nameof(requestType))
        };

        if (req.BaseUrl is null)
            req.BaseUrl = baseUrl;

        return req;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (BlueSkyExecutor is IDisposable disposableExecutor)
            {
                disposableExecutor.Dispose();
            }
        }
    }
}