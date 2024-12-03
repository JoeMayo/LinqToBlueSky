using System.Net;

namespace LinqToBlueSky.OAuth;

public interface IAuthorizer
{
    Task AuthorizeAsync();

    string? UserAgent { get; set; }

    ICredentialStore? CredentialStore { get; set; }

    IWebProxy? Proxy { get; set; }

    bool SupportsCompression { get; set; }

    string? GetAuthorizationString(string method, string oauthUrl, IDictionary<string, string> parameters);
}