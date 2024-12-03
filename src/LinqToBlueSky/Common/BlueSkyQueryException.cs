using LinqToBlueSky.Net;

using System.Net;

namespace LinqToBlueSky.Common;

// TODO: Error handling is likely much different from Twitter.

/// <summary>
/// Use for errors returned from HTTP GET and POST to Twitter
/// </summary>
/// <remarks>
/// The properties commented as "assigned by Twitter" are error details from the Twitter API itself.
/// </remarks>
public class BlueSkyQueryException : InvalidQueryException
{
    /// <summary>
    /// init exception with general message - 
    /// you should probably use one of the other
    /// constructors for a more meaninful exception.
    /// </summary>
    public BlueSkyQueryException()
        : this("BlueSky returned an error from your query.") { }

    /// <summary>
    /// init exception with custom message
    /// </summary>
    /// <param name="message">message to display</param>
    public BlueSkyQueryException(string message)
        : base (message) { }

    /// <summary>
    /// init exception with custom message and chain to originating exception
    /// </summary>
    /// <param name="message">custom message</param>
    /// <param name="inner">originating exception</param>
    public BlueSkyQueryException(string message, Exception inner)
        : base(message, inner) { }

    /// <summary>
    /// Error title - assigned by Twitter
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Error details - assigned by Twitter
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Type of error - assigned by Twitter
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Specific errors - assigned by Twitter
    /// </summary>
    public List<Error>? Errors { get; set; }

    /// <summary>
    /// Http status code from Twitter response
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Http status reason from Twitter response
    /// </summary>
    public string? ReasonPhrase { get; set; }
}
