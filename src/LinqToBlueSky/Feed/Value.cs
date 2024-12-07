namespace LinqToBlueSky.Feed;

public class Value
{
    public string? Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public Embed? Embed { get; set; }
    public List<Facet>? Facets { get; set; }
    public List<string>? Langs { get; set; }
    public string? Text { get; set; }
    public Reply? Reply { get; set; }
}