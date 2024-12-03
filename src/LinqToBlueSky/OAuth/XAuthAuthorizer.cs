﻿namespace LinqToBlueSky.OAuth;

// TODO: This is similar to the username/password authorization that BlueSky uses. Might just need a refactor.

public class XAuthAuthorizer : AuthorizerBase, IAuthorizer
{
    public async Task AuthorizeAsync()
    {
        if (CredentialStore is not XAuthCredentials xauthCredentials)
            throw new ArgumentException($"{nameof(CredentialStore)} is required for authorization.");

        if (xauthCredentials.UserName == null)
            throw new ArgumentException($"{nameof(xauthCredentials.UserName)} is required for authorization.");
        if (xauthCredentials.Password == null)
            throw new ArgumentException($"{nameof(xauthCredentials.Password)} is required for authorization.");

        var postData = new Dictionary<string, string>
        {
            {"x_auth_username", xauthCredentials.UserName},
            {"x_auth_password", xauthCredentials.Password},
            {"x_auth_mode", "client_auth"}
        };

        await PostAccessTokenAsync(postData).ConfigureAwait(false);
    }
}
