namespace LinqToBlueSky.OAuth;

/// <summary>
/// Used for <see cref="PasswordAuthorizer">
/// </summary>
public class PasswordCredentials : InMemoryCredentialStore
{
    /// <summary>
    /// BlueSky Username
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// BlueSky Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// After successful authorization, this will contain the session information.
    /// </summary>
    public BlueSkySession? Session { get; set; }
}
