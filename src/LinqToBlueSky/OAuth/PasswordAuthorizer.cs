using System.Text;
using System.Text.Json;

namespace LinqToBlueSky.OAuth;

public class PasswordAuthorizer : AuthorizerBase, IAuthorizer
{
    public const string CredentialStoreMessage = "You must assign the CredentialStore property (with required values).";

    public async Task AuthorizeAsync()
    {
        const string blueSkyUrl = "https://bsky.social/xrpc/com.atproto.server.createSession";

        if (CredentialStore is not PasswordCredentials credentials)
            throw new ArgumentException($"{nameof(CredentialStore)} is required for authorization.");

        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Identifier, $"{nameof(credentials.Identifier)} is required for authorization.");
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Password, $"{nameof(credentials.Password)} is required for authorization.");

        string credentialsJson = 
            JsonSerializer.Serialize(
                new
                {
                    identifier = credentials.Identifier,
                    password = credentials.Password
                });
        StringContent content = new(credentialsJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await new HttpClient().PostAsync(blueSkyUrl, content);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        credentials.Session = JsonSerializer.Deserialize<BlueSkySession>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public override string GetAuthorizationString(string method, string oauthUrl, IDictionary<string, string> parameters)
    {
        if (CredentialStore is not PasswordCredentials credStore)
            throw new NullReferenceException(CredentialStoreMessage);

        ArgumentNullException.ThrowIfNull(credStore.Session?.AccessJwt, $"{nameof(credStore.Session.AccessJwt)} is required for authorization.");

        return $"Bearer {credStore.Session.AccessJwt}";
    }
}
