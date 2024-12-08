using System.Text.Json.Serialization;

namespace LinqToBlueSky.Feed;

public class Ref
{
    [JsonPropertyName("$link")]
    public string? Link { get; set; }
}