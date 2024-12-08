using LinqToBlueSky.Feed;
using LinqToBlueSky.Provider;

namespace LinqToBlueSky;

public partial class BlueSkyContext
{
    public BlueSkyQueryable<FeedQuery> Feed => new BlueSkyQueryable<FeedQuery>(this);
}
