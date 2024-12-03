namespace LinqToBlueSky.OAuth;

public interface ISignature
{
    string GetAuthorizationString(string method, string url, IDictionary<string, string> parameters, string consumerSecret, string oAuthTokenSecret);
}