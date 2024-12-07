using System.Text.Json.Serialization;

namespace LinqToBlueSky.Feed;

public record FeedQuery
{
    //
    // input fields
    //

    /// <summary>
    /// <see cref="FeedType"/> of the feed."/>
    /// </summary>
    public FeedType Type { get; init; }

    /// <summary>
    /// Algorithm used to fetch the feed.
    /// </summary>
    public string? Algorithm { get; init; }

    /// <summary>
    /// Number of items to fetch.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Lets you page through the feed list.
    /// </summary>
    /// <remarks>
    /// Use cursor from previous response to fetch the next page.
    /// </remarks>
    [JsonPropertyName("input_cursor")] // input_cursor doesn't exist - adding it here to prevent the response "cursor" property from overwriting the input cursor setting.
    public string? Cursor { get; init; }

    //
    // response fields
    //

    /// <summary>
    /// Returned posts
    /// </summary>
    /// <remarks>
    /// The reason we're calling this FeedItem instead of Post is because each 
    /// item not only has a Post, but can optionally have a Reply and/or Reason.
    /// </remarks>
    public List<FeedItem>? Feed { get; init; }

    /// <summary>
    /// When you're paging through the feed, this is the cursor to use to get the next page.
    /// </summary>
    [JsonPropertyName("cursor")] // lets the response "cursor" property populate here.
    public string? NextCursor { get; init; }
}
