﻿using LinqToBlueSky.Common;
using LinqToBlueSky.Net;

using System.Net;
using System.Text;

namespace LinqToBlueSky.OAuth;

// TODO: A lot of this implements OAuth 1.0A, which might not be useful for BlueSky.

public class AuthorizerBase
{
    /// <summary>
    /// URL for OAuth Request Tokens
    /// </summary>
    public string OAuthRequestTokenUrl { get; set; }

    /// <summary>
    /// URL for OAuth authorization
    /// </summary>
    public string? OAuthAuthorizeUrl { get; set; }

    /// <summary>
    /// URL for OAuth Access Tokens
    /// </summary>
    public string OAuthAccessTokenUrl { get; set; }

    /// <summary>
    /// Get/Set Credentials
    /// </summary>
    public ICredentialStore? CredentialStore { get; set; }

    /// <summary>
    /// Force the user to enter their name/password when authorizing
    /// </summary>
    public bool ForceLogin { get; set; }

    /// <summary>
    /// Overrides read/write settings for application registered with Twitter
    /// </summary>
    public AuthAccessType AccessType { get; set; }

    /// <summary>
    /// Optional name to prefill when user visits the Twitter authorization screen
    /// </summary>
    public string? PreFillScreenName { get; set; }

    /// <summary>
    /// User-Agent header string sent to Twitter to represent your application. Defaults to LINQ to Twitter.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Url that Twitter redirects to after user authorizes your app.
    /// </summary>
    public string? Callback { get; set; }

    public IWebProxy? Proxy { get; set; }

    public bool SupportsCompression { get; set; }

    protected static string? ParseVerifierFromResponseUrl(string responseUrl)
    {
        string[] keyValPairs = new Uri(responseUrl).Query.TrimStart('?').Split('&');

        string? verifier =
            (from keyValPair in keyValPairs
             let pair = keyValPair.Split('=')
             let key = pair[0]
             let val = pair.Length == 2 ? pair[1] : string.Empty
             where key == "oauth_verifier"
             select val)
            .SingleOrDefault();

        return verifier;
    }

    IDictionary<string, string>? parameters;
    public IDictionary<string, string> Parameters
    {
        get 
        {
            if (parameters == null)
                parameters = new Dictionary<string, string>
                {
                    { "oauth_consumer_key", CredentialStore?.ConsumerKey ?? string.Empty },
                    { "oauth_token", CredentialStore?.OAuthToken ?? string.Empty }
                };

            return parameters; 
        }
    }

    public AuthorizerBase() : this(false, AuthAccessType.NoChange, string.Empty) { }

    public AuthorizerBase(bool forceLogin, AuthAccessType accessType, string prefillScreenName)
    {
        ForceLogin = forceLogin;
        AccessType = accessType;
        PreFillScreenName = prefillScreenName;
        SupportsCompression = true;

        if (string.IsNullOrWhiteSpace(UserAgent))
            UserAgent = L2BSKeys.DefaultUserAgent;

        OAuthRequestTokenUrl = "https://api.twitter.com/oauth/request_token";
        OAuthAuthorizeUrl = "https://api.twitter.com/oauth/authorize";
        OAuthAccessTokenUrl = "https://api.twitter.com/oauth/access_token";
    }

    public async Task GetRequestTokenAsync(string callback)
    {
        if (string.IsNullOrWhiteSpace(callback))
            throw new ArgumentNullException(nameof(callback), "callback is required.");

        Parameters.Add("oauth_callback", EncodeToProtectMultiByteCharUrls(callback));
        Parameters.Remove("oauth_token");

        if (AccessType != AuthAccessType.NoChange)
            Parameters.Add("x_auth_access_type", AccessType.ToString().ToLower());

        string response = await HttpGetAsync(OAuthRequestTokenUrl, Parameters).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException("Empty response to request token response from Twitter.");

        UpdateCredentialsFromRequestTokenResponse(response);
    }

    static string EncodeToProtectMultiByteCharUrls(string callback)
    {
        return callback == "oob" ? "oob" : new Uri(callback).AbsoluteUri;
    }

    public string PrepareAuthorizeUrl(bool forceLogin)
    {
        if (CredentialStore?.OAuthToken == null)
            throw new InvalidOperationException("OAuthToken not set. Call GetRequestTokenAsync first and verify that OAuthToken is set.");

        string forceLoginParamString = forceLogin ? "&force_login=true" : "";

        string preFillScreenNameParamString = 
            !string.IsNullOrWhiteSpace(PreFillScreenName) ? "&screen_name=" + PreFillScreenName : "";

        return string.Format(
            "{0}?oauth_token={1}{2}{3}", 
            OAuthAuthorizeUrl, CredentialStore.OAuthToken, 
            forceLoginParamString, preFillScreenNameParamString);
    }

