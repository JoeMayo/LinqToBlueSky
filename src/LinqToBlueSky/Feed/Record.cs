using System.Text.Json.Serialization;

namespace LinqToBlueSky.Feed;

public class Record
{
    public string? Type { get; set; }
    public string? Cid { get; set; }
    public string? Uri { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
    public object[]? Feeds { get; set; }
    public string? List { get; set; }
    public string? Name { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string>? Langs { get; set; }
    public Reply? Reply { get; set; }
    public string? Text { get; set; }
    public Embed? Embed { get; set; }
    public List<Embed>? Embeds { get; set; }
    public List<Facet>? Facets { get; set; }
    public Author? Author { get; set; }
    public Value? Value { get; set; }
    public List<Label>? Labels { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int QuoteCount { get; set; }
    public DateTime IndexedAt { get; set; }
    public Creator? Creator { get; set; }
    public int JoinedAllTimeCount { get; set; }
    public int JoinedWeekCount { get; set; }
    public object[]? Tags { get; set; }
    public string[]? HiddenReplies { get; set; }
    public string? Post { get; set; }
    [JsonPropertyName("record")]
    public Record? SubRecord { get; set; }
}