namespace LinqToBlueSky.Feed;

public class Post
{
    public string? Uri { get; set; }
    public string? Cid { get; set; }
    public Author? Author { get; set; }
    public Record? Record { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int LikeCount { get; set; }
    public int QuoteCount { get; set; }
    public DateTime IndexedAt { get; set; }
    public AccountViewer? Viewer { get; set; }
    public List<Label>? Labels { get; set; }
    public Embed? Embed { get; set; }
}

