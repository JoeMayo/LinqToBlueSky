using LinqToBlueSky.Provider;
using System;

namespace LinqToBlueSky;

public partial class BlueSkyContext
{
    // TODO: Replace with BlueSkyQueryable<T> instances

    ///// <summary>
    ///// enables access to Twitter account information, such as Verify Credentials and Rate Limit Status
    ///// </summary>
    //public BlueSkyQueryable<Account> Account
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Account>(this);
    //    }
    //}

    ///// <summary>
    ///// Enables access to Twitter account activity information, such as listing webhooks and showing subscriptions.
    ///// </summary>
    //public BlueSkyQueryable<AccountActivity> AccountActivity
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<AccountActivity>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter blocking information, such as Exists, Blocks, and IDs
    ///// </summary>
    //public BlueSkyQueryable<Blocks> Blocks
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Blocks>(this);
    //    }
    //}

    ///// <summary>
    ///// Enables querying compliance jobs
    ///// </summary>
    //public BlueSkyQueryable<ComplianceQuery> Compliance
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<ComplianceQuery>(this);
    //    }
    //}

    //public BlueSkyQueryable<Counts> Counts 
    //{ 
    //    get
    //    {
    //        return new BlueSkyQueryable<Counts>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Direct Message Events, supporting Twitter chatbots
    ///// </summary>
    //public BlueSkyQueryable<DirectMessageEvents> DirectMessageEvents
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<DirectMessageEvents>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Favorites
    ///// </summary>
    //[Obsolete("Please use the new v2 `Likes` query instead.")]
    //public BlueSkyQueryable<Favorites> Favorites
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Favorites>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Friendship info
    ///// </summary>
    //public BlueSkyQueryable<Friendship> Friendship
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Friendship>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Geo info
    ///// </summary>
    //public BlueSkyQueryable<Geo> Geo
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Geo>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Help info
    ///// </summary>
    //public BlueSkyQueryable<Help> Help
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Help>(this);
    //    }
    //}

    ///// <summary>
    ///// Enables access to media commands, like STATUS (Twitter API v1)
    ///// </summary>
    //public BlueSkyQueryable<Media> Media
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Media>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Likes lookup (Twitter API v2)
    ///// </summary>
    //public BlueSkyQueryable<LikeQuery> Likes => new(this);

    ///// <summary>
    ///// enables access to Twitter List info
    ///// </summary>
    //public BlueSkyQueryable<ListQuery> List => new(this);

    ///// <summary>
    ///// Enables access to muted users
    ///// </summary>
    //public BlueSkyQueryable<Mute> Mute
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Mute>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Raw Query Extensibility (All Twitter API versions)
    ///// </summary>
    //public BlueSkyQueryable<Raw> RawQuery => new(this);

    ///// <summary>
    ///// enables access to Twitter Saved Searches
    ///// </summary>
    //public BlueSkyQueryable<SavedSearch> SavedSearch
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<SavedSearch>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Search to query tweets (Twitter API v1)
    ///// </summary>
    //public BlueSkyQueryable<Search> Search => new(this);

    ///// <summary>
    ///// enables access to Twitter Search v2 to query tweets (Twitter API v2)
    ///// </summary>
    //public BlueSkyQueryable<TwitterSearch> TwitterSearch => new(this);

    ///// <summary>
    ///// enables access to Twitter Search v2 to search spaces (Twitter API v2)
    ///// </summary>
    //public BlueSkyQueryable<SpacesQuery> Spaces => new(this);

    ///// <summary>
    ///// enables access to Twitter Status messages (Twitter API v1)
    ///// </summary>
    //public BlueSkyQueryable<Status> Status
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Status>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter streams
    ///// </summary>
    //public BlueSkyQueryable<Streaming> Streaming
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Streaming>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Trends, such as Trend, Current, Daily, and Weekly
    ///// </summary>
    //public BlueSkyQueryable<Trend> Trends
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<Trend>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Tweets lookup (Twitter API v2)
    ///// </summary>
    //public BlueSkyQueryable<TweetQuery> Tweets => new(this);


    ///// <summary>
    ///// enables access to Twitter blocking information, such as Lookup
    ///// </summary>
    //public BlueSkyQueryable<TwitterBlocksQuery> TwitterBlocks => new(this);

    ///// <summary>
    ///// enables access to Twitter User lookup (Twitter API v2)
    ///// </summary>
    //public BlueSkyQueryable<TwitterUserQuery> TwitterUser => new(this);

    ///// <summary>
    ///// enables access to Twitter User messages, such as Friends and Followers (Twitter API v1)
    ///// </summary>
    //public BlueSkyQueryable<User> User
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<User>(this);
    //    }
    //}

    ///// <summary>
    ///// enables access to Twitter Welcome messages
    ///// </summary>
    //public BlueSkyQueryable<WelcomeMessage> WelcomeMessage
    //{
    //    get
    //    {
    //        return new BlueSkyQueryable<LinqToBlueSky.WelcomeMessage>(this);
    //    }
    //}
}