    public async Task GetAccessTokenAsync(IDictionary<string, string> accessTokenParams)
    {
        if (!accessTokenParams.ContainsKey("oauth_verifier"))
            throw new ArgumentException("oauth_verifier is required.");

        foreach (var key in accessTokenParams.Keys)
            Parameters.Add(key, accessTokenParams[key]);

        Parameters.Remove("oauth_callback");

        string response = await HttpGetAsync(OAuthAccessTokenUrl, Parameters).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException("Empty response to access token response from Twitter.");
        
        UpdateCredentialsWithAccessTokenResponse(response);
    }

    public async Task PostAccessTokenAsync(IDictionary<string, string> accessTokenParams)
    {
        if (!accessTokenParams.ContainsKey("x_auth_mode") && !accessTokenParams.ContainsKey("oauth_verifier"))
            throw new ArgumentException("oauth_verifier is required, unless using xAuth.");

        foreach (var key in accessTokenParams.Keys)
            Parameters.Add(key, accessTokenParams[key]);

        Parameters.Remove("oauth_callback");

        string response = await HttpPostAsync(OAuthAccessTokenUrl, Parameters).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException("Empty response to access token response from Twitter.");

        UpdateCredentialsWithAccessTokenResponse(response);
    }

    void UpdateCredentialsFromRequestTokenResponse(string response)
    {
        if (CredentialStore != null)
            CredentialStore.OAuthToken =
                (from nameValPair in response.Split('&')
                 let pair = nameValPair.Split('=')
                 where pair[0] == "oauth_token"
                 select pair[1])
                .SingleOrDefault();

        Parameters.Add("oauth_token", CredentialStore?.OAuthToken ?? string.Empty);
    }

    void UpdateCredentialsWithAccessTokenResponse(string response)
    {
        if (CredentialStore == null)
            return;

        var responseParams =
            (from nameValPair in response.Split('&')
             let pair = nameValPair.Split('=')
             select new
             {
                 Key = pair[0],
                 Value = pair[1]
             })
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value);

        if (responseParams["oauth_token"] != null)
            CredentialStore.OAuthToken = responseParams["oauth_token"];

        if (responseParams["oauth_token_secret"] != null)
            CredentialStore.OAuthTokenSecret = responseParams["oauth_token_secret"];

        if (responseParams["screen_name"] != null)
            CredentialStore.ScreenName = responseParams["screen_name"];

        if (responseParams["user_id"] != null)
        {
            _ = ulong.TryParse(responseParams["user_id"], out ulong userID);
            CredentialStore.UserID = userID;
        }
    }

    internal async Task<string> HttpGetAsync(string oauthUrl, IDictionary<string, string> parameters)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, oauthUrl);
        req.Headers.Add("Authorization", GetAuthorizationString(HttpMethod.Get.ToString(), oauthUrl, parameters));
        req.Headers.Add("User-Agent", UserAgent);
        req.Headers.ExpectContinue = false;

        var handler = new HttpClientHandler();
        if (handler.SupportsAutomaticDecompression)
            handler.AutomaticDecompression = DecompressionMethods.GZip;
        if (Proxy != null && handler.SupportsProxy)
            handler.Proxy = Proxy;

        var msg = await new HttpClient(handler).SendAsync(req).ConfigureAwait(false);

        await BlueSkyErrorHandler.ThrowIfErrorAsync(msg).ConfigureAwait(false);

        return await msg.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    internal async Task<string> HttpPostAsync(string oauthUrl, IDictionary<string, string> parameters)
    {
        var postData =
            (from keyValPair in parameters
             where !keyValPair.Key.StartsWith("oauth")
             select keyValPair)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var req = new HttpRequestMessage(HttpMethod.Post, oauthUrl);
        req.Headers.Add("Authorization", GetAuthorizationString(HttpMethod.Post.ToString(), oauthUrl, parameters));
        req.Headers.Add("User-Agent", UserAgent);
        req.Headers.ExpectContinue = false;

        var paramString =
            string.Join("&",
                (from parm in postData
                 select parm.Key + "=" + Url.PercentEncode(parm.Value))
                .ToList());
        var content = new StringContent(paramString, Encoding.UTF8, "application/x-www-form-urlencoded");
        req.Content = content;

        var handler = new HttpClientHandler();
        if (handler.SupportsAutomaticDecompression)
            handler.AutomaticDecompression = DecompressionMethods.GZip;
        if (Proxy != null && handler.SupportsProxy)
            handler.Proxy = Proxy;

        var msg = await new HttpClient(handler).SendAsync(req).ConfigureAwait(false);

        await BlueSkyErrorHandler.ThrowIfErrorAsync(msg).ConfigureAwait(false);

        return await msg.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    public virtual string GetAuthorizationString(string method, string oauthUrl, IDictionary<string, string> parameters)
    {
        if (CredentialStore == null)
            return string.Empty;

        string consumerSecret = CredentialStore.ConsumerSecret ?? string.Empty;
        string oAuthTokenSecret = CredentialStore.OAuthTokenSecret ?? string.Empty;
        string authorizationString =
            new Signature().GetAuthorizationString(
                method, oauthUrl, parameters, consumerSecret, oAuthTokenSecret);
        return authorizationString;
    }
}
