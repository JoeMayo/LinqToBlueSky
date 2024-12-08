using ConsoleDemo.CSharp;

using LinqToBlueSky;
using LinqToBlueSky.OAuth;

try
{
    await DoDemosAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

Console.Write("\nPress any key to close console window...");
Console.ReadKey(true);

static async Task DoDemosAsync()
{
    IAuthorizer auth = Auth.ChooseAuthenticationStrategy();

    await auth.AuthorizeAsync();

    // TODO: Update notes for BlueSky
    // For OAuth 1.0A Only: This is how you access credentials after authorization.
    // The oauthToken and oauthTokenSecret do not expire.
    // You can use the userID to associate the credentials with the user.
    // You can save credentials any way you want - database, isolated storage, etc. - it's up to you.
    // You can retrieve and load all 4 credentials on subsequent queries to avoid the need to re-authorize.
    // When you've loaded all 4 credentials, LINQ to Twitter will let you make queries without re-authorizing.
    //
    //var credentials = auth.CredentialStore;
    //string oauthToken = credentials.OAuthToken;
    //string oauthTokenSecret = credentials.OAuthTokenSecret;
    //string screenName = credentials.ScreenName;
    //ulong userID = credentials.UserID;
    //
    // For OAuth 2.0 (preferred), you can get credentials like this:
    //var credentials = auth.CredentialStore as IOAuth2CredentialStore;
    //string accessToken = credentials.AccessToken;
    //string refreshToken = credentials.RefreshToken
    //

    BlueSkyContext ctx = new(auth);
    char key;

    do
    {
        ShowMenu();

        key = Console.ReadKey(true).KeyChar;

        switch (char.ToUpper(key))
        {
            case '0':
                Console.WriteLine("\n\tRunning Feed Demos...\n");
                await FeedDemos.RunAsync(ctx);
                break;
            case 'Q':
                Console.WriteLine("\nQuitting...\n");
                break;
            default:
                Console.WriteLine(key + " is unknown");
                break;
        }

    } while (char.ToUpper(key) != 'Q');
}

static void ShowMenu()
{
    Console.WriteLine("\nPlease select category:\n");

    Console.WriteLine("\t 0. Feed Demos");
    Console.WriteLine();
    Console.Write("\t Q. End Program");
}