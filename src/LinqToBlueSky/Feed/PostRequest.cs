using System.Text.Json.Serialization;

namespace LinqToBlueSky.Feed;

public class PostRequest
{
    [JsonPropertyName("$type")]
    public string? Type { get; set; }

    public required string Text { get; set; }

    public List<Facet>? Facets { get; set; }

    public Ref? Reply { get; set; }

    public Embed? Embed { get; set; }

    public List<string>? Langs { get; set; }

    public List<string>? Labels { get; set; }

    public List<string>? Tags { get; set; }

    public DateTime CreatedAt { get; set; }
}
