using LinqToBlueSky.Provider;

using System.Net;

namespace LinqToBlueSky.Net;

class GetMessageHandler : HttpClientHandler
{
    readonly BlueSkyExecute exe;
    readonly IDictionary<string, string> parameters;
    readonly string url;
    readonly bool authorizerSupportsCompression;

    public GetMessageHandler(BlueSkyExecute exe, IDictionary<string, string> parameters, string url, bool authorizerSupportsCompression)
    {
        this.exe = exe;
        this.parameters = parameters;
        this.url = url;
        this.authorizerSupportsCompression = authorizerSupportsCompression;
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        exe.SetAuthorizationHeader(HttpMethod.Get.ToString(), url, parameters, request);
        request.Headers.Add("User-Agent", exe.UserAgent);
        request.Headers.ExpectContinue = false;
        if (SupportsAutomaticDecompression && authorizerSupportsCompression)
            AutomaticDecompression = DecompressionMethods.GZip;
        if (exe.Authorizer?.Proxy != null && SupportsProxy)
            Proxy = exe.Authorizer.Proxy;

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
