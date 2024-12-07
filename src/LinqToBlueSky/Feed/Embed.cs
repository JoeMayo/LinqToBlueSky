namespace LinqToBlueSky.Feed;

public class Embed
{
    public string? Type { get; set; }
    public External? External { get; set; }
    public List<PostImage>? Images { get; set; }
    public Record? Record { get; set; }
    public Media? Media { get; set; }
}