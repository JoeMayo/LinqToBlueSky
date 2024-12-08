using System.Text.Json.Serialization;

namespace LinqToBlueSky.Feed;

public class Thumb
{
    [JsonPropertyName("$type")]
    public string? Type { get; set; }
    public Ref? Ref { get; set; }
    public string? MimeType { get; set; }
    public int? Size { get; set; }
}