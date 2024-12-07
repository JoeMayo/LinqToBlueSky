namespace LinqToBlueSky.Feed;

public class FeedItem
{
    public Post? Post { get; set; }

    public Reply? Reply { get; set; }

    public Reason? Reason { get; set; }
}
