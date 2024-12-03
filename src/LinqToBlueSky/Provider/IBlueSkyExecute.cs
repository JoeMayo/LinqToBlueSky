﻿using LinqToBlueSky.OAuth;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToBlueSky.Provider;

/// <summary>
/// Members for communicating with Twitter
/// </summary>
public interface IBlueSkyExecute
{
    /// <summary>
    /// Gets or sets the object that can send authorized requests to Twitter.
    /// </summary>
    IAuthorizer? Authorizer { get; set; }

    /// <summary>
    /// Gets the most recent URL executed
    /// </summary>
    /// <remarks>
    /// This is very useful for debugging
    /// </remarks>
    Uri? LastUrl { get; }

    /// <summary>
    /// list of response headers from query
    /// </summary>
    IDictionary<string, string>? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets and sets HTTP UserAgent header
    /// </summary>
    string? UserAgent { get; set; }

    /// <summary>
    /// Timeout (milliseconds) for writing to request 
    /// stream or reading from response stream
    /// </summary>
    int ReadWriteTimeout { get; set; }

    /// <summary>
    /// Timeout (milliseconds) to wait for a server response
    /// </summary>
    int Timeout { get; set; }

    /// <summary>
    /// Performs HTTP POST, with JSON payload, to BlueSky.
    /// </summary>
    /// <param name="method">Delete, Post, or Put</param>
    /// <param name="url">URL of request.</param>
    /// <param name="postData">URL parameters to post.</param>
    /// <param name="postObj">Serializable payload object.</param>
    /// <param name="getResult">Callback for handling async Json response - null if synchronous.</param>
    /// <returns>JSON Response from BlueSKy - empty string if async.</returns>
    Task<string> SendJsonToBlueSkyAsync<T>(string method, string url, IDictionary<string, string> postData, T postObj, CancellationToken cancelToken);

    /// <summary>
    /// performs HTTP POST to BlueSky
    /// </summary>
    /// <param name="method">Delete, Post, or Put</param>
    /// <param name="url">URL of request</param>
    /// <param name="postData">parameters to post</param>
    /// <param name="getResult">callback for handling async Json response - null if synchronous</param>
    /// <returns>Json Response from BlueSky - empty string if async</returns>
    Task<string> PostFormUrlEncodedToBlueSkyAsync<T>(string method, string url, IDictionary<string, string?> postData, CancellationToken cancelToken);

    /// <summary>
    /// Performs HTTP POST media byte array upload to Twitter.
    /// </summary>
    /// <param name="url">Url to upload to.</param>
    /// <param name="postData">Request parameters.</param>
    /// <param name="data">Image to upload.</param>
    /// <param name="name">Image parameter name.</param>
    /// <param name="fileName">Image file name.</param>
    /// <param name="contentType">Type of image: must be one of jpg, gif, or png.</param>
    /// <param name="reqProc">Request processor for handling results.</param>
    /// <returns>JSON response From BlueSKy.</returns>
    Task<string> PostImageAsync(string url, IDictionary<string, string> postData, byte[] data, string name, string fileName, string contentType, CancellationToken cancelToken);

    /// <summary>
    /// performs HTTP POST media byte array upload to BlueSky
    /// </summary>
    /// <param name="url">url to upload to</param>
    /// <param name="postData">request parameters</param>
    /// <param name="image">Image data in a byte[]</param>
    /// <param name="name">Name of parameter to pass to BlueSky.</param>
    /// <param name="fileName">name to pass to BlueeSky for the file</param>
    /// <param name="contentType">Type of image: must be one of jpg, gif, or png</param>
    /// <param name="mediaCategory">
    /// Media category - possible values are tweet_image, tweet_gif, tweet_video, and amplify_video. 
    /// See this post on the Twitter forums: https://twittercommunity.com/t/media-category-values/64781/6
    /// </param>
    /// <param name="shared">True if can be used in multiple DM Events.</param>
    /// <param name="cancelToken">Cancellation token</param>
    /// <returns>JSON results From BlueSky</returns>
    Task<string> PostMediaAsync(string url, IDictionary<string, string> postData, byte[] image, string name, string fileName, string? contentType, string mediaCategory, bool shared, CancellationToken cancelToken);

    /// <summary>
    /// makes HTTP call to BlueSky API
    /// </summary>
    /// <param name="url">URL with all query info</param>
    /// <param name="reqProc">Request Processor for Async Results</param>
    /// <returns>JSON Results from Twitter</returns>
    Task<string> QueryBlueSkyAsync<T>(Request req, IRequestProcessor<T> reqProc);

    /// <summary>
    /// Query for BlueSky Streaming APIs
    /// </summary>
    /// <param name="req">Request URL and parameters.</param>
    /// <returns>Placeholder - real data flows from stream into callback you define.</returns>
    Task<string> QueryBlueSkyStreamAsync(Request req);

    /// <summary>
    /// Allows users to process content returned from stream
    /// </summary>
    Func<StreamContent, Task>? StreamingCallbackAsync { get; set; }

    /// <summary>
    /// Set to true to close stream, false means stream is still open
    /// </summary>
    bool IsStreamClosed { get; }

    /// <summary>
    /// Allows callers to cancel operation (where applicable)
    /// </summary>
    CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Closes the stream
    /// </summary>
    void CloseStream();
}
