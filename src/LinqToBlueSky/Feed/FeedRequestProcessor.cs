using LinqToBlueSky.Common;
using LinqToBlueSky.Provider;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinqToBlueSky.Feed;

public class FeedRequestProcessor<T> : IRequestProcessor<T>, IRequestProcessorWantsJson
{
    /// <summary>
    /// base url for request
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// type of tweet
    /// </summary>
    public FeedType Type { get; set; }

    /// <summary>
    /// UTC date/time to search to
    /// </summary>
    public DateTime EndTime { get; set; }


    /// <summary>
    /// Algorithm used to fetch the feed.
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>
    /// Number of items to fetch.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Lets you page through the feed list.
    /// </summary>
    /// <remarks>
    /// Use cursor from previous response to fetch the next page.
    /// </remarks>
    public string? Cursor { get; set; }

    /// <summary>
    /// extracts parameters from lambda
    /// </summary>
    /// <param name="lambdaExpression">lambda expression with where clause</param>
    /// <returns>dictionary of parameter name/value pairs</returns>
    public Dictionary<string, string> GetParameters(LambdaExpression lambdaExpression)
    {
        var paramFinder =
           new ParameterFinder<FeedQuery>(
               lambdaExpression.Body,
               new List<string> {
                    nameof(Type),
                    nameof(Algorithm),
                    nameof(Limit),
                    nameof(Cursor)
               });

        return paramFinder.Parameters;
    }

    /// <summary>
    /// builds url based on input parameters
    /// </summary>
    /// <param name="parameters">criteria for url segments and parameters</param>
    /// <returns>URL conforming to BlueSky API</returns>
    public Request BuildUrl(Dictionary<string, string> parameters)
    {
        if (parameters.ContainsKey(nameof(Type)))
            Type = RequestProcessorHelper.ParseEnum<FeedType>(parameters[nameof(Type)]);
        else
            throw new ArgumentException($"{nameof(Type)} is required", nameof(Type));

        Type = RequestProcessorHelper.ParseEnum<FeedType>(parameters[nameof(Type)]);

        return Type switch
        {
            FeedType.Timeline => BuildTimelineUrl(parameters),
            _ => throw new InvalidOperationException("The default case of BuildUrl should never execute because a Type must be specified."),
        };
    }

    /// <summary>
    /// Timeline URL
    /// </summary>
    /// <param name="parameters">Parameters to process</param>
    /// <returns><see cref="Request"/> object</returns>
    Request BuildTimelineUrl(Dictionary<string, string> parameters)
    {
        var req = new Request(BaseUrl + "xrpc/app.bsky.feed.getTimeline");
        var urlParams = req.RequestParameters;

        if (parameters.ContainsKey(nameof(Algorithm)))
        {
            Algorithm = parameters[nameof(Algorithm)];
            urlParams.Add(new QueryParameter("algorithm", Algorithm));
        }
        else
        {
            throw new ArgumentException($"{nameof(Algorithm)} is required", nameof(Algorithm));
        }

        if (parameters.ContainsKey(nameof(Limit)))
        {
            Limit = int.Parse(parameters[nameof(Limit)]);
            urlParams.Add(new QueryParameter("limit", Limit.ToString()));
        }
        
        if (parameters.ContainsKey(nameof(Cursor)))
        {
            Cursor = parameters[nameof(Cursor)];
            urlParams.Add(new QueryParameter("cursor", Cursor));
        }

        return req;
    }

    /// <summary>
    /// Transforms response from Twitter into List of Tweets
    /// </summary>
    /// <param name="responseJson">Json response from Twitter</param>
    /// <returns>List of Tweets</returns>
    public virtual List<T> ProcessResults(string responseJson)
    {
        IEnumerable<FeedQuery> feed;

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            feed = new List<FeedQuery> { new() { Algorithm = Algorithm } };
        }
        else
        {
            FeedQuery feedResult = JsonDeserialize(responseJson);
            feed = new List<FeedQuery> { feedResult };
        }

        return feed.OfType<T>().ToList();
    }

    FeedQuery JsonDeserialize(string responseJson)
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(),
            }
        };
        FeedQuery? tweet = JsonSerializer.Deserialize<FeedQuery>(responseJson, options);

        if (tweet == null)
            return new FeedQuery
            {
                Type = Type,
                Algorithm = Algorithm,
                Limit = Limit,
                Cursor = Cursor
            };
        else
            return tweet with
            {
                Type = Type,
                Algorithm = Algorithm,
                Limit = Limit,
                Cursor = Cursor
            };
    }
}
