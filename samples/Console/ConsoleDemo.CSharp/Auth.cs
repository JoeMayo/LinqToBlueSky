using LinqToBlueSky.OAuth;

using System.Diagnostics;

namespace ConsoleDemo.CSharp;

public class Auth
{
    public static IAuthorizer ChooseAuthenticationStrategy()
    {
        Console.WriteLine("Authentication Strategy:\n\n");

        Console.WriteLine("  1 - Password");
        //Console.WriteLine("  2 - OAuth 2.0");

        Console.Write("\nPlease choose: ");
        ConsoleKeyInfo input = Console.ReadKey();
        Console.WriteLine("");

        IAuthorizer auth = input.KeyChar switch
        {
            '1' => DoPasswordAuth(),
            //'2' => DoOAuth2Auth(),
            _ => DoPasswordAuth(),
        };

        return auth;
    }

    static IAuthorizer DoPasswordAuth()
    {
        Console.Write("Username: ");
        string? username = Console.ReadLine();
        Console.Write("Password: ");
        string? password = Console.ReadLine();

        var auth = new PasswordAuthorizer
        {
            CredentialStore = new PasswordCredentials
            {
                Identifier = username,
                Password = password
            }
        };

        return auth;
    }

    // TODO: Need to refactor for BlueSky
    static IAuthorizer DoOAuth2Auth()
    {
        var auth = new OAuth2Authorizer()
        {
            CredentialStore = new OAuth2CredentialStore
            {
                ClientID = Environment.GetEnvironmentVariable(OAuthKeys.TwitterClientID),
                ClientSecret = Environment.GetEnvironmentVariable(OAuthKeys.TwitterClientSecret),
                Scopes = new List<string>
                {
                    "tweet.read",
                    "tweet.write",
                    "tweet.moderate.write",
                    "users.read",
                    "follows.read",
                    "follows.write",
                    "offline.access",
                    "space.read",
                    "mute.read",
                    "mute.write",
                    "like.read",
                    "like.write",
                    "block.read",
                    "block.write",
                    "bookmark.read",
                    "bookmark.write"
                },
                RedirectUri = "http://127.0.0.1:8599"
            },
            GoToTwitterAuthorization = pageLink =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pageLink,
                    UseShellExecute = true
                };
                Process.Start(psi);
            },
            HtmlResponseString = "<div>Awesome! Now you can use the app.</div>"
        };

        return auth;
    }
}
